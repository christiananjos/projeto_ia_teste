---
tipo: arquitetura
status: implementado
data: 2026-10-16
tags: [api, erros, problemdetails]
---

# Tratamento de Erros e ProblemDetails

> Fonte: docs/SPEC.md (Endpoints: "Erros no formato ProblemDetails (RFC 9457)") e [[ADR-004 Minimal APIs com ProblemDetails]]. Implementado pelo 03-dotnet-specialist em 2026-10-16 em `src/API/Http/` (`ProblemDetailsPadrao`, `ResultadoHttpExtensions`, `ExcecaoGlobalHandler`).

| Origem | Status | Código (`code`) |
|---|---|---|
| validator (FluentValidation) | 400 | `validacao` (`ValidationProblemDetails.errors` com chaves camelCase) |
| binding de query string com tipo inválido ou JSON malformado/ausente | 400 | `requisicao_invalida` (sem `errors`; o framework não informa o campo) |
| `DomainException` | 400 | `dominio` |
| sem token / token inválido | 401 | `nao_autenticado` |
| sem role `Admin` | 403 | `acesso_negado` |
| caso de uso não encontrou (inclui item removido) | 404 | `categoria.nao_encontrada`, `produto.nao_encontrado` |
| rota inexistente ou id não-GUID na rota | 404 | `nao_encontrado` |
| duplicidade, ou categoria com produtos | 409 | `categoria.nome_duplicado`, `categoria.possui_produtos`, `produto.sku_duplicado`, `produto.nome_duplicado` |
| categoria inexistente ou inativa (BR-004) | 422 | `produto.categoria_invalida` |
| rate limit | 429 | `limite_requisicoes`, com header `Retry-After` (segundos) |
| erro inesperado | 500 | `erro_interno`, `detail` genérico, sem stack trace |

Todo ProblemDetails traz `type` (`https://httpstatuses.io/{status}`), `title` em português, `status`, `detail`, `instance` (path), `code` e `traceId`. As respostas geradas pelo framework (401, 403, 404 de rota, 400 de binding) saem pelo `UseStatusCodePages` + `CustomizeProblemDetails`, que preenche título, detalhe e `code` padrão por status. Os códigos genéricos (`requisicao_invalida`, `nao_autenticado`, `acesso_negado`, `nao_encontrado`, `limite_requisicoes`, `erro_interno`) foram definidos na implementação, porque a especificação só pedia `code` em todo ProblemDetails sem nomear esses casos.

Níveis de log no `ExcecaoGlobalHandler` (método e path; nunca token, connection string ou corpo):
- `OperationCanceledException` com `HttpContext.RequestAborted` cancelado (cliente desconectou): log `Information`, status 499 (`StatusCodes.Status499ClientClosedRequest`, se a resposta ainda não começou), sem corpo. Não vira 500 nem log `Error`. Observação do 04-reviewer-sdd (2026-10-16): no ASP.NET Core 8+, o próprio `ExceptionHandlerMiddleware` já trata `OperationCanceledException`/`IOException` com `RequestAborted` cancelado (log `Debug` e 499) antes de chamar os `IExceptionHandler`. Por isso esse ramo do handler é uma defesa redundante e, na prática, pouco executado. A afirmação vem do código do framework e não foi verificada em execução nesta rodada. Um teste de integração pode confirmar.
- `DomainException` e `BadHttpRequestException` (4xx): log `Warning`.
- Qualquer outra exceção (500): log `Error`.

Comportamento conferido em execução local em 2026-10-16 (401, 403, 400 de binding, 400 de validação, 404 de id malformado, 429 e 500 sem banco). Relacionado: [[Arquitetura da Solução]], [[ADR-011 Rate limiting por IP]], [[API de Categorias]], [[API de Produtos]].
