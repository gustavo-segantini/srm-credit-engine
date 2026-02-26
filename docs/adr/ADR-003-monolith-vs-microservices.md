# ADR-003: Monólito Modular vs Microservices

| Campo          | Valor                                  |
|----------------|----------------------------------------|
| **Status**     | Aceito                                 |
| **Data**       | 2025-01-01                             |
| **Contexto**   | Estratégia de decomposição do sistema  |

---

## Contexto

Ao iniciar um novo produto fintech, existe a pressão de adotar microservices desde o primeiro dia por percepção de "modernidade" ou escalabilidade. Contudo, a decisão arquitetural deve ser guiada por requisitos concretos e tamanho de equipe, não por tendências.

**Dados do contexto atual:**
- Time: 1–5 engenheiros
- Domínio: ainda em exploração (regras de spread/precificação mudam com frequência)
- Infraestrutura: 1 cluster PostgreSQL, sem SLA multi-tenant no MVP
- Prazo: MVP em semanas, não meses

## Decisão

Adotar **Monólito Modular** com separação de preocupações via Clean Architecture, mantendo a possibilidade de extração para microservices futura.

## Raciocínio

**Por Monólito Modular:**

| Critério                  | Monólito Modular               | Microservices                      |
|---------------------------|--------------------------------|------------------------------------|
| **Overhead operacional**  | 1 processo, 1 banco            | N serviços, discovery, mesh        |
| **Latência interna**      | In-process (ns)                | Network hops (ms)                  |
| **Transações**            | ACID local                     | Eventual consistency / SAGA        |
| **Deploy**                | 1 artefato                     | N pipelines, N imagens             |
| **Debugging**             | Stack trace única              | Distributed tracing obrigatório    |
| **Time size viável**      | 1–15 engenheiros               | 8+ por serviço (Conway's Law)      |
| **EDA migration**         | Introdução gradual de eventos  | Forçado desde o início             |

**"Monolith First"** (Martin Fowler): extrair microservices prematuramente solidifica fronteiras de domínio baseadas em suposições, não em evidências de uso real.

**Modularidade preservada:**
A separação em camadas (Domain → Application → Infrastructure → API) e namespaces (`Pricing`, `Settlements`, `ExchangeRates`, `Reporting`) garante que cada módulo seja extraível a um microservice independentemente quando necessário.

```plaintext
SrmCreditEngine.Domain
  ├── Pricing/       ← extraível como PricingService
  ├── Settlements/   ← extraível como SettlementService  
  ├── ExchangeRates/ ← extraível como FxRateService
  └── Shared/

SrmCreditEngine.Application
  ├── Pricing/
  ├── Settlements/
  └── ExchangeRates/
```

**Trigger para extração (critérios objetivos):**
1. Time de Settlements cresce para 5+ engenheiros **E** ciclos de deploy bloqueiam outros times
2. Latência de precificação >200ms causada por contenção de threads com outros módulos
3. Requisito de SLA diferenciado (99,99% para Settlements vs 99,9% para Reporting)

## Plano de Migração para EDA (quando necessário)

```
Monólito → Outbox Pattern → Kafka → Consumers independentes → Microservices
```

Ver detalhe em `docs/eda-proposal.md`.

## Alternativas Rejeitadas

| Alternativa         | Motivo da rejeição                                                |
|---------------------|-------------------------------------------------------------------|
| Microservices MVP   | Overhead operacional desproporcional; SAGA para transações financeiras é complexo |
| Serverless (Lambda) | Cold start inaceitável para precificação real-time; estado difícil |
| CQRS + EventSourcing desde o início | Curva de aprendizado + audit log pode ser via tabela de eventos simples |

## Consequências

**Positivas:**
- Deploy e debugging mais simples → menor MTTR
- ACID nativo em todas as operações financeiras
- Refactor rápido de regras de negócio sem impacto em interface de rede

**Negativas:**
- Escala horizontal limitada (todos os módulos escalam juntos)
- Se a fronteira de módulo for errada, extração futura custará mais

## Referências

- [MonolithFirst — Martin Fowler](https://martinfowler.com/bliki/MonolithFirst.html)
- [Modular Monolith — Sam Newman](https://samnewman.io/patterns/architectural/modular-monolith/)
