---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, problemdetails, erros, cancelamento]
---

# CT Tratamento de erros e ProblemDetails

> Fonte: spec técnica §6.3, [[ADR-004 Minimal APIs com ProblemDetails]], pedido do 04-reviewer-sdd (cancelamento → 499). Testes do 05-test-engineer em `src/Tests/Catalogo.IntegrationTests/Api/`, 2026-10-16. Todos executados.

Todo erro verificado passa por `HttpExtensions.DeveSerProblemaAsync`, que exige `application/problem+json`, `type = https://httpstatuses.io/{status}`, `title`, `status`, `instance`, `traceId` e `code`.

| Cenário | Esperado | Teste |
|---|---|---|
| Validação | 400 `validacao` + `errors` com chaves camelCase (`categoriaId`, `nome`, `sku`, `preco`) | `Post_ComVariosCamposInvalidos_Retorna400ComTodosOsCamposEmCamelCase` |
| Chaves camelCase e mensagens sem repetição | conforme spec | `ValidacaoExtensionsTests.ParaErro_*` |
| JSON malformado / tipo inválido na query | 400 `requisicao_invalida` | `Post_ComJsonMalformado_Retorna400`, `Listar_ComTipoInvalidoNaQueryString_Retorna400` |
| Id não-GUID | 404 `nao_encontrado` | `GetPorId_ComIdQueNaoEhGuid_Retorna404DeRota` |
| `DomainException` não capturada | 400 `dominio` com a mensagem, log `Warning`, sem `Error` | `TratamentoDeExcecoesTests.DomainException_Retorna400ComCodigoDominioELogWarning` |
| Exceção inesperada com dado sensível na mensagem | 500 `erro_interno`, `detail` genérico, sem a mensagem nem o tipo da exceção, log `Error` | `ExcecaoInesperada_Retorna500SemVazarDetalhesELogaError` |
| Cliente cancela a requisição no meio da consulta | status final 499, nenhum log `Error` | `RequisicaoCanceladaPeloCliente_Responde499SemLogDeErro` |

**Achado do cancelamento:** a cobertura mostrou que o ramo de `OperationCanceledException` do `ExcecaoGlobalHandler` teve 0 execuções. O 499 vem do `ExceptionHandlerMiddleware` do ASP.NET Core, que trata o cancelamento antes de chamar os `IExceptionHandler`. Isso confirma a observação 🟢 do reviewer: o ramo é redundante, mas não causa dano. Ver [[Tratamento de Erros e ProblemDetails]].

Relacionado: [[Estratégia e execução dos testes]].
