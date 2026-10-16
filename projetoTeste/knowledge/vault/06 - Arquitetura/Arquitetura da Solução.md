---
tipo: arquitetura
status: planejado
data: 2026-10-16
tags: [arquitetura, clean-architecture, dotnet]
---

# Arquitetura da Solução

> Fonte: docs/SPEC.md (RNF, Stack). Decisões do 02-architect-sdd em 2026-10-16 ([[ADR-002 Estrutura da solução e dependências]], [[ADR-003 Casos de uso, Result pattern e CQRS leve]]).
> Situação: **desenhada, ainda não implementada**. Ver [[Implementação do Catálogo de Produtos]].

API REST `/api/v1` em .NET 10 para Categoria e Produto, seguindo Clean Architecture. O projeto é greenfield.

## Projetos

| Pasta | Projeto | Depende de |
|---|---|---|
| `src/Domain` | `Catalogo.Domain` | nada (sem pacotes NuGet) |
| `src/Application` | `Catalogo.Application` | Domain, FluentValidation |
| `src/Infrastructure` | `Catalogo.Infrastructure` | Application, EF Core SQL Server |
| `src/API` | `Catalogo.Api` | Application, Infrastructure (esta só para DI) |
| `src/Tests/Catalogo.UnitTests` | testes unitários | Domain, Application |
| `src/Tests/Catalogo.IntegrationTests` | testes de integração | Api |

Na raiz ficam a solução `Catalogo.slnx`, `Directory.Build.props`, `Directory.Packages.props` (CPM) e `global.json`.

## Padrões

- **Domain:** agregados `Categoria` e `Produto`, value object `Sku`, interfaces `ICategoriaRepository`, `IProdutoRepository` e `IUnitOfWork`, exceções `DomainException` e `ConflitoPersistenciaException`. Os Ids são UUID v7 e as datas chegam por `agoraUtc` ([[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]]).
- **Application:** um caso de uso por classe (`XxxUseCase.ExecutarAsync`), 11 no total, sem MediatR nem AutoMapper. Usa Result pattern (`Resultado`/`Erro`/`TipoErro`) e FluentValidation, que roda antes de qualquer acesso ao banco. A leitura passa por `ICategoriaConsultas` e `IProdutoConsultas` (CQRS leve).
- **Infrastructure:** `CatalogoDbContext` (implementa `IUnitOfWork`), repositórios e consultas com `AsNoTracking`. Ver [[Persistência EF Core]].
- **API:** Minimal APIs com `MapGroup` e `TypedResults`. Ver [[Tratamento de Erros e ProblemDetails]] e [[Provedor de Identidade JWT]].

## Casos de uso

- Categoria: `CriarCategoria`, `AtualizarCategoria`, `RemoverCategoria`, `ObterCategoria`, `ListarCategorias`.
- Produto: `CriarProduto`, `AtualizarProduto`, `AlterarStatusProduto`, `RemoverProduto`, `ObterProduto`, `ListarProdutos`.

No POST de produto, as checagens seguem esta ordem: 400 → 422 (categoria) → 409 (SKU) → 409 (nome). Ver [[ADR-006 Contrato de Produto]].

## Contratos

- Categoria: [[ADR-005 Contrato de Categoria]]
- Produto: [[ADR-006 Contrato de Produto]]
- Paginação: [[ADR-009 Paginação, filtros e ordenação]]
- Nomes com espaços: [[ADR-007 Espaços nas pontas rejeitados com 400]]
- Preço: [[ADR-014 Validação de preço]]

## Testes

xUnit, NSubstitute, Shouldly e Testcontainers.MsSql, com meta de 80% de cobertura na Application. Ver [[ADR-013 Estratégia de testes com Testcontainers]].
