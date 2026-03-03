# AI_USAGE.md

Documento de transparência sobre o uso de ferramentas de IA durante o desenvolvimento do SRM Credit Engine.

> **Nota de honestidade:** Este documento descreve com fidelidade o real nível de assistência utilizado. O projeto foi desenvolvido em parceria intensiva com IA — não como "autocomplete de scaffold", mas como co-autor de grande parte da implementação. O valor humano entregue foi a direção arquitetural, a revisão crítica linha a linha, a identificação e correção de alucinações, e a validação funcional de ponta a ponta. Qualquer declaração diferente seria desonesta e invalidaria o critério de avaliação #5 do case.

---

## Ferramentas Utilizadas

| Ferramenta     | Modelo            | Papel no projeto                                                              |
|----------------|-------------------|-------------------------------------------------------------------------------|
| GitHub Copilot | Claude Sonnet 4.6 | Co-autor principal — geração de código, arquitetura, documentação, debugging  |
| GitHub Copilot | GPT-4o (Edits)    | Refatorações inline e sugestões de completação                                |

---

## Nível Real de Assistência

A IA gerou a maior parte do código e documentação deste projeto. As responsabilidades foram divididas assim:

**IA gerou:**
- Toda a estrutura de camadas (Domain, Application, Infrastructure, API)
- Entidades de domínio, Value Objects, interfaces, exceções de domínio
- EF Core configurations, migrations, DbContext
- Todos os controllers, serviços, strategies, repositories
- Dapper analytics query para extrato de liquidação
- Frontend completo (React, TanStack Query, Zustand, Zod validation, 4 páginas)
- Todos os testes unitários e de integração (38 unit + 53 integration + 6 Vitest)
- Toda a documentação (ADRs, C4, ER, high-scale design, EDA, K8s manifests, README)
- Configuração Docker, CI/CD, Husky hooks, Prometheus/Grafana, `.editorconfig`

**Humano dirigiu e validou:**
- Todas as decisões arquiteturais fundamentadas (documentadas nos ADRs)
- Review crítico de cada entrega — compilação, execução e smoke test funcional completo
- Identificação e correção de todos os bugs listados abaixo
- Validação de que o output da IA correspondia ao enunciado do case
- Decisão de quando aceitar, rejeitar ou reescrever o que foi gerado

---

## Bugs Reais Encontrados e Corrigidos

Esta seção é o coração do documento — demonstra que o código foi efetivamente entendido, não apenas aceito.

### 1. Dockerfile com versão errada do runtime
**O que a IA gerou:** `FROM mcr.microsoft.com/dotnet/sdk:9.0` e `FROM mcr.microsoft.com/dotnet/aspnet:9.0`  
**O problema:** O projeto usa .NET 10 (`<TargetFramework>net10.0</TargetFramework>`). O container subia, mas o `dotnet publish` falhava silenciosamente.  
**Correção:** `sdk:10.0` e `aspnet:10.0`

### 2. `--no-restore` quebrando o publish no container Linux
**O que a IA gerou:** `dotnet publish --no-restore` no Dockerfile, assumindo que o restore da etapa anterior era suficiente  
**O problema:** O cache do NuGet no Linux usa caminhos diferentes do Windows. O publish falhava com `error MSB4062: The "ResolvePackageAssets" task could not be loaded`.  
**Correção:** Removido o flag `--no-restore` do step de publish.

### 3. `docker-compose.override.yml` sendo auto-mergeado com `target` incorreto
**O que a IA gerou:** Um arquivo `docker-compose.override.yml` com `target: build` para desenvolvimento local  
**O problema:** O Docker Compose faz merge automático de qualquer arquivo chamado `override`. Em CI/produção, isso forçava build parcial ao invés da imagem publicada final.  
**Correção:** Renomeado para `docker-compose.local.yml` — requer chamada explícita com `-f`.

### 4. Variável de ambiente do Seq errada
**O que a IA gerou:** `Serilog__WriteTo__1__Args__serverUrl: http://seq:5341`  
**O problema:** Essa convenção de path só funciona com índice fixo em arrays. O Serilog não conectava ao Seq no container; logs iam apenas para o stdout.  
**Correção:** Configuração customizada via `Seq__ServerUrl: http://seq:5341` lida diretamente no `Program.cs`.

### 5. `adduser`/`addgroup` indisponível na imagem `aspnet:10.0`
**O que a IA gerou:** `RUN addgroup --system app && adduser --system --group app` no Dockerfile  
**O problema:** A imagem `aspnet:10.0` é baseada em runtime-deps mínimo, sem utilitários de shell do BusyBox.  
**Correção:** `USER $APP_UID` — variável já definida pela imagem base da Microsoft para execução segura não-root.

### 6. Scalar UI inacessível fora do ambiente Development
**O que a IA gerou:** `if (app.Environment.IsDevelopment()) { app.MapScalarApiReference(); }`  
**O problema:** O container Docker roda em `Production` por padrão. A documentação da API ficava inacessível para quem subisse o stack via `docker compose up`.  
**Correção:** `MapScalarApiReference()` movido para fora do bloco condicional.

### 7. Bug de NaN no frontend — nomes de campos do DTO errados
**O que a IA gerou:** Referências a `result.spread` e `result.baseRate` no `OperatorPanel.tsx`  
**O problema:** O DTO de resposta do backend chama `appliedSpreadPercent` e `baseRatePercent`. Os campos chegavam como `undefined`, e `undefined * 100` produzia `NaN` visível na UI em produção.  
**Correção:** Substituídos pelos nomes corretos; removida a multiplicação por 100 (os valores já vêm em percentual).

### 8. Erro de build TypeScript no frontend em modo produção
**O que a IA gerou:** `z.preprocess((v) => Number(v), z.number())` no schema Zod do formulário  
**O problema:** O TypeScript com `strict: true` rejeitava a tipagem no contexto do react-hook-form. O build de desenvolvimento passava (transpilação tolerante), mas `tsc --noEmit` para produção falhava.  
**Correção:** `z.number()` direto com `valueAsNumber: true` no `register()` do react-hook-form.

### 9. ER diagram com tabelas que não existem na migration
**O que a IA gerou:** Diagrama ER com as tabelas `SETTLEMENT_ITEMS` e `OUTBOX_EVENTS`, e colunas `settlement_reference`, `is_cross_currency`, `effective_rate` em `settlements`  
**O problema:** Nenhuma dessas tabelas ou colunas existe em `20260226005905_InitialCreate.cs`. A IA usou o design de alta escala do `high-scale-design.md` como referência em vez da migration real.  
**Correção:** ER diagram reescrito inteiramente contra a migration real; nota adicionada explicando que `SETTLEMENT_ITEMS` e `OUTBOX_EVENTS` são parte do design futuro.

### 10. C4 Container com página ausente e rotas erradas
**O que a IA gerou:** Diagrama listando 3 páginas frontend e rotas como `/exchange-rates/...`  
**O problema:** Existe uma 4ª página (`Cedents`). O prefixo do controller é `/currency/exchange-rates/...`. O endpoint `GET /settlements` listado não existe na API.  
**Correção:** Diagrama reescrito com as 4 páginas reais e todas as rotas verificadas contra os controllers.

### 11. Hashes da simulação de crise eram fictícios
**O que a IA gerou:** README com `git revert` descrevendo hashes `232435c` (commit bugado) e `dd8b6d3` (revert) — ambos inventados  
**O problema:** `git show 232435c` retornava `fatal: bad object`. A simulação era texto sem evidência rastreável.  
**Correção:** O `git revert` foi executado de verdade. Commit `f6609d4` introduziu o bug real (spread 1.5% → 3.0%), e `49debe3` é o revert auditável — ambos visíveis em `git log --oneline` na `main`.

### 12. Afirmação sobre cherry-pick incorreta
**O que a IA gerou:** README afirmando que o hotfix foi aplicado via `git cherry-pick` para `release/v1.0.x`  
**O problema:** `git log origin/release/v1.0.x` mostrava `e5faaec Merge hotfix/fix-cheque-zero-term into release/v1.0.x` — merge `--no-ff`, não cherry-pick.  
**Correção:** Wording corrigido no README para refletir o histórico real.

### 13. `IOptionsMonitor<T>` afirmado mas não utilizado
**O que a IA gerou:** Durante review técnico, afirmou que o `JwtService` utilizava `IOptionsMonitor<T>` para hot-reload de configurações  
**O problema:** Busca em todos os arquivos `.cs` não retornou nenhuma ocorrência. O JWT usa `IConfiguration.GetSection("Jwt")` diretamente — `IOptionsMonitor<T>` não existe no projeto.  
**Correção:** Afirmação corrigida na hora; documentada como exemplo clássico de alucinação de "código que não existe".

### 14. Conflito de versões EF Core bloqueando o build
**O que a IA gerou:** Referências que puxavam `Microsoft.EntityFrameworkCore.Design` compatível com .NET 10 (EF Core 10.x), enquanto Npgsql 9.x exigia EF Core 9.x  
**O problema:** `NU1107: Version conflict detected for Microsoft.EntityFrameworkCore.Relational`  
**Correção:** Pin explícito de `Microsoft.EntityFrameworkCore.Design@9.0.2`; `AddDbContextCheck` removido pois puxava EF Core 10.x.

### 15. `UseXminAsConcurrencyToken()` não resolvia em runtime
**O que a IA gerou:** Chamada ao método de extensão `UseXminAsConcurrencyToken()` do Npgsql  
**O problema:** O método não estava disponível na versão pinada. O EF Core não detectava o token de concorrência; liquidações concorrentes passavam sem `DbUpdateConcurrencyException`.  
**Correção:** Substituído por configuração explícita em cada `IEntityTypeConfiguration<T>`:
```csharp
builder.Property(e => e.RowVersion)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

---

## Onde a IA Economizou Tempo

| Atividade                                     | Sem IA (estimado) | Com IA  | Fator |
|-----------------------------------------------|-------------------|---------|-------|
| Scaffolding monorepo + configurações iniciais | 4h                | 20min   | 12x   |
| Implementação camada Domain                   | 6h                | 1h      | 6x    |
| Implementação camada Infrastructure           | 8h                | 1,5h    | 5x    |
| Implementação camada API + controllers        | 4h                | 45min   | 5x    |
| Frontend completo (4 páginas + hooks)         | 10h               | 2h      | 5x    |
| Testes (38 unit + 53 integration + 6 Vitest)  | 10h               | 1,5h    | 7x    |
| Documentação (ADRs + C4 + K8s + EDA)          | 6h                | 1h      | 6x    |
| **Total**                                     | **~48h**          | **~8h** | **~6x** |

---

## Onde a IA Atrapalhou

1. **Confiança excessiva em afirmações não verificadas** — nomes de campos de DTOs, hashes de commits, métodos de extensão que não existiam. Sem verificação ativa contra o código real, qualquer um desses erros teria chegado ao avaliador.

2. **Tendência de gerar o design aspiracional, não o atual** — o ER diagram com `SETTLEMENT_ITEMS` e o C4 com rotas incorretas são exemplos de a IA descrever "o que deveria ser" em vez de "o que é". Diagramas de arquitetura requerem verificação obrigatória contra migrations e controllers reais.

3. **Viés de completude** — a IA tende a gerar documentação coerente internamente, mesmo quando os detalhes são inventados (hashes de commit, nomes de variáveis). O documento parece correto até você checar no terminal.

---

## Limitações Intencionais (Decisão, Não Esquecimento)

- **Kafka / Outbox Pattern** descrito em `docs/eda-proposal.md` mas não implementado — identificado como Fase 2 deliberada. Adicionar um broker de mensagens ao MVP aumentaria a complexidade operacional sem benefício proporcional na escala inicial.

---

## Conclusão

A IA foi usada como **acelerador de implementação**, não como substituto do julgamento de engenharia. Cada decisão arquitetural foi tomada conscientemente, cada bug foi investigado até a causa raiz, e cada linha de documentação foi verificada contra o comportamento real do sistema.

O que permitiu entregar um projeto de nível Staff em poucos dias foi exatamente essa combinação: a velocidade de geração da IA com a responsabilidade da revisão humana rigorosa.
