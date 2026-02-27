```mermaid
erDiagram
    CURRENCIES {
        uuid id PK
        varchar code UK "BRL, USD, EUR"
        varchar name
        varchar symbol
        varchar decimal_places
        timestamptz created_at
        timestamptz updated_at
    }

    EXCHANGE_RATES {
        uuid id PK
        uuid from_currency_id FK
        uuid to_currency_id FK
        numeric rate "precision 18,8"
        timestamptz effective_date
        timestamptz expires_at
        varchar source
        uint row_version "xmin — optimistic locking"
        timestamptz created_at
        timestamptz updated_at
    }

    CEDENTS {
        uuid id PK
        varchar name
        char cnpj UK "14 digits only"
        varchar contact_email
        boolean is_active
        uint row_version "xmin — optimistic locking"
        timestamptz created_at
        timestamptz updated_at
    }

    RECEIVABLES {
        uuid id PK
        uuid cedent_id FK
        varchar document_number UK
        varchar type "DuplicataMercantil | ChequePredatado"
        numeric face_value "precision 18,2"
        varchar face_currency "BRL | USD"
        timestamptz due_date
        timestamptz submitted_at
        timestamptz created_at
        timestamptz updated_at
    }

    SETTLEMENTS {
        uuid id PK
        uuid receivable_id FK
        varchar status "Pending | Settled | Cancelled | Failed"
        numeric face_value
        varchar face_currency
        numeric present_value "PV calculado"
        varchar payment_currency
        numeric exchange_rate_applied
        boolean is_cross_currency
        numeric effective_rate "base + spread"
        int term_in_months
        varchar settlement_reference UK
        timestamptz settled_at
        uint row_version "xmin — optimistic locking"
        timestamptz created_at
        timestamptz updated_at
    }

    SETTLEMENT_ITEMS {
        uuid id PK
        uuid settlement_id FK
        varchar item_type "FaceValue | Discount | ExchangeAdjustment | Fee"
        numeric amount
        varchar currency
        varchar description
        timestamptz created_at
    }

    OUTBOX_EVENTS {
        uuid id PK
        varchar event_type
        varchar aggregate_id
        varchar aggregate_type
        jsonb payload
        varchar status "Pending | Published | Failed"
        int retry_count
        timestamptz created_at
        timestamptz processed_at
    }

    CURRENCIES ||--o{ EXCHANGE_RATES : "from_currency_id"
    CURRENCIES ||--o{ EXCHANGE_RATES : "to_currency_id"
    CEDENTS    ||--o{ RECEIVABLES   : "cedent_id (1 cedente → N recebíveis)"
    RECEIVABLES ||--o| SETTLEMENTS  : "receivable_id (1 recebível → 0-1 liquidação)"
    SETTLEMENTS ||--o{ SETTLEMENT_ITEMS : "settlement_id (1 liquidação → N itens)"
```

## Descrição das Entidades

| Entidade | Responsabilidade |
|---|---|
| **CURRENCIES** | Catálogo de moedas suportadas (BRL, USD, EUR…). Seed mantido por migration. |
| **EXCHANGE_RATES** | Taxas de câmbio point-in-time. `xmin` garante concorrência otimista na atualização. |
| **CEDENTS** | Empresas cedentes que submetem recebíveis ao fundo. CNPJ único como chave natural. |
| **RECEIVABLES** | Títulos financeiros (duplicatas, cheques) submetidos para precificação. `document_number` único por cedente. |
| **SETTLEMENTS** | Registro imutável da liquidação após precificação. Guarda `present_value`, `exchange_rate_applied` e o status via máquina de estados. |
| **SETTLEMENT_ITEMS** | Ledger de parcelas da liquidação (valor face, desconto, ajuste cambial, taxas). Separação auditável. |
| **OUTBOX_EVENTS** | Padrão Transactional Outbox para publicação assíncrona de eventos de domínio sem Two-Phase Commit. |

## Notas de Precisão Numérica

- Valores monetários: `NUMERIC(18, 2)` — suficiente para negociações até R$ 999 trilhões com centavos exatos.
- Taxas de câmbio: `NUMERIC(18, 8)` — 8 casas decimais para pares exóticos sem perda de precisão.
- Todos os timestamps em UTC (`TIMESTAMPTZ`).
