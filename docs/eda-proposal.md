# EDA Proposal: Event-Driven Architecture para SRM Credit Engine

> Proposta de evolução da arquitetura monolítica atual para um modelo orientado a eventos, habilitando processamento assíncrono, auditoria e integração com sistemas externos.

---

## Motivação

A arquitetura síncrona atual (HTTP Request/Response) tem limitações bem definidas:

| Limitação                     | Impacto                                               |
|-------------------------------|-------------------------------------------------------|
| Liquidação síncrona           | Timeout em picos de carga                             |
| Sem auditoria de eventos      | Compliance financeiro exige trilha de mudanças        |
| Integração com corretoras/ERPs| Acoplamento direto via REST aumenta fragilidade       |
| Relatórios bloqueiam escrita  | Queries OLAP concorrem com OLTP                       |

---

## Domain Events Identificados

### Settlement Domain

| Evento                  | Trigger                               | Payload                                               |
|-------------------------|---------------------------------------|-------------------------------------------------------|
| `SettlementRequested`   | POST /settlements                     | receivableIds, paymentCurrency, operatorId            |
| `SettlementCreated`     | Settlement.CreatePending()            | id, cedentId, status=Pending, amounts, timestamp      |
| `SettlementSettled`     | Settlement.MarkAsSettled()            | id, settledAt, netDisbursement, currency              |
| `SettlementFailed`      | Settlement.MarkAsFailed()             | id, reason, failedAt                                  |
| `SettlementCancelled`   | Settlement.Cancel()                   | id, cancelledAt, cancelledBy                          |

### Exchange Rate Domain

| Evento                  | Trigger                               | Payload                                               |
|-------------------------|---------------------------------------|-------------------------------------------------------|
| `ExchangeRateUpdated`   | PUT /exchange-rates                   | fromCurrency, toCurrency, rate, updatedAt, updatedBy  |

### Pricing Domain

| Evento                  | Trigger                               | Payload                                               |
|-------------------------|---------------------------------------|-------------------------------------------------------|
| `PricingSimulated`      | POST /pricing/simulate (opcional)     | receivableType, term, faceValue, presentValue, spread |

---

## Topologia Kafka

```
Producers (SRM API)
    │
    ├──► settlement.requested    [32 partitions, key=cedentId]
    ├──► settlement.lifecycle    [32 partitions, key=settlementId]
    ├──► exchange-rate.updated   [4 partitions,  key=currencyPair]
    └──► pricing.simulated       [8 partitions,  key=receivableType]

Consumers
    ├── srm-settlement-workers   ← settlement.requested → DB write + settlement.lifecycle
    ├── srm-audit-consumer       ← settlement.lifecycle → append-only audit_events table
    ├── srm-notification-consumer← settlement.lifecycle → e-mail/webhook para cedente
    ├── srm-fx-cache-consumer    ← exchange-rate.updated → Redis.Remove(cacheKey)
    └── srm-reporting-consumer   ← settlement.lifecycle → materialized view refresh
```

---

## Outbox Pattern — Entrega Garantida

O maior risco em EDA financeiro é: **o DB commita mas o broker não recebe o evento** (crash entre as duas operações). O Outbox Pattern resolve isso com **transação única**.

### Schema

```sql
CREATE TABLE outbox_events (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_type TEXT       NOT NULL,  -- 'Settlement', 'ExchangeRate'
    aggregate_id UUID        NOT NULL,
    event_type   TEXT        NOT NULL,  -- 'SettlementSettled'
    payload      JSONB       NOT NULL,
    created_at   TIMESTAMPTZ DEFAULT NOW(),
    published_at TIMESTAMPTZ,           -- NULL = não publicado
    retry_count  INT         DEFAULT 0
);

CREATE INDEX idx_outbox_unpublished ON outbox_events (created_at)
    WHERE published_at IS NULL;
```

### Implementação C#

```csharp
// Domain Event base
public abstract record DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public abstract string EventType { get; }
}

public record SettlementSettledEvent(
    Guid SettlementId,
    DateTimeOffset SettledAt,
    decimal NetDisbursement,
    string Currency
) : DomainEvent
{
    public override string EventType => "SettlementSettled";
}

// Application Handler
public class CreateSettlementHandler
{
    public async Task Handle(CreateSettlementCommand cmd)
    {
        // ... business logic ...
        
        settlement.MarkAsSettled();
        
        // Escreve no outbox NA MESMA TRANSAÇÃO
        _db.OutboxEvents.Add(new OutboxEvent
        {
            AggregateType = "Settlement",
            AggregateId = settlement.Id,
            EventType = "SettlementSettled",
            Payload = JsonSerializer.SerializeToDocument(new SettlementSettledEvent(
                settlement.Id, settlement.SettledAt!.Value,
                settlement.NetDisbursementAmount, settlement.PaymentCurrency.ToString()))
        });
        
        await _unitOfWork.SaveChangesAsync(); // Atômico: settlement + outbox
    }
}
```

### Outbox Publisher Worker

```csharp
// Background service rodando a cada 100ms
public class OutboxPublisherWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var events = await _db.OutboxEvents
                .Where(e => e.PublishedAt == null && e.RetryCount < 5)
                .OrderBy(e => e.CreatedAt)
                .Take(100)
                .ToListAsync(ct);
            
            foreach (var evt in events)
            {
                try
                {
                    await _kafka.ProduceAsync(
                        topic: GetTopic(evt.EventType),
                        key: evt.AggregateId.ToString(),
                        value: evt.Payload);
                    
                    evt.PublishedAt = DateTimeOffset.UtcNow;
                }
                catch
                {
                    evt.RetryCount++;
                }
            }
            
            await _db.SaveChangesAsync(ct);
            await Task.Delay(100, ct);
        }
    }
    
    private static string GetTopic(string eventType) => eventType switch
    {
        "SettlementSettled" or "SettlementFailed" => "settlement.lifecycle",
        "ExchangeRateUpdated" => "exchange-rate.updated",
        _ => "srm.events"
    };
}
```

---

## Idempotência dos Consumers

Eventos podem ser entregues mais de uma vez (Kafka at-least-once). Consumers devem ser idempotentes:

```csharp
// Consumer de auditoria
public async Task ConsumeAsync(SettlementSettledEvent evt)
{
    // Upsert idempotente via ON CONFLICT DO NOTHING
    await _db.ExecuteAsync("""
        INSERT INTO audit_events (event_id, event_type, payload, created_at)
        VALUES (@Id, @EventType, @Payload::jsonb, @OccurredAt)
        ON CONFLICT (event_id) DO NOTHING
        """, evt);
}
```

---

## Schema Registry

Para contratos de eventos versionados, adotar **Confluent Schema Registry** com Avro ou **Protobuf**:

```
settlement.lifecycle
  ├── v1: { id, status, settledAt, amounts }
  └── v2: { id, status, settledAt, amounts, +operatorId }  ← backward compatible
```

Regra: somente **additive changes** em versões menores. Breaking changes → novo tópico.

---

## Plano de Adoção Gradual

```
Sprint 1 — Outbox implementado no monólito (sem broker externo)
  → Background worker lê outbox e faz HTTP para consumers internos

Sprint 2 — Kafka introduzido para settlement.lifecycle
  → Consumer de auditoria primeiro (baixo risco)

Sprint 3 — settlement.requested assíncrono
  → API retorna 202, status via polling

Sprint 4 — Extração de consumers para microservices independentes
  → Notification Service, Reporting Service separados
```

---

## Trade-offs

| Aspecto                  | Síncrono (atual)             | Assíncrono (EDA)                         |
|--------------------------|------------------------------|------------------------------------------|
| Complexidade operacional | Baixa                        | Alta (Kafka, schema registry, DLQ)       |
| Throughput               | ~500 rps / replica           | ~50.000 rps com particionamento          |
| Consistência             | Forte (ACID)                 | Eventual (acceptable para liquidações)   |
| Auditoria                | Manual                       | Event sourcing nativo                    |
| Debugging                | Stack trace local            | Distributed tracing obrigatório (OTLP)  |
| Modelo mental            | Request/Response             | Command/Event — curva de aprendizado     |

---

## Referências

- [Transactional Outbox Pattern — microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- [Debezium CDC como alternativa ao outbox manual](https://debezium.io/documentation/reference/stable/transformations/outbox-event-router.html)
- [Kafka Exactly-Once Semantics](https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/)
