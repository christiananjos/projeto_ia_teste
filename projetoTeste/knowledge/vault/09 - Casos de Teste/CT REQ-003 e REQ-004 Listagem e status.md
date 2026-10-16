---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, produto, listagem, paginacao, req-003, req-004]
---

# CT REQ-003 e REQ-004 Listagem e status

> Fonte: docs/SPEC.md (REQ-003, REQ-004), [[ADR-009 Paginação, filtros e ordenação]], [[ADR-006 Contrato de Produto]]. Testes do 05-test-engineer, 2026-10-16.

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| Sem parâmetros | `ListarProdutosQuery()` (1, 20, `ativo` nulo) | `ProdutoEndpointsTests.Listar_SemParametros_UsaOsPadroes` | Sim |
| Todos os filtros e ordenação na query string | repassados exatamente | `Listar_RepassaFiltrosEOrdenacao` | Sim |
| `ordenarPor`/`direcao` sem diferenciar maiúsculas; valor desconhecido | aceito / 400 | `ListarProdutosQueryValidatorTests`, `Listar_ComParametroForaDoDominio_Retorna400` | Sim |
| `page=0`, `pageSize=0/101`, `page=int.MaxValue` | 400 / 400 / aceito sem overflow | `Paginacao_RespeitaOsLimites`, `PaginaResponseTests.CalcularDeslocamento_*` | Sim |
| `totalPaginas = ceil(total/pageSize)`, 0 sem itens | conforme spec | `PaginaResponseTests.CalcularTotalPaginas` | Sim |
| `page=abc`, `ativo=talvez`, `categoriaId=x` | 400 `requisicao_invalida` | `Listar_ComTipoInvalidoNaQueryString_Retorna400` | Sim |
| Filtro `nome` gera `LIKE` parametrizado | sem SQL concatenado | `ModeloEfTests.FiltroPorNome_GeraLikeParametrizadoEAplicaOQueryFilter` | Sim |
| PATCH inativar / reativar / mesmo valor (idempotente) / sem `ativo` / inexistente | 204 / 204 / 204 sem mudar data / 400 / 404 | `AlterarStatusProdutoUseCaseTests`, `PatchStatus_*`, `ProdutoTests.DefinirStatus_*` | Sim |
| Filtro, ordenação por nome e preço desc, `nome` CI, `categoriaId`, página 2, página além do fim (SQL real) | conforme spec | `ProdutosE2ETests.Listar_FiltraOrdenaEPagina` | Não (sem Docker) |
| Desempate por `id asc` na ordem do SQL Server (`SqlGuid`) | estável | `Listar_ComPrecosIguais_DesempataPorId` | Não (sem Docker) |
| `%` e `_` no filtro tratados como texto (SQL real) | 0 itens | `Listar_ComCuringasNoFiltroDeNome_TrataComoTexto` | Não (sem Docker) |
| Inativo sai da listagem padrão, aparece com `ativo=false` e no GET por id (SQL real) | conforme spec | `Inativar_TiraDaListagemPadraoMasContinuaNoGetPorId` | Não (sem Docker) |
| PATCH com o mesmo valor não muda `dataAtualizacao` (SQL real) | igual | `AlterarStatusComOMesmoValor_EhIdempotente` | Não (sem Docker) |

Relacionado: [[API de Produtos]], [[Estratégia e execução dos testes]].
