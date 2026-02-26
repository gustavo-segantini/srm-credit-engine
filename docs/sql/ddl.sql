-- =============================================================================
-- SRM Credit Engine — DDL (Data Definition Language)
-- Gerado com base no EF Core Migration: 20260226005905_InitialCreate
-- Banco: PostgreSQL 16
-- Encoding: UTF-8
-- =============================================================================

-- -----------------------------------------------------------------------------
-- EXTENSIONS
-- -----------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";  -- query observability

-- -----------------------------------------------------------------------------
-- ENUMS
-- -----------------------------------------------------------------------------

CREATE TYPE receivable_type AS ENUM (
    'Cheque',
    'Duplicata',
    'CRI',
    'CRA',
    'Debenture'
);

CREATE TYPE currency_code AS ENUM (
    'BRL',
    'USD',
    'EUR',
    'GBP'
);

CREATE TYPE settlement_status AS ENUM (
    'Pending',
    'Settled',
    'Failed',
    'Cancelled'
);

-- -----------------------------------------------------------------------------
-- CEDENTS (Cedentes)
-- -----------------------------------------------------------------------------

CREATE TABLE cedents (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    name            TEXT            NOT NULL,
    document_number TEXT            NOT NULL,   -- CNPJ
    email           TEXT,
    phone           TEXT,
    credit_limit    NUMERIC(18, 4)  NOT NULL,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ,
    xmin            xid,                        -- optimistic concurrency token

    CONSTRAINT pk_cedents PRIMARY KEY (id)
);

CREATE UNIQUE INDEX uix_cedents_document_number
    ON cedents (document_number);

CREATE INDEX ix_cedents_is_active
    ON cedents (is_active)
    WHERE is_active = TRUE;

COMMENT ON TABLE cedents IS 'Empresas cedentes de recebíveis';
COMMENT ON COLUMN cedents.document_number IS 'CNPJ no formato 00.000.000/0000-00';
COMMENT ON COLUMN cedents.xmin IS 'Token de concorrência otimista — coluna de sistema PostgreSQL';

-- -----------------------------------------------------------------------------
-- RECEIVABLES (Recebíveis)
-- -----------------------------------------------------------------------------

CREATE TABLE receivables (
    id                UUID            NOT NULL DEFAULT gen_random_uuid(),
    cedent_id         UUID            NOT NULL,
    document_number   TEXT            NOT NULL,
    receivable_type   receivable_type NOT NULL,
    face_value_amount NUMERIC(18, 4)  NOT NULL,
    face_value_currency currency_code NOT NULL,
    due_date          TIMESTAMPTZ     NOT NULL,
    is_settled        BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at        TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_receivables PRIMARY KEY (id),
    CONSTRAINT fk_receivables_cedents
        FOREIGN KEY (cedent_id) REFERENCES cedents (id)
        ON DELETE RESTRICT
);

CREATE UNIQUE INDEX uix_receivables_document_cedent
    ON receivables (document_number, cedent_id);

CREATE INDEX ix_receivables_cedent_id
    ON receivables (cedent_id);

CREATE INDEX ix_receivables_is_settled
    ON receivables (is_settled)
    WHERE is_settled = FALSE;

CREATE INDEX ix_receivables_due_date
    ON receivables (due_date);

COMMENT ON TABLE receivables IS 'Recebíveis elegíveis para cessão';
COMMENT ON COLUMN receivables.face_value_amount IS 'Valor de face (nominal) do recebível';
COMMENT ON COLUMN receivables.is_settled IS 'TRUE quando o recebível foi liquidado — previne double-settlement';

-- -----------------------------------------------------------------------------
-- SETTLEMENTS (Liquidações)
-- -----------------------------------------------------------------------------

CREATE TABLE settlements (
    id                          UUID              NOT NULL DEFAULT gen_random_uuid(),
    cedent_id                   UUID              NOT NULL,
    status                      settlement_status NOT NULL DEFAULT 'Pending',
    payment_currency            currency_code     NOT NULL,
    total_face_value_amount     NUMERIC(18, 4)    NOT NULL,
    total_face_value_currency   currency_code     NOT NULL,
    net_disbursement_amount     NUMERIC(18, 4),
    net_disbursement_currency   currency_code,
    total_discount_amount       NUMERIC(18, 4),
    total_discount_currency     currency_code,
    exchange_rate               NUMERIC(18, 8),
    operator_id                 TEXT              NOT NULL,
    notes                       TEXT,
    created_at                  TIMESTAMPTZ       NOT NULL DEFAULT NOW(),
    settled_at                  TIMESTAMPTZ,
    failed_at                   TIMESTAMPTZ,
    cancelled_at                TIMESTAMPTZ,
    failure_reason              TEXT,
    xmin                        xid,              -- optimistic concurrency token

    CONSTRAINT pk_settlements PRIMARY KEY (id),
    CONSTRAINT fk_settlements_cedents
        FOREIGN KEY (cedent_id) REFERENCES cedents (id)
        ON DELETE RESTRICT,
    CONSTRAINT chk_settlements_net_disbursement
        CHECK (net_disbursement_amount IS NULL OR net_disbursement_amount >= 0),
    CONSTRAINT chk_settlements_exchange_rate
        CHECK (exchange_rate IS NULL OR exchange_rate > 0)
) PARTITION BY RANGE (created_at);

-- Partições mensais (criadas via script de manutenção ou pg_partman)
CREATE TABLE settlements_2025_01 PARTITION OF settlements
    FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');

CREATE TABLE settlements_2025_02 PARTITION OF settlements
    FOR VALUES FROM ('2025-02-01') TO ('2025-03-01');

CREATE TABLE settlements_2025_03 PARTITION OF settlements
    FOR VALUES FROM ('2025-03-01') TO ('2025-04-01');

CREATE TABLE settlements_2025_04 PARTITION OF settlements
    FOR VALUES FROM ('2025-04-01') TO ('2025-05-01');

CREATE TABLE settlements_2025_05 PARTITION OF settlements
    FOR VALUES FROM ('2025-05-01') TO ('2025-06-01');

CREATE TABLE settlements_2025_06 PARTITION OF settlements
    FOR VALUES FROM ('2025-06-01') TO ('2025-07-01');

CREATE TABLE settlements_2025_07 PARTITION OF settlements
    FOR VALUES FROM ('2025-07-01') TO ('2025-08-01');

CREATE TABLE settlements_2025_08 PARTITION OF settlements
    FOR VALUES FROM ('2025-08-01') TO ('2025-09-01');

CREATE TABLE settlements_2025_09 PARTITION OF settlements
    FOR VALUES FROM ('2025-09-01') TO ('2025-10-01');

CREATE TABLE settlements_2025_10 PARTITION OF settlements
    FOR VALUES FROM ('2025-10-01') TO ('2025-11-01');

CREATE TABLE settlements_2025_11 PARTITION OF settlements
    FOR VALUES FROM ('2025-11-01') TO ('2025-12-01');

CREATE TABLE settlements_2025_12 PARTITION OF settlements
    FOR VALUES FROM ('2025-12-01') TO ('2026-01-01');

CREATE INDEX ix_settlements_cedent_id
    ON settlements (cedent_id);

CREATE INDEX ix_settlements_status
    ON settlements (status);

CREATE INDEX ix_settlements_created_at
    ON settlements (created_at DESC);

CREATE INDEX ix_settlements_operator_id
    ON settlements (operator_id);

COMMENT ON TABLE settlements IS 'Liquidações de recebíveis — particionada por mês de criação';
COMMENT ON COLUMN settlements.xmin IS 'Token de concorrência otimista — previne conflito em atualizações concorrentes';
COMMENT ON COLUMN settlements.exchange_rate IS 'Taxa de câmbio snapshot no momento da liquidação';

-- -----------------------------------------------------------------------------
-- SETTLEMENT ITEMS (Itens da liquidação)
-- -----------------------------------------------------------------------------

CREATE TABLE settlement_items (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    settlement_id       UUID            NOT NULL,
    receivable_id       UUID            NOT NULL,
    face_value_amount   NUMERIC(18, 4)  NOT NULL,
    face_value_currency currency_code   NOT NULL,
    present_value_amount NUMERIC(18, 4) NOT NULL,
    present_value_currency currency_code NOT NULL,
    discount_amount     NUMERIC(18, 4)  NOT NULL,
    discount_currency   currency_code   NOT NULL,
    applied_spread      NUMERIC(8, 6)   NOT NULL,
    term_in_months      INT             NOT NULL,

    CONSTRAINT pk_settlement_items PRIMARY KEY (id),
    CONSTRAINT fk_settlement_items_settlements
        FOREIGN KEY (settlement_id) REFERENCES settlements (id)
        ON DELETE CASCADE,
    CONSTRAINT fk_settlement_items_receivables
        FOREIGN KEY (receivable_id) REFERENCES receivables (id)
        ON DELETE RESTRICT,
    CONSTRAINT uix_settlement_items_receivable
        UNIQUE (settlement_id, receivable_id)
);

CREATE INDEX ix_settlement_items_settlement_id
    ON settlement_items (settlement_id);

CREATE INDEX ix_settlement_items_receivable_id
    ON settlement_items (receivable_id);

COMMENT ON TABLE settlement_items IS 'Itens individuais de cada liquidação (1 por recebível)';
COMMENT ON COLUMN settlement_items.applied_spread IS 'Spread aplicado no cálculo do PV — snapshot do momento da liquidação';

-- -----------------------------------------------------------------------------
-- EXCHANGE RATES (Taxas de câmbio)
-- -----------------------------------------------------------------------------

CREATE TABLE exchange_rates (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    from_currency   currency_code   NOT NULL,
    to_currency     currency_code   NOT NULL,
    rate            NUMERIC(18, 8)  NOT NULL,
    effective_at    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_by      TEXT            NOT NULL,
    xmin            xid,            -- optimistic concurrency token

    CONSTRAINT pk_exchange_rates PRIMARY KEY (id),
    CONSTRAINT chk_exchange_rates_rate
        CHECK (rate > 0),
    CONSTRAINT chk_exchange_rates_different_currencies
        CHECK (from_currency != to_currency)
);

CREATE UNIQUE INDEX uix_exchange_rates_pair
    ON exchange_rates (from_currency, to_currency);

COMMENT ON TABLE exchange_rates IS 'Taxas de câmbio atuais por par de moedas';
COMMENT ON COLUMN exchange_rates.rate IS 'Quantos to_currency equivalem a 1 from_currency';
COMMENT ON COLUMN exchange_rates.xmin IS 'Token de concorrência otimista';

-- -----------------------------------------------------------------------------
-- OUTBOX EVENTS (Para EDA — Fase 2)
-- -----------------------------------------------------------------------------

CREATE TABLE outbox_events (
    id              UUID        NOT NULL DEFAULT gen_random_uuid(),
    aggregate_type  TEXT        NOT NULL,
    aggregate_id    UUID        NOT NULL,
    event_type      TEXT        NOT NULL,
    payload         JSONB       NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    published_at    TIMESTAMPTZ,
    retry_count     INT         NOT NULL DEFAULT 0,

    CONSTRAINT pk_outbox_events PRIMARY KEY (id),
    CONSTRAINT chk_outbox_max_retries CHECK (retry_count <= 10)
);

CREATE INDEX ix_outbox_unpublished
    ON outbox_events (created_at)
    WHERE published_at IS NULL AND retry_count < 5;

COMMENT ON TABLE outbox_events IS 'Outbox Pattern — eventos pendentes de publicação para broker (Fase 2)';

-- -----------------------------------------------------------------------------
-- SEED DATA — Exchange Rates iniciais
-- -----------------------------------------------------------------------------

INSERT INTO exchange_rates (from_currency, to_currency, rate, updated_by) VALUES
    ('USD', 'BRL', 4.9500, 'system-seed'),
    ('EUR', 'BRL', 5.4200, 'system-seed'),
    ('GBP', 'BRL', 6.2800, 'system-seed'),
    ('BRL', 'USD', 0.2020, 'system-seed'),
    ('BRL', 'EUR', 0.1845, 'system-seed'),
    ('BRL', 'GBP', 0.1592, 'system-seed'),
    ('EUR', 'USD', 1.0950, 'system-seed'),
    ('USD', 'EUR', 0.9132, 'system-seed')
ON CONFLICT (from_currency, to_currency) DO NOTHING;

-- =============================================================================
-- END OF DDL
-- =============================================================================
