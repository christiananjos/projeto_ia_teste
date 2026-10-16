---
tipo: banco-de-dados
status: implementado
data: 2026-10-16
tags: [sql-server, tabelas, indices, migration]
---

# Modelo de Dados do Catálogo

> Fonte: docs/SPEC.md (Modelo de Dados, Índices) e a migration `Inicial` gerada pelo 03-dotnet-specialist em 2026-10-16 (`src/Infrastructure/Persistencia/Migrations/20261016183109_Inicial.cs`). Configuração em `src/Infrastructure/Persistencia/Configuracoes/`.

Schema `dbo`, banco [[SQL Server]]. Comportamentos do EF (query filter, conversor UTC, tradução de erros) em [[Persistência EF Core]].

## Tabela `Categorias`

| Coluna | Tipo | Observação |
|---|---|---|
| `Id` | `uniqueidentifier` PK `PK_Categorias` | UUID v7 gerado no domínio |
| `Nome` | `nvarchar(100)` NOT NULL | collation `Latin1_General_100_CI_AS` |
| `Descricao` | `nvarchar(500)` NULL | |
| `Ativa` | `bit` NOT NULL | default `1` |
| `DataCriacao` | `datetime2(7)` NOT NULL | UTC |
| `DataAtualizacao` | `datetime2(7)` NOT NULL | UTC |

Índice: `UX_Categorias_Nome` único em `Nome` (não filtrado, porque a remoção de categoria é física).

## Tabela `Produtos`

| Coluna | Tipo | Observação |
|---|---|---|
| `Id` | `uniqueidentifier` PK `PK_Produtos` | UUID v7 gerado no domínio |
| `CategoriaId` | `uniqueidentifier` NOT NULL | FK `FK_Produtos_Categorias_CategoriaId` → `Categorias(Id)`, `ON DELETE NO ACTION` (Restrict) |
| `Nome` | `nvarchar(150)` NOT NULL | `Latin1_General_100_CI_AS` |
| `Sku` | `nvarchar(40)` NOT NULL | `Latin1_General_100_CI_AS`; value object `Sku` convertido para string, guardado como o cliente enviou |
| `Descricao` | `nvarchar(1000)` NULL | |
| `Preco` | `decimal(18,2)` NOT NULL | |
| `Ativo` | `bit` NOT NULL | default `1` |
| `DataCriacao` / `DataAtualizacao` | `datetime2(7)` NOT NULL | UTC |
| `DataRemocao` | `datetime2(7)` NULL | exclusão lógica (BR-005) |

Índices (nomes fixos, usados pela tradução de erros):
- `UX_Produtos_Sku`: único em `Sku`, filtro `[DataRemocao] IS NULL`.
- `UX_Produtos_CategoriaId_Nome`: único em `(CategoriaId, Nome)`, filtro `[DataRemocao] IS NULL`.
- `IX_Produtos_CategoriaId`: não único, declarado explicitamente (o EF não o criaria sozinho porque o índice composto já começa por `CategoriaId`, mas o composto é filtrado e não cobre os removidos, que a exclusão de categoria precisa achar).

## Decisões de implementação

- `Ativa`/`Ativo` usam `HasDefaultValue(true)` com `HasSentinel(true)`: o EF sempre envia `false` explicitamente e deixa o default do banco preencher `true`, evitando o problema clássico de default `bool` em que `false` seria trocado por `true`.
- A ordenação por `Id` desempata de forma estável, mas o SQL Server ordena `uniqueidentifier` começando pelos últimos 6 bytes. Por isso `id asc` **não** segue a ordem de criação dos UUID v7, ao contrário do que a [[ADR-009 Paginação, filtros e ordenação]] sugeria. A ordem continua determinística, que é o que a ADR exige.
- Migrations: só a `Inicial`, aplicada por `dotnet ef database update` (ou bundle) e pela fixture dos testes de integração. A API não migra no startup. Para gerar migrations: `dotnet ef migrations add <Nome> --project src/Infrastructure --startup-project src/Infrastructure --output-dir Persistencia/Migrations` (usa `CatalogoDbContextFactory`, que lê `ConnectionStrings__Catalogo` ou cai no LocalDB só para design-time).

Relacionado: [[API de Categorias]], [[API de Produtos]], [[ADR-001 Remoção de categoria com produtos removidos]], [[ADR-008 Unicidade case-insensitive via collation]], [[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]].
