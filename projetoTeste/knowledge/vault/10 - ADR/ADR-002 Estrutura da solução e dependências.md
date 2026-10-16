---
tipo: adr
status: aceito
data: 2026-10-16
tags: [estrutura, clean-architecture]
---

# ADR-002: Estrutura da solução e dependências entre camadas

> Fonte: docs/SPEC.md (Clean Architecture: Domain, Application, Infrastructure, Api), CLAUDE.md (`dotnet build`/`dotnet test`) e as pastas vazias que já existem em `src/`. Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
O projeto é greenfield. As pastas `src/API`, `src/Application`, `src/Domain`, `src/Infrastructure` e `src/Tests` já existem, mas estão vazias.

## Decisão
- Os projetos `Catalogo.Domain`, `Catalogo.Application`, `Catalogo.Infrastructure` e `Catalogo.Api` ficam direto nas pastas existentes.
- Os testes `Catalogo.UnitTests` e `Catalogo.IntegrationTests` ficam em `src/Tests/`.
- Na raiz ficam `Catalogo.slnx`, `Directory.Build.props`, `Directory.Packages.props` (CPM) e `global.json`.
- O Domain não tem nenhuma dependência. A Api referencia a Infrastructure só para a DI.

## Alternativas Consideradas
- Subpasta extra para cada projeto: um nível a mais sem ganho.
- `.sln` clássico: o SDK 10 já usa `.slnx` como padrão.
- Versão de pacote declarada em cada csproj: fica mais difícil manter as versões alinhadas.

## Consequências
- `dotnet build` e `dotnet test` funcionam a partir da raiz.

## Relacionado
- [[Arquitetura da Solução]]
