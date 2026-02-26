# ADR-003 — Monólito Modular vs Microservices

| Campo       | Valor                              |
|-------------|------------------------------------|
| **Status**  | Accepted                           |
| **Data**    | 2026-02-25                         |
| **Autores** | SRM Credit Engine Team             |
| **Tags**    | arquitetura, deployment, evolução  |

---

## Contexto

O SRM Credit Engine é o sistema de precificação e liquidação de recebíveis da SRM. As principais forças em tensão no momento da decisão de arquitetura de deployment foram:

- **Complexidade de domínio**: regras de precificação (PV, juros compostos, câmbio), máquina de estados de liquidação e relatórios fazem parte de um único Bounded Context bem definido.
- **Tamanho atual da equipe**: 2–4 engenheiros full-stack, sem squads dedicados por capacidade.
- **Requisitos de consistência**: a criação de uma liquidação e a consulta de precificação devem ser **transacionalmente consistentes** — o PV calculado no simulate deve ser idêntico ao registrado no ledger.
- **Horizonte de crescimento**: o case descreve uma perspectiva de escala para 1 milhão de transações/minuto, mas esse volume não existe no MVP.
- **Operações**: não há equipe de SRE dedicada; manter múltiplos serviços independentes com service mesh, distributed tracing e gestão de secrets seria overhead desproporcional ao tamanho atual.

## Decisão

**Adotar Monólito Modular (Modular Monolith) como ponto de partida, com plano explícito de extração incremental para microserviços conforme critérios objetivos forem atingidos.**

A solução atual é estruturada em Clean Architecture com separação rigorosa de camadas e limites de assembly:

```
SrmCreditEngine.Domain          ← Núcleo — entidades, value objects, regras
SrmCreditEngine.Application     ← Use cases, interfaces, DTOs
SrmCreditEngine.Infrastructure  ← EF Core, Dapper, repositórios, UoW
SrmCreditEngine.API             ← Controllers, middleware, DI bootstrap
```

As camadas comunicam-se **apenas por interfaces**, nunca por referência direta entre projetos não adjacentes. Essa disciplina garante que a extração futura de qualquer módulo seja uma decisão de infraestrutura, não uma refatoração de domínio.

---

## Alternativas Consideradas

| Opção | Prós | Contras | Decisão |
|---|---|---|---|
| **Microserviços desde o início** | Escalabilidade independente, deploys isolados | Overhead de rede, distributed transactions, infra complexa, team size insuficiente | ❌ Rejeitado |
| **Monólito Modular (escolhido)** | Deploy simples, transações ACID locais, onboarding rápido, refatoração segura | Escalabilidade horizontal limitada a nível de instância | ✅ Aceito |
| **Monólito Tradicional (big ball of mud)** | Nenhum | Sem fronteiras de módulo, débito técnico alto, difícil extração futura | ❌ Rejeitado |
| **Serverless Functions** | Escala automática por função | Cold start, estado externo obrigatório, vendor lock-in, debugging complexo | ❌ Rejeitado |

### Comparação detalhada: Monólito Modular vs Microservices

| Critério | Monólito Modular | Microservices |
|---|---|---|
| **Overhead operacional** | 1 processo, 1 banco | N serviços, service discovery, mesh |
| **Latência interna** | In-process (ns) | Network hops (ms) |
| **Transações** | ACID local via `UnitOfWork` | Eventual consistency / SAGA |
| **Deploy** | 1 artefato Docker | N pipelines, N imagens |
| **Debugging** | Stack trace única | Distributed tracing obrigatório |
| **Team size adequado** | 1–15 engenheiros | 8+ por serviço (Lei de Conway) |
| **Adoção de EDA** | Introdução gradual (Outbox) | Forçado desde o início |
| **Refactor de domínio** | Seguro, in-process | Requer versionamento de contratos de API |

**"Monolith First"** (Martin Fowler): extrair microserviços prematuramente solidifica fronteiras de domínio baseadas em suposições, não em evidências de uso real. O custo de uma extração errada é altíssimo — chamadas síncronas adicionadas onde havia chamadas in-process, SAGA patterns para transações que eram atômicas.

---

## Modularidade Preservada

A separação em projetos .NET distintos e namespaces por capacidade garante extração futura sem refatoração de domínio:

```
SrmCreditEngine.Domain
  ├── Entities/
  │   ├── Settlement.cs      ← núcleo do SettlementService futuro
  │   ├── Currency.cs        ← núcleo do FxRateService futuro
  │   └── Cedent.cs
  ├── Services/
  │   └── PricingDomainService.cs  ← núcleo do PricingService futuro
  └── ValueObjects/
      └── Money.cs           ← compartilhado via NuGet package no futuro

SrmCreditEngine.Application
  ├── UseCases/Pricing/       ← extração → PricingService
  ├── UseCases/Settlements/   ← extração → SettlementService
  └── UseCases/Reports/       ← extração → ReportingService (read-only)
```

---

## Critérios para Extração de Microserviços

A extração de um módulo para serviço independente deve ser avaliada quando **pelo menos dois** dos critérios abaixo forem atingidos:

| Critério | Threshold |
|---|---|
| Volume de transações no módulo | > 100k req/min sustentados |
| Equipe dedicada ao módulo | ≥ 2 engenheiros exclusivos |
| Ciclo de deploy independente necessário | > 2 deploys/semana no módulo |
| SLA diferenciado do módulo | Divergência > 1 nível em relação ao serviço central |
| Tecnologia divergente necessária | Ex.: ML model serving, stream processing dedicado |
| Contenção de recursos comprovada | Latência P99 do módulo degradada por carga de outro módulo |

### Candidatos naturais para extração futura

1. **Pricing Service** — CPU-bound, pode necessitar de auto-scaling agressivo e cache dedicado (Redis). Interface natural: REST/gRPC com cache-aside.
2. **Exchange Rate Service** — Leitura intensiva, baixa escrita. Candidato a serviço de leitura com cache TTL agressivo e pub/sub para invalidação.
3. **Report/Statement Service** — Queries analíticas pesadas; pode migrar para CQRS com read replica dedicada ou ClickHouse.
4. **Notification/Event Relay** — Conforme o EDA amadurece, o Outbox Processor pode virar um serviço de relay independente consumindo da tabela `outbox_events`.

---

## Plano de Migração — Strangler Fig Pattern

Quando os critérios de extração forem atingidos, o padrão **Strangler Fig** é o caminho recomendado:

```
Fase 1 — Interface Protocol
  Definir contrato OpenAPI/gRPC para o módulo a extrair
  Manter implementação no monólito por trás do contrato
  Adicionar feature flag por tenant/cedente

Fase 2 — Shadow Service
  Novo serviço processa em paralelo (shadow mode)
  Monólito ainda é o source of truth
  Comparar respostas; divergência zero por N dias

Fase 3 — Traffic Shift
  Canary 5% → 25% → 50% → 100%
  Rollback automático via feature flag

Fase 4 — Decommission
  Remover código do módulo do monólito
  Monólito torna-se consumer do novo serviço
```

A comunicação inter-serviços deve usar:
- **Kafka (assíncrono)** para operações que toleram eventual consistency — ver [EDA Proposal](../eda-proposal.md)
- **gRPC (síncrono)** apenas para read-your-writes obrigatórios (ex.: pricing confirmado antes de criar settlement)

---

## Rota de Evolução EDA

A adoção do Outbox Pattern (já implementado) é o ponto de partida da jornada EDA, compatível tanto com o monólito quanto com microserviços:

```
Monólito  →  Outbox + Kafka  →  Consumers independentes  →  Microserviços
  (hoje)      (fase 2)               (fase 3)                  (fase 4)
```

---

## Consequências

### Positivas
- Deploy em um único container Docker facilita CI/CD e ambientes de desenvolvimento local.
- Transações ACID garantidas para operações que cruzam entidades (`Settlement` + `Receivable` + `ExchangeRate` em uma única `UnitOfWork`).
- Onboarding de novos desenvolvedores mais rápido — um único repositório, um único contexto de execução.
- Debugging linear — stack traces não cruzam fronteiras de rede.
- Sem overhead de service mesh, sidecar proxies ou distributed tracing obrigatório na fase inicial.
- Refatoração de regras de negócio (ex.: mudar a fórmula de PV) sem impacto em contratos de rede.

### Negativas / Trade-offs
- Escalabilidade horizontal aplica-se ao processo inteiro — impossível escalar apenas o módulo de pricing sem escalar o serviço completo.
- Risco de acoplamento acidental se as fronteiras de módulo não forem respeitadas (mitigado pelo uso de interfaces e assembly boundaries via projetos separados no .NET).
- Deploy de qualquer componente implica rebuild e redeploy do serviço completo.

### Neutras
- A comunicação assíncrona via Outbox + Kafka (já implementada como proposta) é compatível tanto com o modelo monolítico quanto com a eventual extração para microserviços — não há lock-in arquitetural.

---

## Referências

- [ADR-001 — .NET 10 + Clean Architecture](ADR-001-dotnet-clean-architecture.md)
- [ADR-002 — PostgreSQL vs NoSQL](ADR-002-postgresql-vs-nosql.md)
- [EDA Proposal — Kafka + Outbox Pattern](../eda-proposal.md)
- [High-Scale Design — 1M tx/min](../high-scale-design.md)
- Sam Newman — *Building Microservices*, cap. "When Should You Use Microservices?"
- Martin Fowler — [MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html)
- Michael Nygard — *Release It!*, Strangler Fig pattern
- Vaughn Vernon — *Implementing Domain-Driven Design*, Bounded Context boundaries
