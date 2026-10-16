---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, produto, categoria, br-004]
---

# CT BR-004 Categoria válida

> Fonte: docs/SPEC.md (BR-004), [[ADR-005 Contrato de Categoria]], [[ADR-006 Contrato de Produto]]. Testes do 05-test-engineer, 2026-10-16.

**Regra:** produto só é criado ou movido para categoria que exista e esteja ativa; senão 422 `produto.categoria_invalida`. Manter a categoria atual inativa no PUT é aceito.

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| Criar com categoria inexistente | 422, sem checar SKU | `CriarProdutoUseCaseTests.ComCategoriaInexistente_Retorna422` | Sim |
| Criar com categoria inativa | 422 | `ComCategoriaInativa_Retorna422`, `Post_ComCategoriaInativa_Retorna422` | Sim |
| Categoria inválida e SKU duplicado ao mesmo tempo | 422 vence | `CategoriaInvalidaTemPrioridadeSobreSkuDuplicado` | Sim |
| Mover para inexistente / inativa | 422, produto não muda | `AtualizarProdutoUseCaseTests.MovendoParaCategoriaInexistente_*`, `MovendoParaCategoriaInativa_*`, `Put_MovendoParaCategoriaInexistente_Retorna422` | Sim |
| Manter a categoria atual inativa | 200 | `MantendoACategoriaAtualInativa_Aceita` | Sim |
| Mover para categoria ativa | 200 com `categoriaNome` da nova | `MovendoParaOutraCategoriaAtiva_UsaONomeDaNovaCategoria` | Sim |
| Corrida: FK 547 ao salvar | 422 | `ComCorridaDetectadaPeloBanco_*` (Referencia) | Sim |
| Categoria inativada via PUT, depois POST de produto (SQL real) | 422 | `ProdutosE2ETests.CriarEmCategoriaInativa_Retorna422` | Não (sem Docker) |
| FK traduzida com o nome certo (SQL real) | `Referencia`, `FK_Produtos_Categorias_CategoriaId` | `RepositoriosSqlTests.ProdutoComCategoriaInexistente_*` | Não (sem Docker) |

Relacionado: [[API de Produtos]], [[API de Categorias]], [[Estratégia e execução dos testes]].
