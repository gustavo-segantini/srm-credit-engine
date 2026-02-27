# AI_USAGE.md

Documento de transparência sobre o uso de ferramentas de IA durante o desenvolvimento do SRM Credit Engine.

---

## Ferramentas Utilizadas

| Ferramenta      | Versão / Modelo     | Uso                                               |
|-----------------|---------------------|---------------------------------------------------|
| GitHub Copilot  | Claude Sonnet 4.6   | Geração de código, scaffolding, debugging, docs   |
| GitHub Copilot  | GPT-4o (Edits)      | Refatorações pontuais e sugestões inline          |

---

## O Que Foi Gerado com Assistência de IA

### Estrutura e Scaffolding

- Estrutura de diretórios do monorepo (`apps/backend`, `apps/frontend`, `infra/`, `docs/`)
- Arquivos `.csproj` de todos os projetos com referências entre camadas
- `docker-compose.yml` e `docker-compose.override.yml`
- `Dockerfile` do backend (.NET 10 multi-stage) e do frontend (nginx)
- Pipeline CI/CD (`.github/workflows/ci.yml`)

### Código de Backend

- Entidades de domínio (`Settlement`, `Receivable`, `Cedent`, `ExchangeRate`) com regras de negócio encapsuladas
- Value Objects (`Money`, `CurrencyCode`, `ReceivableType`)
- Domain Services (`PricingDomainService`) com fórmula de desconto a VP
- Application Handlers (Command/Query) com FluentValidation
- EF Core Configurations (todas as entidades, mapeamento `xmin`, relacionamentos)
- Dapper query para relatório de extrato

### Código de Frontend

- Tipos TypeScript (`src/types/index.ts`) mapeando DTOs do backend
- Serviço Axios (`src/services/api.ts`) com interceptor de erros RFC 7807
- Hooks TanStack Query (`usePricingSimulation`, `useSettlements`)
- Zustand store para moeda de pagamento selecionada
- Páginas: `OperatorPanel`, `TransactionGrid`, `ExchangeRates`
- Layout com navegação responsiva

### Testes

- Suite completa de 38 testes unitários cobrindo:
  - `PricingDomainService` (11 casos)
  - `Settlement` state machine (12 casos)
  - `Money` value object (15 casos)
- Padrão AAA (Arrange, Act, Assert) com FluentAssertions + NSubstitute

### Documentação

- ADR-001, ADR-002, ADR-003 (Architecture Decision Records)
- Diagramas C4 Level 1 e Level 2 em Mermaid
- `docs/high-scale-design.md` — evolução arquitetural para 1M tx/min
- `docs/eda-proposal.md` — proposta Event-Driven com Kafka + Outbox Pattern
- `docs/sql/ddl.sql` — DDL completo para revisão de DBA
- `README.md` — documentação completa do projeto
- `.editorconfig`, `.gitignore`
- Configurações de observabilidade (Prometheus, Grafana dashboards)

---

## O Que Foi Desenvolvido com Julgamento Humano

As seguintes decisões **não foram automaticamente geradas** — exigiram validação e direção explícita:

### Decisões Arquiteturais

1. **Monólito modular vs microservices** — escolha deliberada de começar com monólito para reduzir overhead operacional no MVP, com plano documentado de extração
2. **EF Core 9.x + pin da versão** — conflito de versões (Npgsql 9.0.4 incompatível com EF Core 10.x) exigiu diagnóstico manual e pinagem explícita de `Microsoft.EntityFrameworkCore.Design@9.0.2`
3. **Mapeamento `xmin` por property configuration explícita** — `UseXminAsConcurrencyToken()` não resolvia no projeto; substituído por `HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`
4. **Remoção de `AddDbContextCheck`** — pacote `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` puxava EF Core 10.x; removido intencionalmente para manter compatibilidade
5. **Spreads por tipo de recebível** — valores definidos com base no enunciado do case (Cheque > Duplicata > Debenture > CRI > CRA) refletindo risco relativo de cada instrumento

### Decisões de Qualidade

1. **Estrutura dos testes** — organização por triângulo de testes (unit only para MVP), cobertura focada em invariantes de domínio
2. **Correções de FluentAssertions 8.x** — `BeLessOrEqualTo` renomeado, asserção de `Code` em exception como `.Which.Code.Should().Be()` (não `WithMessage`)
3. **Particionamento PostgreSQL por mês** — decisão de incluir no DDL como preparação para escala futura

---

## Qualidade e Revisão

Todo código gerado foi:

1. **Revisado manualmente** antes de integração
2. **Compilado e testado** — `dotnet build` sem erros, `dotnet test` 38/38 passando
3. **Validado contra o case** — requisitos funcionais do `README_case_dev_srm.md` verificados linha a linha
4. **Iterado quando incorreto** — ex: duplicate class em PricingStrategyTests detectado e corrigido manualmente

---

## Limitações Identificadas

- **Kafka / Outbox Pattern** descrito em `docs/eda-proposal.md` mas não implementado no código — identificado como Fase 2 deliberadamente, pois adicionar um broker de mensagens ao MVP aumentaria a complexidade operacional sem benefício proporcional em escala inicial
- **IaC (Terraform / Kubernetes)** mencionado como opcional no case; não implementado — Docker Compose cobre o escopo local e de avaliação
- **Cherry-pick de hotfix** como alternativa ao `git revert` — demonstrado via revert (abordagem mais segura para branches compartilhadas); cherry-pick documentado como opção no README

---

## Estimativa de Produtividade

| Atividade                     | Sem IA (estimado) | Com IA    | Aceleração |
|-------------------------------|-------------------|-----------|------------|
| Scaffolding + boilerplate     | 4h                | 30min     | 8x         |
| Implementação domain layer    | 6h                | 1,5h      | 4x         |
| Implementação infra layer     | 8h                | 2h        | 4x         |
| Testes unitários (38)         | 6h                | 1h        | 6x         |
| Documentação (ADRs + C4)      | 4h                | 45min     | 5x         |
| **Total estimado**            | **~28h**          | **~6h**   | **~4,5x**  |

A aceleração não elimina a necessidade de conhecimento técnico profundo — sem entendimento de EF Core versioning, xmin semantics, e Clean Architecture, os erros de compilação não teriam sido resolvidos.
