---
tipo: arquitetura
status: planejado
data: 2026-10-16
tags: [api, erros, problemdetails]
---

# Tratamento de Erros e ProblemDetails

> Fonte: docs/SPEC.md (Endpoints: "Erros no formato ProblemDetails (RFC 9457)") e [[ADR-004 Minimal APIs com ProblemDetails]].
> Situação: **planejado, ainda não implementado**.

| Origem | Status | Código (`code`) |
|---|---|---|
| validator, binding ou JSON inválido | 400 | `validacao` (`ValidationProblemDetails.errors` em camelCase) |
| `DomainException` | 400 | `dominio` |
| sem token / sem role `Admin` | 401 / 403 | sem código |
| não encontrado (inclui item removido e id não-guid na rota) | 404 | `categoria.nao_encontrada`, `produto.nao_encontrado` |
| duplicidade, ou categoria com produtos | 409 | `categoria.nome_duplicado`, `categoria.possui_produtos`, `produto.sku_duplicado`, `produto.nome_duplicado` |
| categoria inexistente ou inativa (BR-004) | 422 | `produto.categoria_invalida` |
| rate limit | 429 | sem código; vem com `Retry-After` |
| erro inesperado | 500 | `detail` genérico, sem stack trace |

Todo ProblemDetails traz `type`, `title`, `status`, `detail`, `instance`, `code` e `traceId`.

Relacionado: [[Arquitetura da Solução]], [[ADR-011 Rate limiting por IP]].
