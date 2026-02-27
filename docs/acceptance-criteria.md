# Critérios de Aceite — SRM Credit Engine

> Documento de referência para validação do produto. Organizado pelos quatro eixos definidos no case: **Usabilidade**, **Segurança**, **Desempenho** e **Escalabilidade**. Cada critério segue o formato BDD (Given / When / Then) onde aplicável.

---

## 1. Usabilidade

### AC-01 — Simulação em tempo real no Painel do Operador
**Given** o operador preenche valor, tipo de recebível, data de vencimento e moeda de pagamento  
**When** qualquer campo de precificação é alterado e o formulário é válido  
**Then** o resultado de simulação (VP, deságio, desembolso líquido) é atualizado automaticamente em até 800ms sem clique adicional

**Critério de falha:** O campo exibe resultado desatualizado ou exige ação manual extra.

---

### AC-02 — Seleção de cedente dinâmica
**Given** o operador acessa o Painel do Operador  
**When** o formulário é renderizado  
**Then** um `<select>` exibe apenas cedentes **ativos**, com nome e CNPJ visíveis, carregado da API — sem necessidade de digitar UUID manualmente

---

### AC-03 — Filtros da Grid de Transações
**Given** a grid de Transações está carregada  
**When** o operador aplica filtro de data início, data fim, moeda ou cedente  
**Then** a tabela recarrega os dados do servidor com os filtros aplicados e a paginação reseta para página 1

---

### AC-04 — Paginação server-side
**Given** existem mais de 15 liquidações registradas  
**When** o operador navega pelas páginas  
**Then** apenas `pageSize` registros são retornados por requisição e os controles de página refletem o total correto

---

### AC-05 — Feedback de estado
**Given** o operador submete um formulário  
**When** a operação está em andamento  
**Then** o botão exibe estado de carregamento ("Simulating…", "Confirming…") e fica desabilitado até a resposta; erros são exibidos inline em vermelho

---

### AC-06 — Cadência de câmbio
**Given** o usuário acessa a página de Taxas de Câmbio  
**When** seleciona um par de moedas (ex: USD → BRL)  
**Then** a taxa atual, fonte e data de atualização são exibidos; o formulário de atualização manual funciona e invalida o cache React Query

---

## 2. Segurança

### AC-07 — Autenticação obrigatória em operações financeiras
**Given** um cliente sem token JWT válido  
**When** tenta criar uma liquidação (`POST /api/v1/settlements`) ou atualizar taxa de câmbio (`PUT /api/v1/currency/exchange-rates`)  
**Then** a API retorna `401 Unauthorized` com body RFC 7807 Problem Details

---

### AC-08 — Token expirado rejeitado
**Given** um cliente com token JWT expirado (> 60 minutos)  
**When** realiza qualquer requisição autenticada  
**Then** a API retorna `401 Unauthorized` e loga o evento (Serilog `Warning`)

---

### AC-09 — Rate Limiting
**Given** um cliente excede 30 requisições/minuto em `POST /pricing/simulate`  
**When** a próxima requisição é feita dentro da janela  
**Then** a API retorna `429 Too Many Requests` com header `Retry-After`

---

### AC-10 — Validação de input — CNPJ
**Given** um payload com CNPJ inválido (menos de 14 dígitos, com pontuação, dígito verificador errado)  
**When** `POST /api/v1/cedents` é chamado  
**Then** a API retorna `400 Bad Request` com array de erros de validação; nenhum dado é persistido

---

### AC-11 — Validação de input — Precificação
**Given** um payload com `faceValue ≤ 0`, `dueDate` no passado ou `receivableType` inválido  
**When** `POST /api/v1/pricing/simulate` é chamado  
**Then** a API retorna `400 Bad Request` com mensagem descritiva; nenhum cálculo é executado

---

### AC-12 — Precisão numérica
**Given** uma liquidação com valor face de `R$ 10.000,00`, spread 1,5% a.m. e prazo de 3 meses  
**When** o cálculo de VP é executado  
**Then** o resultado é `R$ 9.569,88` (±R$ 0,01), sem arredondament errôneo por uso de float/double

**Implementação:** Todos os valores monetários usam `decimal` (.NET) e `numeric(18,6)` (PostgreSQL).

---

### AC-13 — Controle de concorrência
**Given** duas requisições simultâneas tentam liquidar o mesmo recebível  
**When** ambas chegam ao banco dentro do mesmo ciclo de transação  
**Then** apenas uma é processada com sucesso; a outra recebe `409 Conflict` (DbUpdateConcurrencyException via RowVersion/xmin)

---

## 3. Desempenho

### AC-14 — Latência de simulação
**Given** o sistema está sob carga normal (< 50 rps)  
**When** `POST /api/v1/pricing/simulate` é chamado  
**Then** a resposta é retornada em P95 < 100ms (medido via Prometheus `http_request_duration_seconds`)

---

### AC-15 — Relatório com grande volume
**Given** existem 100.000 liquidações no banco  
**When** `GET /api/v1/reports/settlement-statement` é chamado com filtros de data e paginação (`pageSize=50`)  
**Then** a resposta é retornada em P95 < 500ms

**Implementação:** Query Dapper com SQL nativo, índices em `created_at`, `cedent_id` e `payment_currency`.

---

### AC-16 — Frontend — First Contentful Paint
**Given** o ambiente de produção (build com `npm run build`)  
**When** o usuário abre o Painel do Operador pela primeira vez  
**Then** FCP < 1,5s (Lighthouse); assets servidos com gzip via nginx

---

### AC-17 — Health Check
**Given** o container da API está em execução  
**When** `GET /health` é chamado pelo orquestrador de containers (Docker / Kubernetes)  
**Then** retorna `200 OK` com payload `{"status": "Healthy"}` em < 50ms

---

## 4. Escalabilidade

### AC-18 — Stateless API — escalonamento horizontal
**Given** múltiplas réplicas da API são iniciadas  
**When** requisições são distribuídas entre elas via load balancer  
**Then** o comportamento é idêntico em todas as réplicas (sem estado em memória compartilhada, sem sticky sessions obrigatórias)

**Implementação:** Sem cache em memória local; JWT é validado por chave pública (stateless); todas as sessões de usuário são independentes.

---

### AC-19 — Particionamento de dados
**Given** a tabela `settlements` ultrapassa 50 milhões de registros  
**When** queries de relatório filtram por `created_at` dentro de um mês específico  
**Then** o planner do PostgreSQL utiliza partition pruning, eliminando partições fora do intervalo

**Implementação:** DDL documenta particionamento mensal por `RANGE(created_at)` (ver `docs/sql/ddl.sql`).

---

### AC-20 — HPA (Horizontal Pod Autoscaler)
**Given** o cluster Kubernetes tem o HPA configurado (ver `infra/k8s/`)  
**When** o uso médio de CPU das réplicas da API excede 70%  
**Then** novas réplicas são provisionadas automaticamente até o limite de 10 pods

---

### AC-21 — Circuit Breaker
**Given** o serviço externo de câmbio retorna falha 5 vezes consecutivas  
**When** uma nova requisição é feita  
**Then** o circuit breaker abre (estado Open) e retorna erro imediatamente por 30s, sem aguardar timeout completo — protegendo o thread pool

---

## 5. Rastreabilidade e Auditoria

### AC-22 — Log estruturado em toda transação financeira
**Given** uma liquidação é criada ou falha  
**When** o evento ocorre  
**Then** um log estruturado (JSON/Serilog) é emitido contendo: `settlementId`, `cedentId`, `amount`, `currency`, `status`, `timestamp` — rastreável em Grafana

---

### AC-23 — Trilha de auditoria para liquidações
**Given** um regulador solicita o histórico de uma liquidação  
**When** consulta `GET /api/v1/reports/settlement-statement?cedentId={id}`  
**Then** todos os registros são retornados com status, valores e timestamps sem possibilidade de exclusão (soft-delete apenas para cedentes)

---

### AC-24 — Versionamento semântico de releases
**Given** uma versão é entregue  
**When** a tag Git é inspecionada (`git tag` ou GitHub Releases)  
**Then** existe uma tag anotada no formato `vMAJOR.MINOR.PATCH` (ex: `v1.1.0`) descrevendo o conteúdo do release

---

## Matriz de Cobertura

| Critério | Cobertura Atual | Tipo de Teste |
|---|---|---|
| AC-01 — Simulação tempo real | ✅ | Vitest (manual confirm) |
| AC-02 — Select cedente | ✅ | Vitest |
| AC-03 — Filtros grid | ✅ | Vitest |
| AC-04 — Paginação server-side | ✅ | Integration Test |
| AC-05 — Feedback de estado | ✅ | Vitest |
| AC-06 — Cadência câmbio | ✅ | Integration Test |
| AC-07 — 401 sem token | ✅ | Integration Test |
| AC-08 — Token expirado | ✅ | Integration Test |
| AC-09 — Rate limiting 429 | ✅ | Integration Test |
| AC-10 — Validação CNPJ | ✅ | Unit Test |
| AC-11 — Validação precificação | ✅ | Unit Test |
| AC-12 — Precisão decimal | ✅ | Unit Test (38 testes) |
| AC-13 — Concorrência / RowVersion | ✅ | Integration Test |
| AC-14 — Latência < 100ms | 🟡 Observable via Prometheus | Observabilidade |
| AC-15 — Relatório 100k rows | 🟡 Arquitetura (Dapper + índices) | Load Test (Fase 2) |
| AC-16 — FCP < 1.5s | 🟡 Build nginx configurado | Lighthouse (manual) |
| AC-17 — Health check | ✅ | Docker healthcheck |
| AC-18 — Stateless API | ✅ | Arquitetura (JWT stateless) |
| AC-19 — Particionamento | 🟡 DDL documentado | DBA review |
| AC-20 — HPA K8s | ✅ | `infra/k8s/backend-hpa.yaml` |
| AC-21 — Circuit Breaker | ✅ | Unit Test (Polly) |
| AC-22 — Log estruturado | ✅ | Serilog + Grafana |
| AC-23 — Trilha de auditoria | ✅ | Integration Test |
| AC-24 — Semantic versioning | ✅ | `v1.0.0`, `v1.1.0` |
