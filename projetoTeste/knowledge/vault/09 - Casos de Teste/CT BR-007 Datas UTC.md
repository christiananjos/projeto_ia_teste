---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, datas, utc, br-007]
---

# CT BR-007 Datas UTC

> Fonte: docs/SPEC.md (BR-007), [[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]]. Testes do 05-test-engineer, 2026-10-16.

**Regra:** `DataCriacao` e `DataAtualizacao` são definidas pelo servidor em UTC; o cliente não as envia.

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| Criação com `TimeProvider` falso | datas = instante do relógio, `Kind=Utc`, criação = atualização | `CategoriaTests.Criar_*`, `ProdutoTests.Criar_DefineAsDatasPeloServidorEmUtc`, `CriarCategoriaUseCaseTests.ComNomeLivre_*` | Sim |
| Data `Local`/`Unspecified` no domínio | `DomainException` | `Criar_ComDataQueNaoEhUtc_*`, `DefinirStatus_ComDataQueNaoEhUtc_*`, `Remover_ComDataQueNaoEhUtc_*` | Sim |
| Atualização muda só `DataAtualizacao` | `DataCriacao` intacta | `Atualizar_SubstituiCampos*`, `AtualizarProdutoUseCaseTests.NaMesmaCategoria_*` | Sim |
| `dataCriacao` enviada no corpo | ignorada | `CategoriaEndpointsTests.Post_ComDatasNoCorpo_IgnoraOsValoresDoCliente` | Sim |
| Serialização com sufixo `Z` | `...Z` | `Post_ComDadosValidos_Retorna201ComLocationECorpo`, `Listar_DevolveOsItensNoFormatoDaSpec` | Sim |
| Conversor EF: grava em UTC, lê com `Kind=Utc` | conforme spec | `UtcDateTimeConverterTests` | Sim |
| Leitura do banco com `Kind=Utc` (SQL real) | `Utc` | `ProdutosE2ETests.ProdutoRemovido_*` (`DataRemocao`), `CategoriasE2ETests.CriarEObter_PersisteComDatasUtc` | Não (sem Docker) |

Relacionado: [[Modelo de Dados do Catálogo]], [[Estratégia e execução dos testes]].
