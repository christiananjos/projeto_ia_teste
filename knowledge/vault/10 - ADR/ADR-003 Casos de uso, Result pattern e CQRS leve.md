---
tipo: adr
status: aceito
data: 2026-10-16
tags: [application, padroes]
---

# ADR-003: Casos de uso explícitos, Result pattern e CQRS leve (sem MediatR)

> Fonte: docs/SPEC.md (FluentValidation, cobertura de 80% na Application). Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
São 11 operações simples. MediatR e AutoMapper passaram a ter licença comercial.

## Decisão
- Uma classe por caso de uso (`XxxUseCase.ExecutarAsync`), injetada direto no endpoint.
- Erros esperados (validação, 404, 409, 422) voltam como `Resultado`/`Erro`, sem exceção.
- Escrita usa Repository + Unit of Work, com as interfaces no Domain.
- Leitura usa serviços de consulta (`ICategoriaConsultas`, `IProdutoConsultas`) que projetam direto para DTO com `AsNoTracking`.
- O mapeamento é manual, com métodos estáticos `De(...)`.

## Alternativas Consideradas
- MediatR com pipeline de validação: custo de licença e indireção.
- Exceções de aplicação mapeadas por middleware: usa exceção como fluxo de controle.
- Um service por agregado: dependências demais em cada classe.
- Repositório genérico: vaza `IQueryable`.

## Consequências
- Os casos de uso ficam testáveis com NSubstitute, e o fluxo de cada um fica explícito.

## Relacionado
- [[Arquitetura da Solução]]
- [[Tratamento de Erros e ProblemDetails]]
