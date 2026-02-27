# SRM Credit Engine

Plataforma de crédito multi-moeda para cessão de recebíveis, desenvolvida como implementação de nível **Expert/Staff/Principal** do case de engenharia SRM.

[![CI](https://github.com/your-org/srm-credit-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/srm-credit-engine/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-53%2F53-brightgreen)](apps/backend/tests)
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
- [Critérios de Aceite](#critérios-de-aceite)

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

**Spreads por tipo de recebível (alinhados ao case):**

| Tipo                  | Spread | Risco  |
|-----------------------|--------|--------|
| Cheque Pré-datado     | 2,5%   | Alto   |
| Duplicata Mercantil   | 1,5%   | Médio  |

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

| Serviço       | URL                          | Credenciais              |
|---------------|------------------------------|--------------------------|
| Frontend      | http://localhost:3000        | —                        |
| API / Scalar  | http://localhost:8080/scalar | —                        |
| Health Check  | http://localhost:8080/health | —                        |
| Seq (Logs UI) | http://localhost:8090        | —                        |
| Grafana       | http://localhost:3001        | admin / admin            |
| Prometheus    | http://localhost:9090        | —                        |
| PostgreSQL    | localhost:5433               | srm_user / srm_pass      |

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

API: `http://localhost:5229` | Docs: `http://localhost:5229/scalar`

### Frontend

```bash
cd apps/frontend
npm install
npm run dev
```

Frontend: `http://localhost:5173` (proxy `/api` → `localhost:5229` configurado em `vite.config.ts`)

---

## API Reference

Base URL: `http://localhost:8080/api/v1`

| Método | Endpoint                             | Auth | Descrição                    |
|--------|--------------------------------------|------|------------------------------|
| POST   | `/auth/token`                        | —    | Obtém JWT Bearer token       |
| POST   | `/cedents`                           | ✅   | Cadastra cedente             |
| GET    | `/cedents`                           | —    | Lista cedentes               |
| GET    | `/cedents/{id}`                      | —    | Detalhe do cedente           |
| PUT    | `/cedents/{id}`                      | ✅   | Atualiza cedente             |
| DELETE | `/cedents/{id}`                      | ✅   | Desativa cedente             |
| POST   | `/pricing/simulate`                  | —    | Simula precificação (PV)     |
| GET    | `/settlements`                       | —    | Lista liquidações (paginado) |
| POST   | `/settlements`                       | ✅   | Confirma liquidação          |
| GET    | `/settlements/{id}`                  | —    | Detalhe de liquidação        |
| GET    | `/exchange-rates/{from}/{to}`        | —    | Cotação atual                |
| PUT    | `/exchange-rates`                    | ✅   | Atualiza cotação             |
| GET    | `/reports/settlement-statement`      | —    | Extrato consolidado          |

Documentação interativa: `http://localhost:8080/scalar`

---

## Testes

**53/53 testes passando:**

### Backend — Unitários (38)

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

### Backend — Integração (9)

Usam **Testcontainers** (PostgreSQL real, sem mocks) e `WebApplicationFactory`.

| Suite                        | Testes | Cobertura                                        |
|------------------------------|--------|--------------------------------------------------|
| `AuthEndpointTests`          | 2      | Token válido / credenciais inválidas             |
| `PricingEndpointTests`       | 2      | Simulate correto / dueDate no passado            |
| `SettlementsEndpointTests`   | 5      | Criar / obter / sem token 401 / duplicata 409    |

```bash
cd apps/backend
dotnet test tests/SrmCreditEngine.IntegrationTests
# Passed! - Failed: 0, Passed: 9, Skipped: 0
```

### Frontend — Vitest (6)

Testa a página de Cedentes com `@testing-library/react`.

```bash
cd apps/frontend
npm test
# Test Files: 1 passed | Tests: 6 passed
```

### Todos os testes de uma vez

```bash
# Backend (unit + integration)
cd apps/backend && dotnet test

# Frontend
cd apps/frontend && npm test
```

### Cobertura de Código

```bash
# Backend — gera coverage.cobertura.xml em TestResults/unit/
cd apps/backend
dotnet test tests/SrmCreditEngine.UnitTests \
  --collect "XPlat Code Coverage" \
  --results-directory ./TestResults/unit

# Frontend — relatório em coverage/ (texto + lcov + html)
cd apps/frontend
npm run test:coverage
```

**Resultados atuais (medidos após build limpo):**

| Projeto | Escopo medido | Statements | Branches | Linhas |
|---|---|---|---|---|
| `SrmCreditEngine.Domain` | regras de negócio | — | — | **50,7%** |
| `SrmCreditEngine.Application` | use cases / strategies | — | — | 6,0% |
| **Backend total (solução)** | todas as camadas | — | **14,6%** | **21,8%** |
| `Cedents.tsx` | CRUD de cedentes | **54,5%** | **64,7%** | **56,3%** |
| **Frontend total** | todos os arquivos | 9,2% | 16,5% | 10,5% |

**Por que os totais parecem baixos?**

A estratégia de testes adotada é o **[Testing Trophy](https://kentcdodds.com/blog/the-testing-trophy-and-testing-classifications)** — cobertura concentrada onde o risco de negócio é maior:

- **Domain 50,7% de linhas** — cobre 100% das invariantes financeiras (`PricingStrategy`, `Settlement` state machine, `Money`). O restante são getters/ctors que não carregam lógica.
- **Application 6%** — os use cases são testados de ponta a ponta pelos 9 testes de integração (Testcontainers + WebApplicationFactory), que exercem camadas reais (banco, validação, middleware). Mockar tudo de novo em testes unitários seria duplicação sem valor.
- **Frontend 9,2% total / 54,5% em Cedents** — apenas a página de Cedentes tem testes de componente. As demais páginas (`SimulatorPanel`, `TransactionGrid`, `ExchangeRates`) foram verificadas via smoke test manual com a aplicação rodando, por serem fortemente acopladas a chamadas HTTP que exigiriam mocks completos da API para testes de componente.

Para atingir coberturas altas artificialmente sem aumentar a confiança real nos testes, seria necessário testar getters triviais e mocks de mocks — prática geralmente considerada anti-padrão em projetos financeiros onde o custo de manutenção dos testes supera o benefício.

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

### Kubernetes / IaC

Manifestos prontos para produção em `infra/k8s/` (Kustomize):

```
infra/k8s/
├── namespace.yaml          → Namespace srm-credit-engine
├── configmap.yaml          → Variáveis de ambiente não-secretas
├── backend-deployment.yaml → 2 réplicas, rolling update, probes /health
├── backend-service.yaml    → ClusterIP :8080
├── backend-hpa.yaml        → HPA: min 2 / max 10 pods (CPU 70%, MEM 80%)
├── frontend-deployment.yaml→ 2 réplicas nginx
├── frontend-service.yaml   → ClusterIP :80
├── postgres-statefulset.yaml → StatefulSet 1 réplica, PVC 20Gi
├── postgres-service.yaml   → Headless service para DNS do StatefulSet
├── ingress.yaml            → TLS (cert-manager), roteamento /api + /
└── kustomization.yaml      → Entry point: kubectl apply -k infra/k8s/
```

**Deploy:**
```bash
# Criar secrets (fora do controle de versão)
kubectl create secret generic srm-secrets \
  --from-literal=POSTGRES_PASSWORD=<senha> \
  --from-literal=JWT__SecretKey=<chave> \
  -n srm-credit-engine

# Aplicar todos os manifestos
kubectl apply -k infra/k8s/
```

**Escalabilidade:** o HPA ajusta automaticamente de 2 a 10 pods do backend com cooldown de 5 min no scale-down (AC-20).

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
v1.0.0  ← entrega inicial (pricing engine, settlements, frontend)
v1.0.1  ← hotfix: guard contra prazo ≤ 0 em ChequePredatado (cherry-pick para release/v1.0.x)
v1.1.0  ← JWT, Polly, CRUD Cedentes, Testcontainers, Vitest
v1.2.0  ← IaC (Kubernetes/Kustomize), critérios de aceite (24 ACs BDD)
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

## Critérios de Aceite

Definidos em [`docs/acceptance-criteria.md`](docs/acceptance-criteria.md) — 24 ACs em formato BDD (Given/When/Then) organizados em 5 eixos:

| Eixo | ACs | Exemplos |
|---|---|---|
| **Usabilidade** | AC-01 – AC-06 | Simulação em tempo real (debounce 700 ms), select de cedente, filtro por cedente, câmbio em tempo real |
| **Segurança** | AC-07 – AC-13 | JWT 401 sem token, 401 em token expirado, rate limiting 429, validação CNPJ, otimistic locking 409 |
| **Desempenho** | AC-14 – AC-17 | P95 < 100 ms, Dapper 100 k linhas < 500 ms, FCP < 1,5 s, health check < 50 ms |
| **Escalabilidade** | AC-18 – AC-21 | Backend stateless, partition pruning, HPA K8s (ref: `backend-hpa.yaml`), circuit breaker |
| **Rastreabilidade** | AC-22 – AC-24 | Structured logs (Seq), audit trail, versionamento semântico |

---

## Ferramentas Recomendadas (VS Code)

Os diagramas neste repositório usam a sintaxe **Mermaid** (arquivos `.md` em `docs/diagrams/` e `er-diagram.md`). Para visualizá-los diretamente no VS Code:

1. Instale a extensão [Markdown Preview Mermaid Support](https://marketplace.visualstudio.com/items?itemName=bierner.markdown-mermaid)
   ```
   ext install bierner.markdown-mermaid
   ```
2. Abra qualquer `.md` com diagrama e pressione `Ctrl+Shift+V` para o preview renderizado.

---

## Licença

MIT — ver [LICENSE](LICENSE)
