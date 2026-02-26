# ADR-001: .NET 10 + Clean Architecture para o Backend

| Campo          | Valor                    |
|----------------|--------------------------|
| **Status**     | Aceito                   |
| **Data**       | 2025-01-01               |
| **Contexto**   | Escolha do stack backend |

---

## Contexto

O SRM Credit Engine é uma plataforma de cessão de crédito com requisitos de:
- Regras de negócio financeiras complexas (precificação, liquidação)
- Necessidade de auditabilidade e rastreabilidade de operações
- Evolução incremental (Junior → Expert) sem refactoring disruptivo
- Time misto (backend, frontend, devops) trabalhando em paralelo

## Decisão

Adotar **.NET 10 (ASP.NET Core)** com **Clean Architecture** em 4 camadas:
`Domain` → `Application` → `Infrastructure` → `API`

## Raciocínio

**Por .NET 10:**
- Performance líder de benchmark (vs. Go, Python, Java) — TechEmpower Round 22
- Suporte LTS até novembro de 2026
- Native AOT para potencial micro-serviço futuro sem cold-start
- Ecossistema maduro: EF Core, Dapper, Serilog, FluentValidation
- OpenAPI built-in (`AddOpenApi()` / `MapOpenApi()`) sem dependência externa

**Por Clean Architecture (vs. Vertical Slice / CQRS puro):**
- Domínio completamente isolado de frameworks — testável de forma unitária pura
- Curva de onboarding menor para desenvolvedores familiarizados com DDD
- Inversão de dependência permite substituir PostgreSQL por qualquer BD relacional sem tocar no domínio
- Interface `IPricingStrategy` + Pattern Strategy aberto/fechado: adicionar nova modalidade = uma nova classe

**Alternativas rejeitadas:**

| Alternativa       | Motivo da rejeição                                   |
|-------------------|------------------------------------------------------|
| Node.js/TypeScript | Ausência de tipagem decimal nativa; risco em cálculos financeiros |
| Python (FastAPI)  | GIL; performance inferior para processamento paralelo          |
| Java (Spring Boot)| JVM overhead; complexidade de configuração vs. .NET            |
| Vertical Slice    | Adequado para CQRS; mas aumenta acoplamento entre camadas neste contexto financeiro |

## Consequências

**Positivas:**
- Regras de precificação 100% testáveis sem banco de dados
- Estratégias adicionáveis via injeção de dependência (Open/Closed Principle)
- Separação clara entre consulta analítica (Dapper) e escrita transacional (EF Core)

**Negativas:**
- Mais boilerplate inicial (4 projetos vs. 1)
- Curva de aprendizado da Clean Architecture para devs novos

## Referências

- Robert C. Martin, *Clean Architecture* (2017)
- Microsoft, [ASP.NET Core Performance Benchmarks](https://www.techempower.com/benchmarks/)
