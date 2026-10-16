---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, produto, exclusao-logica, br-005]
---

# CT BR-005 Exclusão lógica

> Fonte: docs/SPEC.md (BR-005), [[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]]. Testes do 05-test-engineer, 2026-10-16.

**Regra:** `DELETE` marca `DataRemocao`; o produto some de todas as consultas e libera nome e SKU; remover de novo dá 404.

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| `Remover` preenche `DataRemocao` e `DataAtualizacao` | `EstaRemovido = true` | `ProdutoTests.Remover_PreencheDataRemocaoEDataAtualizacao` | Sim |
| `Remover` duas vezes no domínio | `DomainException` | `Remover_ProdutoJaRemovido_LancaDomainException` | Sim |
| Caso de uso com produto removido (repositório devolve nulo) | 404, sem salvar | `RemoverProdutoUseCaseTests.ProdutoJaRemovido_Retorna404SemSalvar`, `Delete_JaRemovido_Retorna404` | Sim |
| Produto inativo pode ser removido | 204 | `ProdutoInativo_TambemPodeSerRemovido` | Sim |
| Query filter `DataRemocao == null` no modelo e no SQL gerado | presente | `ModeloEfTests.Produto_TemQueryFilter*`, `FiltroPorNome_GeraLikeParametrizadoEAplicaOQueryFilter` | Sim |
| Removido: GET, PUT, PATCH e DELETE dão 404 e a listagem não mostra (SQL real) | 404 / 0 itens | `ProdutosE2ETests.ProdutoRemovido_Retorna404EmTodasAsOperacoes` | Não (sem Docker) |
| Reuso de SKU e nome depois da remoção (SQL real) | 201 | `ProdutosE2ETests.DepoisDeRemover_SkuENomePodemSerReusados` | Não (sem Docker) |
| Repositórios ignoram removidos nas checagens (SQL real) | `ExisteSku`/`ExisteNome` falsos | `RepositoriosSqlTests.Repositorios_IgnoramProdutosRemovidosNasChecagens` | Não (sem Docker) |

Relacionado: [[API de Produtos]], [[CT BR-006 Remoção de categoria]], [[Estratégia e execução dos testes]].
