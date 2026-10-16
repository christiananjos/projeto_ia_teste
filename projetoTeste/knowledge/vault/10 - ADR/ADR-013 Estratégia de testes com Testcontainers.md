---
tipo: adr
status: aceito
data: 2026-10-16
tags: [testes, testcontainers, L-12]
---

# ADR-013: Testes com xUnit, NSubstitute, Shouldly e Testcontainers SQL Server

> Fonte: docs/SPEC.md (Testes: cobertura de 80% na Application, integração com "banco em memória/Testcontainers") e lacuna L-12 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
A spec deixa em aberto se os testes de integração usam banco em memória ou Testcontainers.

## Decisão
- **Unitários:** xUnit, NSubstitute e Shouldly. O `TimeProvider` é substituído por um falso.
- **Integração:** `WebApplicationFactory<Program>` com `Testcontainers.MsSql`. As migrations são aplicadas no banco do container, e o JWT é gerado no próprio teste.
- **Cobertura:** coverlet, com meta de 80% de linhas na Application.
- Não usar EF InMemory nem SQLite.

## Alternativas Consideradas
- EF InMemory: ignora índice filtrado, collation, FK e `ExecuteDelete`.
- SQLite: tem comportamento diferente nesses mesmos pontos.
- FluentAssertions: licença comercial a partir da v8.

## Consequências
- O ambiente local e a CI precisam de Docker.

## Relacionado
- [[ADR-008 Unicidade case-insensitive via collation]]
- [[ADR-001 Remoção de categoria com produtos removidos]]
- [[SQL Server]]
