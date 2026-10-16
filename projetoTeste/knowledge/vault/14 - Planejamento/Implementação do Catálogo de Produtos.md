---
tipo: planejamento
status: em-andamento
data: 2026-10-16
tags: [proximo-passo, implementacao]
---

# Implementação do Catálogo de Produtos

> Fonte: docs/SPEC.md (REQ-001 a REQ-004, BR-001 a BR-007). Arquitetura desenhada pelo 02-architect-sdd em 2026-10-16; backend implementado pelo 03-dotnet-specialist em 2026-10-16.

## Situação
O backend está implementado e `dotnet build` passa sem erros nem avisos. Os testes existem e passam (05-test-engineer, 2026-10-16). Falta executar a parte que depende de um SQL Server real.

## Componentes
- [x] Solução e infraestrutura de build: `Catalogo.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config` ([[ADR-002 Estrutura da solução e dependências]])
- [x] Domain: `Categoria`, `Produto`, `Sku`, as interfaces de repositório, `IUnitOfWork` e as exceções
- [x] Application: 11 casos de uso, 7 validators, DTOs, `Resultado`/`Erro` e as interfaces de consulta ([[ADR-003 Casos de uso, Result pattern e CQRS leve]])
- [x] Infrastructure: `CatalogoDbContext`, configurações com índices e collation, repositórios, consultas e a migration `Inicial` ([[Persistência EF Core]], [[Modelo de Dados do Catálogo]])
- [x] API: endpoints de categoria e produto, ProblemDetails, JWT, policies e rate limiting ([[API de Categorias]], [[API de Produtos]], [[Tratamento de Erros e ProblemDetails]], [[Provedor de Identidade JWT]])
- [x] Testes unitários e de integração (05-test-engineer, 2026-10-16): `src/Tests/Catalogo.UnitTests` (288 aprovados) e `src/Tests/Catalogo.IntegrationTests` (79 aprovados, 27 ignorados sem Docker), ambos no `Catalogo.slnx`. Application com 100% de linhas. Ver [[Estratégia e execução dos testes]].
- [ ] Executar os 27 testes com SQL Server real ([[Testes de integração com SQL Server pendentes]]).

## Pendências
- **Correções da revisão (04-reviewer-sdd, 2026-10-16): feitas pelo 03-dotnet-specialist em 2026-10-16. Falta uma nova passada do reviewer.**
  - [x] 🔴 Regex do SKU trocada para `^[A-Za-z0-9-]+\z` ([[SKU aceita quebra de linha no final]], resolvido).
  - [x] 🟡 Snapshot `CatalogoDbContextModelSnapshot.cs` movido para `src/Infrastructure/Persistencia/Migrations/`. A pasta `src/Infrastructure/Catalogo/` foi apagada.
  - [x] 🟡 `ExcecaoGlobalHandler` trata `OperationCanceledException` com `RequestAborted` cancelado: log `Information`, status 499 sem corpo ([[Tratamento de Erros e ProblemDetails]]).
  - [x] 🟢 `ExisteSkuAsync` recebe `Sku`, e o caso de uso cria o VO uma vez só. Removido o `QueueProcessingOrder` sem efeito no rate limiter.
  - [ ] 🟢 `RemoverCategoriaUseCase` ainda carrega a entidade rastreada só para checar se existe. Ficou assim porque a troca exige um método novo no repositório; o reviewer considerou aceitável.
- [x] **Teste de regressão do SKU:** coberto e executado em 2026-10-16 ([[CT BR-003 SKU]]).
- [x] **Cancelamento → 499 sem log `Error`:** confirmado em teste em 2026-10-16. O 499 vem do middleware do framework, e o ramo do `ExcecaoGlobalHandler` não chega a ser executado ([[CT Tratamento de erros e ProblemDetails]]).
- **Validar contra SQL Server real.** Continua pendente: os testes existem, mas o Docker não estava rodando ([[Testes de integração com SQL Server pendentes]]). O pipeline HTTP sem banco agora tem testes automatizados (79 casos).
- A ferramenta global `dotnet-ef` da máquina está em 10.0.9, mais antiga que o runtime EF 10.0.13 (só gera aviso). Atualizar com `dotnet tool update -g dotnet-ef` antes de gerar novas migrations.

## Relacionado
- [[Arquitetura da Solução]]
- [[Confirmar decisões que alteram comportamento]]
