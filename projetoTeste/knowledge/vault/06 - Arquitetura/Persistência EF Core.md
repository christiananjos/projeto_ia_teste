---
tipo: arquitetura
status: planejado
data: 2026-10-16
tags: [ef-core, sql-server, persistencia]
---

# Persistência EF Core

> Fonte: docs/SPEC.md (Modelo de Dados, Índices). Decisões do 02-architect-sdd em 2026-10-16.
> Situação: **planejada, ainda não implementada**. Banco: [[SQL Server]].

## Tabelas

- `Categorias`: `Id uniqueidentifier` PK, `Nome nvarchar(100)` com collation `Latin1_General_100_CI_AS`, `Descricao nvarchar(500) NULL`, `Ativa bit`, `DataCriacao` e `DataAtualizacao` em `datetime2`.
- `Produtos`: `Id` PK, `CategoriaId` (FK `FK_Produtos_Categorias_CategoriaId`, **Restrict**), `Nome nvarchar(150)` CI_AS, `Sku nvarchar(40)` CI_AS (conversão do VO `Sku`), `Descricao nvarchar(1000) NULL`, `Preco decimal(18,2)`, `Ativo bit`, `DataCriacao`, `DataAtualizacao`, `DataRemocao NULL`.

## Índices

Os nomes são fixos porque a tradução de erros depende deles.

- `UX_Categorias_Nome`: único.
- `UX_Produtos_Sku`: único, filtrado por `[DataRemocao] IS NULL`.
- `UX_Produtos_CategoriaId_Nome`: único, filtrado por `[DataRemocao] IS NULL`.

Motivo: [[ADR-008 Unicidade case-insensitive via collation]].

## Comportamentos

- Produto tem um global query filter `DataRemocao == null` ([[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]]).
- Todo `DateTime` passa por um conversor UTC (BR-007).
- Erros SQL 2601 e 2627 viram `ConflitoPersistenciaException(Unicidade)`. Erro 547 vira `ConflitoPersistenciaException(Referencia)`.
- A remoção de categoria roda numa transação com `ExecuteDelete`: primeiro os produtos já removidos, depois a categoria ([[ADR-001 Remoção de categoria com produtos removidos]]).
- A migration `Inicial` fica versionada. A API não aplica migrations no startup.
- A connection string `ConnectionStrings:Catalogo` vem só de user-secrets ou de variável de ambiente.
