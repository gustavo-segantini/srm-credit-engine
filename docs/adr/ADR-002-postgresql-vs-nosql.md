# ADR-002: PostgreSQL como Banco de Dados Relacional

| Campo          | Valor                       |
|----------------|-----------------------------|
| **Status**     | Aceito                      |
| **Data**       | 2025-01-01                  |
| **Contexto**   | Escolha do mecanismo de persistência |

---

## Contexto

A plataforma precisa persistir:
1. **Dados transacionais** — liquidações, recebíveis, cedentes (ACID obrigatório)
2. **Taxas de câmbio** — atualizações frequentes com concorrência otimista
3. **Relatórios analíticos** — agregações sobre milhares de liquidações por período

O sistema também precisa prevenir *double-settlement* (liquidação duplicada do mesmo recebível).

## Decisão

Adotar **PostgreSQL 16** como único mecanismo de persistência.

## Raciocínio

**Por PostgreSQL:**
- **ACID completo** com isolamento `READ COMMITTED` por padrão e suporte a `SERIALIZABLE` para operações críticas
- **`xmin` como token de concorrência otimista** — coluna de sistema atualizada automaticamente a cada escrita; elimina necessidade de coluna extra `row_version`
- **Índices parciais e compostos** — `unique index on (document_number, cedent_id)` previne duplicatas a nível de banco mesmo com race conditions
- **`TIMESTAMPTZ`** — armazena datas sempre em UTC; sem risco de ambiguidade de timezone
- **Sequências e `SERIAL`** — geração eficiente de IDs sem lock de aplicação
- **`pg_stat_statements`** — observabilidade de queries sem instrumentação adicional
- **Extensões**: `pgvector` (futuro similarity search em cedentes), `timescaledb` (futuro time-series de cotações)

**Concorrência otimista via `xmin`:**
```sql
-- EF Core configuração
builder.Property<uint>("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```
A cada `UPDATE`, PostgreSQL incrementa automaticamente o `xmin`. O EF Core verifica que o valor lido é igual ao atual antes de aplicar o `UPDATE`; caso contrário, lança `DbUpdateConcurrencyException` (mapeada para HTTP 409).

**Alternativas rejeitadas:**

| Alternativa         | Motivo da rejeição                                                   |
|---------------------|----------------------------------------------------------------------|
| MongoDB             | Transações multi-documento têm custo; sem `JOIN` nativo; tipagem fraca |
| SQL Server          | Licença proprietária; custo em cloud                                  |
| MySQL               | Menor suporte a tipos avançados (`xmin`, `TIMESTAMPTZ`, JSON)         |
| Redis (primário)    | Volatilidade; inadequado para dados financeiros como store principal   |
| EventStore / Kafka  | Adiciona complexidade CQRS/ES desnecessária no MVP                    |

## Consequências

**Positivas:**
- Prevenção de `double-settlement` a nível de banco (`UNIQUE` index + `xmin`)
- Sem dependência de lock distribuído para concorrência otimista
- Schema evolution via EF Core Migrations com rollback `dotnet ef migrations remove`

**Negativas:**
- Vertical scaling limitado a ~100k conexões (mitigável via `PgBouncer`)
- Para escala horizontal de escrita (1M tx/min), sharding is needed (ver `docs/high-scale-design.md`)

## Referências

- [PostgreSQL xmin Concurrency](https://www.npgsql.org/efcore/optimistic-concurrency.html)
- [Npgsql EF Core Provider](https://github.com/npgsql/efcore.pg)
