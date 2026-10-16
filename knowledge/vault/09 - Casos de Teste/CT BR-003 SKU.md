---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, produto, sku, br-003, regressao]
---

# CT BR-003 SKU

> Fonte: docs/SPEC.md (BR-003), [[ADR-008 Unicidade case-insensitive via collation]], pedido do 04-reviewer-sdd (regressão de [[SKU aceita quebra de linha no final]]). Testes escritos e executados pelo 05-test-engineer em 2026-10-16.

**Regra:** obrigatório, 3–40 caracteres, só letras ASCII, números e hífen (`^[A-Za-z0-9-]+\z`), único globalmente entre produtos não removidos, sem diferenciar maiúsculas, imutável depois da criação.

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| `"TEC-001\n"`, `"AB\n"`, `"TEC-001\r\n"` no validator | erro em `sku` | `CriarProdutoRequestValidatorTests.SkuComQuebraDeLinhaNoFinal_TemErroNoSku` | Sim |
| Os mesmos valores em `Sku.Criar` (e `"\nTEC-001"`) | `DomainException` | `SkuTests.Criar_ComQuebraDeLinha_LancaDomainException` | Sim |
| `"TEC-001\n"`/`"AB\n"` no caso de uso | 400, sem acessar o banco | `CriarProdutoUseCaseTests.ComSkuTerminandoEmQuebraDeLinha_Retorna400` | Sim |
| `"TEC-001\n"`/`"AB\n"` via HTTP | 400 `errors.sku` | `ProdutoEndpointsTests.Post_ComSkuTerminandoEmQuebraDeLinha_Retorna400` | Sim |
| Tamanho 2/41, vazio, nulo, espaço, `_`, `.`, acento | 400 / `DomainException` | `SkuInvalido_TemErroNoSku`, `Criar_ForaDoTamanho_*`, `Criar_ComCaractereInvalido_*` | Sim |
| Limites 3 e 40 | aceito | `Sku_LimitesExatosDe3A40`, `Criar_ComLimitesExatos_Aceita` | Sim |
| SKU repetido (checagem prévia) | 409 `produto.sku_duplicado`, sem checar nome | `CriarProdutoUseCaseTests.ComSkuJaUsado_Retorna409SemVerificarONome` | Sim |
| Corrida: banco viola `UX_Produtos_Sku` | 409 `produto.sku_duplicado` | `ComCorridaDetectadaPeloBanco_MapeiaParaOErroDeNegocio`, `Post_ComCorridaDeSkuNoBanco_Retorna409DeSku` | Sim |
| `tec-001` x `TEC-001` em outra categoria (SQL real) | 409 | `ProdutosE2ETests.SkuDuplicadoComOutraCaixaEmOutraCategoria_Retorna409` | Não (sem Docker) |
| Índice `UX_Produtos_Sku` traduzido com o nome certo (SQL real) | `ConflitoPersistenciaException(Unicidade, "UX_Produtos_Sku")` | `RepositoriosSqlTests.SkuDuplicadoComOutraCaixa_*` | Não (sem Docker) |
| PUT com `sku` no corpo | ignorado, SKU mantido | `Put_IgnoraSkuEAtivoEnviadosNoCorpo`, `ProdutoTests.Atualizar_*MantemSku*`, `ProdutosE2ETests.Atualizar_NaoAlteraOSku` | Sim (o E2E não) |
| Modelo EF: índice único filtrado por `[DataRemocao] IS NULL`, collation CI | conforme spec | `ModeloEfTests.IndiceDeSku_*`, `NomesESku_UsamCollationCaseInsensitive` | Sim |

Relacionado: [[API de Produtos]], [[CT BR-005 Exclusão lógica]], [[Estratégia e execução dos testes]].
