---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, categoria, req-001]
---

# CT REQ-001 Categorias

> Fonte: docs/SPEC.md (REQ-001), [[ADR-005 Contrato de Categoria]], [[ADR-008 Unicidade case-insensitive via collation]]. Testes do 05-test-engineer, 2026-10-16. A remoção está em [[CT BR-006 Remoção de categoria]].

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| POST válido | 201, `Location`, `ativa = true` | `CriarCategoriaUseCaseTests.ComNomeLivre_*`, `CategoriaEndpointsTests.Post_ComDadosValidos_*` | Sim |
| Nome 2/101, espaço nas pontas, só espaços; descrição 501 | 400 por campo | `CriarCategoriaRequestValidatorTests`, `CategoriaTests` | Sim |
| Nome duplicado (checagem prévia e corrida em `UX_Categorias_Nome`) | 409 `categoria.nome_duplicado` | `ComNomeJaUsado_*`, `ComCorridaDeUnicidadeNoBanco_*`, `Post_ComNomeDuplicado_Retorna409` | Sim |
| PUT sem `ativa` | 400 `errors.ativa` | `SemAtiva_*`, `Put_SemAtiva_Retorna400` | Sim |
| PUT inexistente / nome de outra categoria | 404 / 409 | `AtualizarCategoriaUseCaseTests`, `Put_Inexistente_*`, `Put_ComNomeDeOutraCategoria_*` | Sim |
| PUT checa duplicidade ignorando o próprio id | `ExisteNomeAsync(nome, id)` | `VerificaNomeDuplicadoIgnorandoAPropriaCategoria` | Sim |
| GET por id existente / inexistente / id não-GUID | 200 / 404 `categoria.nao_encontrada` / 404 `nao_encontrado` | `GetPorId_*` | Sim |
| Listagem: padrões 1/20, filtros repassados, `page=0`, `pageSize=101`, `ativa=talvez` | 200 / 400 | `Listar_*` (Api e UseCases) | Sim |
| `PERIFÉRICOS` x `Periféricos` (SQL real) | 409 | `CategoriasE2ETests.NomeDuplicadoComOutraCaixa_Retorna409` | Não (sem Docker) |
| Filtro `nome` CI + `ativa`, ordem por nome (SQL real) | conforme spec | `CategoriasE2ETests.Listar_FiltraPorNomeEAtivaEOrdenaPorNome` | Não (sem Docker) |

Relacionado: [[API de Categorias]], [[Estratégia e execução dos testes]].
