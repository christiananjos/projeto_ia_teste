---
tipo: planejamento
status: em-andamento
data: 2026-10-16
tags: [proximo-passo, implementacao]
---

# Implementação do Catálogo de Produtos

> Fonte: docs/SPEC.md (REQ-001 a REQ-004, BR-001 a BR-007). Arquitetura desenhada pelo 02-architect-sdd em 2026-10-16; backend implementado pelo 03-dotnet-specialist em 2026-10-16.

## Situação
O backend está implementado e `dotnet build` passa sem erros nem avisos. Faltam os testes (próximo passo: 05-test-engineer) e a validação contra um SQL Server real.

## Componentes
- [x] Solução e infraestrutura de build: `Catalogo.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config` ([[ADR-002 Estrutura da solução e dependências]])
- [x] Domain: `Categoria`, `Produto`, `Sku`, as interfaces de repositório, `IUnitOfWork` e as exceções
- [x] Application: 11 casos de uso, 7 validators, DTOs, `Resultado`/`Erro` e as interfaces de consulta ([[ADR-003 Casos de uso, Result pattern e CQRS leve]])
- [x] Infrastructure: `CatalogoDbContext`, configurações com índices e collation, repositórios, consultas e a migration `Inicial` ([[Persistência EF Core]], [[Modelo de Dados do Catálogo]])
- [x] API: endpoints de categoria e produto, ProblemDetails, JWT, policies e rate limiting ([[API de Categorias]], [[API de Produtos]], [[Tratamento de Erros e ProblemDetails]], [[Provedor de Identidade JWT]])
- [ ] Testes unitários e de integração com Testcontainers ([[ADR-013 Estratégia de testes com Testcontainers]]). Os projetos `src/Tests/Catalogo.UnitTests` e `src/Tests/Catalogo.IntegrationTests` ainda não existem: o 05-test-engineer cria e adiciona ao `Catalogo.slnx` (pasta `/src/Tests/` já existe na solução). As versões dos pacotes de teste ainda não estão no `Directory.Packages.props`. `Application` e `Infrastructure` já declaram `InternalsVisibleTo` para os dois projetos.

## Pendências
- **Correções da revisão (04-reviewer-sdd, 2026-10-16): feitas pelo 03-dotnet-specialist em 2026-10-16. Falta uma nova passada do reviewer.**
  - [x] 🔴 Regex do SKU trocada para `^[A-Za-z0-9-]+\z` ([[SKU aceita quebra de linha no final]], resolvido).
  - [x] 🟡 Snapshot `CatalogoDbContextModelSnapshot.cs` movido para `src/Infrastructure/Persistencia/Migrations/`. A pasta `src/Infrastructure/Catalogo/` foi apagada.
  - [x] 🟡 `ExcecaoGlobalHandler` trata `OperationCanceledException` com `RequestAborted` cancelado: log `Information`, status 499 sem corpo ([[Tratamento de Erros e ProblemDetails]]).
  - [x] 🟢 `ExisteSkuAsync` recebe `Sku`, e o caso de uso cria o VO uma vez só. Removido o `QueueProcessingOrder` sem efeito no rate limiter.
  - [ ] 🟢 `RemoverCategoriaUseCase` ainda carrega a entidade rastreada só para checar se existe. Ficou assim porque a troca exige um método novo no repositório; o reviewer considerou aceitável.
- **Teste de regressão do SKU:** o 05-test-engineer deve cobrir `"TEC-001\n"` → 400 no validator e `DomainException` no `Sku.Criar`.
- **Validar contra SQL Server real.** Nesta sessão não havia Docker nem LocalDB, então nada que toca o banco foi executado: inserts, índices filtrados, collation CI, tradução de 2601/2627/547, `ExecuteDelete` da ADR-001 e materialização do `Sku`. O SQL gerado foi conferido com `ToQueryString` e o pipeline HTTP sem banco foi testado manualmente (401, 403, 400, 404, 429, 500). Os testes de integração do 05-test-engineer cobrem esse ponto, mas precisam de Docker.
- A ferramenta global `dotnet-ef` da máquina está em 10.0.9, mais antiga que o runtime EF 10.0.13 (só gera aviso). Atualizar com `dotnet tool update -g dotnet-ef` antes de gerar novas migrations.

## Relacionado
- [[Arquitetura da Solução]]
- [[Confirmar decisões que alteram comportamento]]
