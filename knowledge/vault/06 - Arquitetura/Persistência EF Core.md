---
tipo: arquitetura
status: implementado
data: 2026-10-16
tags: [ef-core, sql-server, persistencia]
---

# Persistência EF Core

> Fonte: docs/SPEC.md (Modelo de Dados, Índices). Decisões do 02-architect-sdd em 2026-10-16, implementadas pelo 03-dotnet-specialist em 2026-10-16 em `src/Infrastructure/`.
> Banco: [[SQL Server]]. Tabelas, colunas e índices: [[Modelo de Dados do Catálogo]].

## Componentes

- `Persistencia/CatalogoDbContext.cs`: `DbSet` de `Categorias` e `Produtos`, implementa `IUnitOfWork.SalvarAlteracoesAsync`. Schema padrão `dbo`.
- `Persistencia/Configuracoes/`: `CategoriaConfiguration` e `ProdutoConfiguration` (collation, índices filtrados, FK Restrict sem navegação, query filter).
- `Persistencia/Conversores/UtcDateTimeConverter.cs`: aplicado a `DateTime` e `DateTime?` via `ConfigureConventions`.
- `Persistencia/TradutorErrosSql.cs` e `NomesBanco.cs`: tradução de erros SQL e nomes fixos de índices/FK.
- `Persistencia/CatalogoDbContextFactory.cs`: factory de design-time para `dotnet ef`.
- `Repositorios/` (escrita, rastreado) e `Consultas/` (leitura, `AsNoTracking` com projeção para DTO).
- `DependencyInjection.AddInfrastructure(IConfiguration)`: `DbContext` scoped, `IUnitOfWork` resolve o mesmo contexto.

## Comportamentos

- Produto tem um global query filter `DataRemocao == null` ([[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]]). Só a exclusão física da ADR-001 usa `IgnoreQueryFilters()`.
- Todo `DateTime` passa pelo conversor UTC: grava em UTC e lê com `Kind = Utc`, então a API serializa com `Z` (BR-007).
- Erros SQL 2601 e 2627 viram `ConflitoPersistenciaException(Unicidade)`; 547 vira `ConflitoPersistenciaException(Referencia)`. A restrição é identificada procurando os nomes conhecidos na mensagem (funciona mesmo com mensagem localizada pelo servidor), com fallback para o nome entre aspas.
- A remoção de categoria roda dentro de `CreateExecutionStrategy().ExecuteAsync` + transação, com `ExecuteDelete`: primeiro os produtos já removidos, depois a categoria. FK 547 → rollback e `ConflitoPersistenciaException(Referencia)` ([[ADR-001 Remoção de categoria com produtos removidos]]).
- O filtro `nome` da listagem usa `string.Contains`, que o EF 10 traduz para `LIKE @p ESCAPE N'\'` com `%`, `_` e `[` escapados (conferido com `ToQueryString`).
- `ExisteSkuAsync(Sku sku, ...)` recebe o value object, criado uma vez só no `CriarProdutoUseCase`, e compara com `p.Sku == sku`; o conversor transforma o parâmetro em `nvarchar(40)` e a collation CI faz `tec-001` colidir com `TEC-001`.
- A connection string `ConnectionStrings:Catalogo` é lida na criação do contexto (permite que os testes a substituam); se faltar, a primeira requisição que usa o banco falha com mensagem explicando user-secrets / `ConnectionStrings__Catalogo`. `UseSqlServer(..., EnableRetryOnFailure())`.
- A API não aplica migrations no startup.
