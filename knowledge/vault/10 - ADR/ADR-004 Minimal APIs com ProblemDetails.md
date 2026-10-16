---
tipo: adr
status: aceito
data: 2026-10-16
tags: [api, problemdetails]
---

# ADR-004: Minimal APIs com ProblemDetails (RFC 9457)

> Fonte: docs/SPEC.md (base `/api/v1`, erros em ProblemDetails). Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
A spec exige ProblemDetails, mas não define o estilo de API.

## Decisão
- Minimal APIs com `MapGroup` e `TypedResults`.
- `AddProblemDetails`, `IExceptionHandler` e `UseStatusCodePages`.
- Mapeamento de erros: `Validacao` → 400, `NaoEncontrado` → 404, `Conflito` → 409, `RegraNegocio` → 422, `DomainException` → 400 e erro inesperado → 500 genérico.
- As respostas de erro levam as extensões `code` e `traceId`.
- As rotas usam `{id:guid}`, então um id malformado responde 404.

## Alternativas Consideradas
- Controllers MVC: mais cerimônia.
- `Asp.Versioning`: exagero quando só existe uma versão.

## Consequências
- Tabela de códigos em [[Tratamento de Erros e ProblemDetails]].

## Relacionado
- [[ADR-003 Casos de uso, Result pattern e CQRS leve]]
