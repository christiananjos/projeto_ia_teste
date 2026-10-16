---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, xunit, testcontainers, cobertura]
---

# Estratégia e execução dos testes

> Fonte: código em `src/Tests/` criado pelo 05-test-engineer em 2026-10-16, seguindo [[ADR-013 Estratégia de testes com Testcontainers]] e a spec técnica §7. Resultados medidos nesta máquina em 2026-10-16 (SDK 10.0.302, Docker CLI instalado, mas sem o daemon rodando).

## Projetos

| Projeto | O que cobre | Precisa de Docker? |
|---|---|---|
| `src/Tests/Catalogo.UnitTests` | Entidades e `Sku`, validators (limites exatos), casos de uso com NSubstitute (ordem das checagens e mapeamento de `ConflitoPersistenciaException`), `Comum`, modelo EF sem conexão (índices, filtros, collation, FK, query filter, migration em dia, SQL parametrizado), `UtcDateTimeConverter`, DI | Não |
| `src/Tests/Catalogo.IntegrationTests/Api` | Pipeline HTTP completo via `WebApplicationFactory<Program>` com a persistência substituída por NSubstitute: JWT, policies, rate limit, binding, validação, ProblemDetails, handler global, cancelamento | Não |
| `src/Tests/Catalogo.IntegrationTests/Persistencia` | Ponta a ponta com SQL Server real (`Testcontainers.MsSql`, um container por coleção, `MigrateAsync`, limpeza Produtos → Categorias entre testes) e tradução de erros SQL 2601/2627/547 | **Sim** |

Pacotes (CPM em `Directory.Packages.props`): xunit.v3 3.2.2, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.11.0, NSubstitute 5.3.0, Shouldly 4.3.0, coverlet.collector 10.1.0, Microsoft.Extensions.TimeProvider.Testing 10.11.0, Microsoft.AspNetCore.Mvc.Testing 10.0.13, Testcontainers.MsSql 4.16.0. A cobertura usa `src/Tests/coverage.runsettings` (via `RunSettingsFilePath` nos dois `.csproj`), que exclui as migrations e o código gerado.

## Comandos

```bash
dotnet build
dotnet test --collect:"XPlat Code Coverage"
```

Sem Docker, os 27 testes de `Persistencia/` aparecem como **ignorados**, com o motivo (`DockerUnavailableException`). Para a CI, defina `CATALOGO_EXIGIR_DOCKER=true`: aí eles **falham** em vez de serem ignorados.

## Resultado em 2026-10-16

- Build: 0 avisos, 0 erros.
- `Catalogo.UnitTests`: 288 aprovados.
- `Catalogo.IntegrationTests`: 79 aprovados, 27 ignorados (sem Docker).
- Cobertura consolidada (linhas): Application 100% (ramos 100%), Domain 97%, Api 94,3%, Infrastructure 41,2%. A Infrastructure fica baixa porque repositórios e consultas só rodam com SQL Server ([[Testes de integração com SQL Server pendentes]]).

## Casos por regra
- [[CT REQ-001 Categorias]]
- [[CT BR-001 Nome do produto]]
- [[CT BR-002 Preço]]
- [[CT BR-003 SKU]]
- [[CT BR-004 Categoria válida]]
- [[CT BR-005 Exclusão lógica]]
- [[CT BR-006 Remoção de categoria]]
- [[CT BR-007 Datas UTC]]
- [[CT REQ-003 e REQ-004 Listagem e status]]
- [[CT Autenticação, autorização e rate limit]]
- [[CT Tratamento de erros e ProblemDetails]]

Workflow manual com cURL: `docs/api/testes-api.md` (ver [[API de Categorias]] e [[API de Produtos]]).
