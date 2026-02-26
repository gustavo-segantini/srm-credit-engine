# High-Scale Design: SRM Credit Engine @ 1M transações/min

> Este documento descreve como a arquitetura atual (monólito modular) evoluiria para suportar **1 milhão de transações por minuto** (~16.667 tx/s), mantendo latência P99 < 200ms para precificação e consistência eventual aceitável para relatórios.

---

## Baseline Atual

| Recurso          | Capacidade estimada MVP |
|------------------|------------------------|
| API replicas     | 1 instância            |
| PostgreSQL       | 1 primary, 0 replicas  |
| Throughput       | ~500 rps (não validado)|
| Latência P99     | < 100ms (local)        |

---

## Arquitetura Target (1M tx/min)

```
                        ┌─────────────────────────────────────────┐
                        │            Load Balancer (L7)            │
                        │         NGINX / AWS ALB / Cloudflare     │
                        └────────────────┬────────────────────────┘
                                         │
               ┌─────────────────────────▼─────────────────────────┐
               │                  API Gateway                       │
               │          (Rate limiting, Auth JWT/mTLS,            │
               │           Request routing, Circuit breaker)        │
               └────────┬──────────────┬──────────────┬────────────┘
                        │              │              │
              ┌─────────▼──┐  ┌────────▼──┐  ┌───────▼───────┐
              │ Pricing API │  │Settlement │  │  FxRate API   │
              │  (N replicas│  │    API    │  │  (N replicas) │
              │  stateless) │  │(N replicas│  │               │
              └─────────┬──┘  └────────┬──┘  └───────┬───────┘
                        │              │              │
                        │    ┌─────────▼──────────┐  │
                        │    │   Kafka / Redpanda   │  │
                        │    │  SettlementCreated   │  │
                        │    │  SettlementSettled   │  │
                        │    └─────────┬──────────┘  │
                        │              │              │
               ┌─────────▼──────────┐  │  ┌──────────▼─────────┐
               │   Redis Cluster    │  │  │  Redis Cluster      │
               │   (Pricing cache)  │  │  │  (FxRate cache)     │
               │   TTL: 60s         │  │  │  TTL: 30s           │
               └─────────┬──────────┘  │  └──────────┬─────────┘
                         │             │              │
               ┌──────────────────────────────────────────────────┐
               │                PostgreSQL Cluster                  │
               │  ┌───────────────┐    ┌────────────────────────┐  │
               │  │   Primary     │    │   Read Replicas (3x)   │  │
               │  │  (Writes)     │◄──►│   (Queries/Reports)    │  │
               │  └───────────────┘    └────────────────────────┘  │
               │  Particionamento por mês em `settlements`          │
               │  PgBouncer (connection pooling, max 1000 conn)     │
               └──────────────────────────────────────────────────┘
```

---

## Estratégias por Camada

### 1. API Layer — Escala Horizontal Stateless

**Princípio:** Cada replica da API é completamente stateless. Session state e cache vivem fora (Redis).

```yaml
# Kubernetes HPA
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
spec:
  minReplicas: 3
  maxReplicas: 50
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          averageUtilization: 60
    - type: Pods
      pods:
        metric:
          name: http_requests_per_second
        target:
          averageValue: 500
```

**Por que funciona:** Precificação é CPU-bound mas sem estado — cada calculo de PV/spread é independente.

---

### 2. Read Cache — Redis para Precificação e Câmbio

Taxa de câmbio é lida em TODA simulação de precificação. Sem cache, o PostgreSQL seria martelado a 16.667 leituras/s de uma tabela tiny.

```csharp
// IExchangeRateRepository — cache-aside pattern
public async Task<ExchangeRate> GetRateAsync(CurrencyCode from, CurrencyCode to)
{
    var cacheKey = $"fx:{from}:{to}";
    
    if (_cache.TryGetValue(cacheKey, out ExchangeRate? cached))
        return cached;
    
    var rate = await _db.ExchangeRates
        .FirstOrDefaultAsync(r => r.FromCurrency == from && r.ToCurrency == to);
    
    _cache.Set(cacheKey, rate, TimeSpan.FromSeconds(30));
    return rate;
}
```

**Cache invalidation:** `ExchangeRateUpdated` domain event dispara `_cache.Remove(cacheKey)` em todos os pods via Pub/Sub do Redis.

---

### 3. PostgreSQL — Particionamento + Read Replicas

**Particionamento declarativo por mês em `settlements`:**

```sql
CREATE TABLE settlements (
    id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    ...
    PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);

CREATE TABLE settlements_2025_01 PARTITION OF settlements
    FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');

-- Criação automática de partições via pg_partman ou cron job
```

**Benefit:** `SELECT` em `TransactionGrid` com filtro de data tocará apenas 1–2 partições em vez de varrer toda a tabela.

**Connection pooling via PgBouncer:**
```ini
[databases]
srm = host=postgres-primary port=5432 dbname=srm_credit_engine

[pgbouncer]
pool_mode = transaction          ; mais eficiente que session mode
max_client_conn = 10000
default_pool_size = 25           ; 25 conn reais ao PostgreSQL
```

**Read Replicas:** Queries de relatório (`/api/v1/reports/statement`) apontam para replica; escrita aponta para primary. Implementado via dois `DbContext` registrados com diferentes connection strings:

```csharp
services.AddDbContext<AppDbContext>(o => o.UseNpgsql(writeConnStr));
services.AddDbContext<AppReadDbContext>(o => o.UseNpgsql(readConnStr));
```

---

### 4. Kafka — Processamento Assíncrono de Liquidações

Para 1M tx/min, a confirmação síncrona bloqueia o operador. A solução é **Command → Event → Eventual Settlement**:

```
POST /api/v1/settlements
  → SettlementController publica SettlementRequested no Kafka topic
  → HTTP 202 Accepted { settlementId, status: "Processing" }

Consumer group: srm-settlement-workers (N workers)
  → Consome SettlementRequested
  → Executa validação + cálculo + DB write
  → Publica SettlementSettled ou SettlementFailed

WebSocket / SSE: cliente poll status via GET /settlements/{id}
```

**Topics e retenção:**
| Topic                    | Partições | Retenção |
|--------------------------|-----------|----------|
| `settlement.requested`   | 32        | 7 dias   |
| `settlement.settled`     | 32        | 30 dias  |
| `settlement.failed`      | 8         | 30 dias  |
| `exchange_rate.updated`  | 4         | 7 dias   |

---

### 5. Outbox Pattern — Garantia de Entrega

Para prevenir perda de eventos se o broker cair após o DB commit:

```sql
CREATE TABLE outbox_events (
    id UUID PRIMARY KEY,
    aggregate_type TEXT NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type TEXT NOT NULL,
    payload JSONB NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    published_at TIMESTAMPTZ
);
```

```csharp
// No mesmo DbContext.SaveChanges()
_db.OutboxEvents.Add(new OutboxEvent {
    AggregateType = "Settlement",
    AggregateId = settlement.Id,
    EventType = "SettlementSettled",
    Payload = JsonSerializer.Serialize(domainEvent)
});
await _db.SaveChangesAsync(); // atômico com o settlement
```

Worker separado publica eventos do outbox para Kafka e marca `published_at`. Retry automático em falha.

---

## SLOs Target

| Métrica               | Target                 |
|-----------------------|------------------------|
| Precificação P50      | < 20ms                 |
| Precificação P99      | < 100ms                |
| Liquidação (assíncrona) sync | < 50ms (202)   |
| Liquidação settled    | < 2s (via polling)     |
| Disponibilidade       | 99.9% (8h downtime/ano)|
| RPO (Recovery Point)  | 5 minutos              |
| RTO (Recovery Time)   | 15 minutos             |

---

## Custo Estimado (AWS, us-east-1)

| Recurso                           | Especificação           | ~Custo/mês   |
|-----------------------------------|-------------------------|--------------|
| EKS Cluster + 6 c6i.xlarge nodes  | 4 vCPU / 8 GB each      | ~$900        |
| RDS PostgreSQL Multi-AZ (r6g.2xl) | 8 vCPU / 64 GB + 3 replicas | ~$1.200  |
| ElastiCache Redis Cluster (3x r6g.large) | 2 vCPU / 13 GB  | ~$400        |
| MSK Kafka (3 brokers kafka.m5.large) | —                    | ~$450        |
| Load Balancer + data transfer     | —                       | ~$200        |
| **Total estimado**                |                         | **~$3.150/mês** |

---

## Plano de Migração (Fases)

```
Fase 1 (atual MVP):  Monólito + PostgreSQL + Redis (apenas FxRate cache)
Fase 2 (10k tx/min): + 3 API replicas + PgBouncer + Read replicas
Fase 3 (100k tx/min): + Kafka async settlement + Outbox pattern
Fase 4 (1M tx/min):  + Particionamento + HPA + Multi-region (active-passive)
```

Ver [EDA Proposal](./eda-proposal.md) para detalhes do Kafka + Outbox.
