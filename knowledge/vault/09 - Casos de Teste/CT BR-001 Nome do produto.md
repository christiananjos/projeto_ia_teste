---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, produto, nome, br-001]
---

# CT BR-001 Nome do produto

> Fonte: docs/SPEC.md (BR-001), [[ADR-007 Espaços nas pontas rejeitados com 400]], [[ADR-008 Unicidade case-insensitive via collation]]. Testes do 05-test-engineer, 2026-10-16.

**Regra:** obrigatório, 3–150 caracteres, sem espaços nas pontas, único na categoria (sem diferenciar maiúsculas) entre não removidos.

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| Tamanho 2/151 | 400 `errors.nome` / `DomainException` | `Nome_LimitesExatosDe3A150`, `ProdutoTests.Criar_ComNomeForaDoLimite_*` | Sim |
| Tamanho 3/150 | aceito | os mesmos | Sim |
| Espaço no início/fim | 400 "O nome não pode começar nem terminar com espaços." | `NomeComEspacoNasPontas_TemErro`, `Criar_ComNomeComEspacoNasPontas_*` | Sim |
| Nulo/vazio | 400 "O nome é obrigatório." | `SemNome_TemErro` | Sim |
| Nome repetido na categoria (criar) | 409 `produto.nome_duplicado`, sem adicionar | `CriarProdutoUseCaseTests.ComNomeJaUsadoNaCategoria_*`, `Post_ComNomeDuplicadoNaCategoria_Retorna409` | Sim |
| Nome de outro produto (atualizar), ignorando o próprio id | 409 | `AtualizarProdutoUseCaseTests.ComNomeDeOutroProdutoNaCategoria_Retorna409`, `Put_ComNomeDuplicado_Retorna409` | Sim |
| Corrida: `UX_Produtos_CategoriaId_Nome` | 409 nome | `ComCorridaDetectadaPeloBanco_*` | Sim |
| `TECLADO MECÂNICO` x `Teclado Mecânico` na mesma categoria (SQL real) e mesmo nome em outra categoria | 409 / 201 | `ProdutosE2ETests.NomeDuplicadoNaMesmaCategoria_Retorna409EmOutraCategoriaAceita` | Não (sem Docker) |
| Tradução do índice composto (SQL real) | `Unicidade`, `UX_Produtos_CategoriaId_Nome` | `RepositoriosSqlTests.NomeDuplicadoNaCategoria_*` | Não (sem Docker) |

Relacionado: [[API de Produtos]], [[Estratégia e execução dos testes]].
