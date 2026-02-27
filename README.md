# SRM Credit Engine

Plataforma de crédito multi-moeda para cessão de recebíveis, desenvolvida como implementação de nível **Expert/Staff/Principal** do case de engenharia SRM.

[![CI](https://github.com/your-org/srm-credit-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/srm-credit-engine/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-38%2F38-brightgreen)](apps/backend/tests)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)
[![React](https://img.shields.io/badge/React-19-61DAFB)](https://react.dev)

---

## Sumário

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Quick Start (Docker)](#quick-start-docker)
- [Desenvolvimento Local](#desenvolvimento-local)
- [API Reference](#api-reference)
- [Testes](#testes)
- [Infraestrutura & Observabilidade](#infraestrutura--observabilidade)
- [Documentação Técnica](#documentação-técnica)
- [Branching Strategy](#branching-strategy)

---

## Visão Geral

O SRM Credit Engine permite que operadores de mesa:

1. **Simulem a precificação** de recebíveis (Cheque, Duplicata, CRI, CRA, Debenture) calculando Valor Presente com spread por tipo
2. **Confirmem liquidações** multi-moeda (BRL/USD/EUR/GBP) com conversão cambial em tempo real
3. **Acompanhem o grid** de liquidações com paginação server-side e filtros
4. **Gerenciem taxas de câmbio** com controle de concorrência otimista

### Fórmula de Precificação

```
PV = FV / (1 + r)^n
```

`r = taxa_base + spread_tipo` (mensal) · `n` = prazo em meses

**Spreads por tipo de recebível:**

| Tipo       | Spread | Risco         |
|------------|--------|---------------|
| Cheque     | 3,5%   | Alto          |
| Duplicata  | 2,5%   | Médio         |
| Debenture  | 1,8%   | Médio-Baixo   |
| CRI        | 1,2%   | Baixo         |
| CRA        | 1,0%   | Baixo         |

---

## Arquitetura

Clean Architecture em 4 camadas:

```
API Layer         → Controllers, Middleware, Program.cs
Application Layer → Use Cases, Handlers, Validators, DTOs
Domain Layer      → Entities, Value Objects, Domain Services
Infrastructure    → EF Core, Dapper, Repositories, UoW
```

**Documentação completa de arquitetura:**
- [ADR-001: .NET 10 + Clean Architecture](docs/adr/ADR-001-net10-clean-arch.md)
- [ADR-002: PostgreSQL vs NoSQL](docs/adr/ADR-002-postgresql-vs-nosql.md)
- [ADR-003: Monólito vs Microservices](docs/adr/ADR-003-monolith-vs-microservices.md)
- [C4 Context Diagram](docs/diagrams/c4-context.md)
- [C4 Container Diagram](docs/diagrams/c4-container.md)
- [High-Scale Design (1M tx/min)](docs/high-scale-design.md)
- [EDA Proposal](docs/eda-proposal.md)
- [DDL SQL](docs/sql/ddl.sql)

---

## Quick Start (Docker)

**Pré-requisitos:** Docker 24+ e Docker Compose 2.20+

```bash
git clone https://github.com/your-org/srm-credit-engine.git
cd srm-credit-engine
docker compose up -d
```

**Serviços disponíveis:**

| Serviço       | URL                          | Credenciais   |
|---------------|------------------------------|---------------|
| Frontend      | http://localhost:3000        | —             |
| API / Scalar  | http://localhost:8080/scalar | —             |
| Health Check  | http://localhost:8080/health | —             |
| Seq (Logs)    | http://localhost:5341        | —             |
| Grafana       | http://localhost:3001        | admin / admin |
| Prometheus    | http://localhost:9090        | —             |
| PostgreSQL    | localhost:5432               | srm / srm123! |

```bash
docker compose down -v   # para e remove volumes
```

---

## Desenvolvimento Local

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22 LTS](https://nodejs.org/en/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- dotnet-ef: `dotnet tool install -g dotnet-ef`

### Backend

```bash
docker compose up -d postgres seq

cd apps/backend
dotnet restore
dotnet ef database update \
    --project src/SrmCreditEngine.Infrastructure \
    --startup-project src/SrmCreditEngine.API
dotnet watch run --project src/SrmCreditEngine.API
```

API: `http://localhost:8080` | Docs: `http://localhost:8080/scalar`

### Frontend

```bash
cd apps/frontend
npm install
npm run dev
```

Frontend: `http://localhost:5173` (proxy `/api` → `localhost:8080` configurado em `vite.config.ts`)

---

## API Reference

Base URL: `http://localhost:8080/api/v1`

| Método | Endpoint                      | Descrição                    |
|--------|-------------------------------|------------------------------|
| POST   | `/pricing/simulate`           | Simula precificação (PV)     |
| GET    | `/settlements`                | Lista liquidações (paginado) |
| POST   | `/settlements`                | Confirma liquidação          |
| GET    | `/settlements/{id}`           | Detalhe de liquidação        |
| GET    | `/exchange-rates/{from}/{to}` | Cotação atual                |
| PUT    | `/exchange-rates`             | Atualiza cotação             |
| GET    | `/reports/statement`          | Extrato consolidado          |

Documentação interativa: `http://localhost:8080/scalar`

---

## Testes

**38/38 testes unitários passando:**

| Suite                     | Testes | Cobertura                             |
|---------------------------|--------|---------------------------------------|
| `PricingStrategyTests`    | 11     | Fórmula PV, spreads, casos de borda   |
| `SettlementEntityTests`   | 12     | Máquina de estados, transições        |
| `MoneyValueObjectTests`   | 15     | Aritmética, conversão, arredondamento |
| **Total**                 | **38** | **100% regras domain**                |

```bash
cd apps/backend
dotnet test tests/SrmCreditEngine.UnitTests
# Passed! - Failed: 0, Passed: 38, Skipped: 0
```

---

## Infraestrutura & Observabilidade

**Stack Docker Compose:**

```
postgres:16      → Banco relacional
datalust/seq     → Log aggregator (Serilog)
srm_api          → ASP.NET Core 10
srm_frontend     → React SPA (nginx)
prom/prometheus  → Scraping /metrics a cada 10s
grafana:11.2     → Dashboards RED metrics
```

**Grafana Dashboard (pré-provisionado em `infra/monitoring/`):**
- Request Rate / Error Rate por status code
- Latência P95 / P99
- Breakdown por rota

---

## Documentação Técnica

```
docs/
├── adr/
│   ├── ADR-001-net10-clean-arch.md
│   ├── ADR-002-postgresql-vs-nosql.md
│   └── ADR-003-monolith-vs-microservices.md
├── diagrams/
│   ├── c4-context.md
│   └── c4-container.md
├── sql/
│   └── ddl.sql
├── high-scale-design.md
└── eda-proposal.md
```

---

## Branching Strategy

**Escolha: Trunk-Based Development (TBD)**

O TBD foi escolhido por dois motivos alinhados ao contexto do projeto:

1. **Ciclo de feedback curto** — feature branches vivem menos de 1 dia; conflitos de merge praticamente não existem.
2. **CI obrigatório** — cada branch deve passar no pipeline antes de mergear na `main`, tornando a `main` sempre deployável.

```
main  ←── feat/*, fix/*, chore/*, test/*, docs/* via merge --no-ff + PR
```

### Fluxo de Feature Branch

O histórico deste repositório demonstra o fluxo completo:

```
main ─────────────────────────────────────────────────────────────────▶
       │              │               │               │
       └─ feat/polly  └─ feat/cedent  └─ test/vitest  └─ ...
             │               │               │
           commit           commit          commit
             │               │               │
       merge --no-ff   merge --no-ff    merge --no-ff
```

Branches criados neste projeto (visíveis no `git log --graph`):

| Branch | Tipo | Descrição |
|---|---|---|
| `chore/husky-git-hooks` | chore | Husky v9 + lint-staged |
| `docs/er-diagram` | docs | Diagrama ER Mermaid |
| `feat/polly-resilience` | feat | Retry + Circuit Breaker |
| `feat/jwt-auth-rate-limiting` | feat | JWT Bearer + Rate Limiting |
| `feat/cedent-crud` | feat | CRUD de Cedentes |
| `feat/receivables-endpoint` | feat | GET /receivables |
| `feat/frontend-cedents` | feat | Página Cedentes (React) |
| `test/vitest-setup` | test | Vitest + Testing Library |
| `test/integration-tests` | test | Testcontainers |

---

### Gestão de Crise — `git revert` em Produção

**Incidente simulado — 2026-02-26T20:00:00Z**

Um bug crítico foi introduzido na branch `main` via commit `232435c`:

```diff
- private const decimal SpreadMonthly = 0.015m; // 1.5% a.m.
+ private const decimal SpreadMonthly = 0.0m; // BUG: spread zeroed
```

**Impacto:** O fundo passou a precificar todas as Duplicatas Mercantis com spread **0%**, comprando recebíveis sem desconto — margem financeira zerada em todas as novas operações.

**Resolução:** Em vez de `git reset --hard` (que reescreve histórico público e causa `force push`), foi utilizado `git revert` para criar um commit de desfazimento auditável:

```bash
# Identificar o commit culpado
git log --oneline | grep "Q1 performance"
# → 232435c feat(pricing): adjust duplicata spread for Q1 performance review

# Reverter de forma segura (preserva histórico completo)
git revert 232435c --no-edit
# → dd8b6d3 Revert "feat(pricing): adjust duplicata spread..."
```

**Por que `git revert` e não `git reset`?**

| Abordagem | Reescreve histórico público? | Seguro para branches compartilhados? |
|---|---|---|
| `git reset --hard` + force push | ✅ Sim — apaga o commit | ❌ Não — quebra histórico dos outros |
| `git revert` | ❌ Não — adiciona commit de desfazimento | ✅ Sim — safe para `main` |

O log final conta a história completa do incidente, preservando rastreabilidade para auditoria regulatória (FIDC/CVM):

```
dd8b6d3  Revert "feat(pricing): adjust duplicata spread for Q1 performance review"
232435c  feat(pricing): adjust duplicata spread for Q1 performance review  ← BUG
b1914ea  chore: add frontend-level Husky symlink
3490c71  Merge branch 'test/integration-tests'
...
```

---

### Tags Semânticas

```bash
v1.0.0  ← entrega inicial (feat completo: pricing engine, settlements, frontend)
v1.1.0  ← pós-rewrite: JWT, Polly, CRUD Cedentes, Testcontainers, Vitest
```

---

### Interactive Rebase — Higiene do Histórico

Antes de cada merge para `main`, o histórico da feature branch é organizado com `git rebase -i`:

```bash
# Squash de commits de fix/wip antes do merge
git rebase -i origin/main

# Exemplo: múltiplos commits de uma feature transformados em 1 atômico
# pick a1b2c3 feat(cedents): add domain entity
# squash d4e5f6 fix: validator typo
# squash 789abc test: add unit test
# → resultado: 1 commit limpo com mensagem final consolidada
```

Regra do time: **nenhum commit `wip:`, `tmp:` ou `fix typo` chega ao histórico da `main`**.

---

## Licença

MIT — ver [LICENSE](LICENSE)
