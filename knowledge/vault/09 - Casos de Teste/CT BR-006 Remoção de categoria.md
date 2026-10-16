---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, categoria, remocao, br-006, adr-001]
---

# CT BR-006 Remoção de categoria

> Fonte: docs/SPEC.md (BR-006), [[ADR-001 Remoção de categoria com produtos removidos]]. Testes do 05-test-engineer, 2026-10-16.

**Regra:** com ao menos um produto não removido (ativo ou inativo) → 409 `categoria.possui_produtos`. Sem produtos ou só com removidos → 204 e exclusão física (os removidos vão junto, numa transação).

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| Sem produtos não removidos | chama `ExcluirComProdutosRemovidosAsync`, 204 | `RemoverCategoriaUseCaseTests.SemProdutosNaoRemovidos_*`, `Delete_SemProdutos_Retorna204` | Sim |
| Com produto não removido | 409, sem excluir | `ComProdutoNaoRemovido_Retorna409SemExcluir`, `Delete_ComProdutoNaoRemovido_Retorna409` | Sim |
| Corrida FK 547 durante a exclusão | 409 | `ComCorridaDeFkNaExclusao_Retorna409`, `Delete_ComCorridaDeFk_Retorna409` | Sim |
| Conflito de unicidade na exclusão (não esperado) | exceção propaga | `ComConflitoDeUnicidade_NaoEngoleAExcecao` | Sim |
| Inexistente | 404 | `CategoriaInexistente_Retorna404`, `Delete_Inexistente_Retorna404` | Sim |
| FK sem cascata (`Restrict`) e nome fixo | conforme spec | `ModeloEfTests.FkDeCategoria_TemNomeFixoESemCascata` | Sim |
| Produto ativo ou inativo bloqueia (SQL real) | 409 | `CategoriasE2ETests.RemoverComProdutoNaoRemovido_Retorna409` | Não (sem Docker) |
| Só produtos removidos → 204 e produtos apagados (SQL real) | 204, 0 produtos | `CategoriasE2ETests.RemoverComApenasProdutosRemovidos_*` | Não (sem Docker) |
| Repositório com produto ativo → `Referencia` e rollback (SQL real) | nada apagado | `RepositoriosSqlTests.ExcluirCategoriaComProdutoNaoRemovido_*` | Não (sem Docker) |

Relacionado: [[API de Categorias]], [[CT BR-005 Exclusão lógica]], [[Estratégia e execução dos testes]].
