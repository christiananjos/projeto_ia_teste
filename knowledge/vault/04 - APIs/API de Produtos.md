---
tipo: api
status: implementado
data: 2026-10-16
tags: [api, produto, rest, paginacao]
---

# API de Produtos

> Fonte: docs/SPEC.md (REQ-002 a REQ-004, BR-001 a BR-007, Endpoints › Produtos), [[ADR-006 Contrato de Produto]], [[ADR-009 Paginação, filtros e ordenação]]. Implementado pelo 03-dotnet-specialist em 2026-10-16 em `src/API/Endpoints/ProdutoEndpoints.cs`.

Base: `/api/v1/produtos`. Rotas com `{id}` usam `{id:guid}` (id malformado → 404). Erros em ProblemDetails, ver [[Tratamento de Erros e ProblemDetails]].

| Método e rota | Policy | Sucesso | Erros | Caso de uso |
|---|---|---|---|---|
| `GET /` `?page&pageSize&categoriaId&nome&ativo&ordenarPor&direcao` | `Leitura` | 200 `PaginaResponse<ProdutoResponse>` | 400, 401, 429 | `ListarProdutosUseCase` |
| `GET /{id}` | `Leitura` | 200 `ProdutoResponse` (inclui inativo) | 401, 404, 429 | `ObterProdutoUseCase` |
| `POST /` | `Escrita` | 201 + `Location: /api/v1/produtos/{id}` + corpo | 400, 401, 403, 409, 422, 429 | `CriarProdutoUseCase` |
| `PUT /{id}` | `Escrita` | 200 `ProdutoResponse` | 400, 401, 403, 404, 409, 422, 429 | `AtualizarProdutoUseCase` |
| `PATCH /{id}/status` corpo `{ "ativo": bool }` | `Escrita` | 204 | 400, 401, 403, 404, 429 | `AlterarStatusProdutoUseCase` |
| `DELETE /{id}` | `Escrita` | 204 | 401, 403, 404, 429 | `RemoverProdutoUseCase` |

Produto removido logicamente responde 404 em GET, PUT, PATCH e DELETE (BR-005).

## Contratos

- `POST` corpo: `{ categoriaId, nome, sku, descricao?, preco }`.
- `PUT` corpo: `{ categoriaId, nome, descricao?, preco }`. `sku` e `ativo` não fazem parte do PUT; se vierem, são ignorados (SKU imutável, BR-003).
- `PATCH /status`: `ativo` obrigatório (ausente/nulo → 400 `errors.ativo`). Idempotente: mesmo valor → 204 sem alterar `dataAtualizacao`.
- Resposta: `{ id, categoriaId, categoriaNome, nome, sku, descricao, preco, ativo, dataCriacao, dataAtualizacao }`. `dataRemocao` nunca é exposta.

## Validação (400, antes de qualquer acesso ao banco)

- `categoriaId` obrigatório e diferente de `Guid.Empty`.
- `nome` 3–150, sem espaços nas pontas ([[ADR-007 Espaços nas pontas rejeitados com 400]]).
- `sku` 3–40, só letras, números e hífen (só no POST), regex `^[A-Za-z0-9-]+\z` em `LimitesCampos.SkuRegex`, usada pelo validator e pelo `Sku.Criar`. O `\z` (e não `$`) impede que um `\n` no final passe ([[SKU aceita quebra de linha no final]], resolvido em 2026-10-16).
- `descricao` ≤ 1000.
- `preco` obrigatório, `> 0`, `≤ 9999999999999999.99`, no máximo 2 casas ([[ADR-014 Validação de preço]]).

## Ordem das checagens

- **POST:** 400 → 422 `produto.categoria_invalida` (inexistente ou inativa) → 409 `produto.sku_duplicado` → 409 `produto.nome_duplicado`.
- **PUT:** 400 → 404 `produto.nao_encontrado` → 422 se a categoria não existir, ou se **mudou** e a nova está inativa (manter a categoria atual inativa é aceito, [[ADR-005 Contrato de Categoria]]) → 409 nome.
- Corrida detectada pelo banco: violação de `UX_Produtos_Sku` → 409 SKU; outra unicidade → 409 nome; FK → 422.

## Paginação

- `page` ≥ 1 (padrão 1), `pageSize` 1–100 (padrão 20). Fora disso → 400 (sem correção silenciosa).
- `ativo` omitido = `true`. `categoriaId` igualdade. `nome` contém, sem diferenciar maiúsculas (acentos diferenciam); vazio/espaços é ignorado.
- `ordenarPor` = `nome` | `preco` (padrão `nome`), `direcao` = `asc` | `desc` (padrão `asc`), sem diferenciar maiúsculas; outro valor → 400. Desempate sempre `id asc`.
- Query string com tipo inválido (`page=abc`, `ativo=talvez`, `categoriaId=x`) → 400.
- Formato: `{ "itens": [], "page": 1, "pageSize": 20, "totalItens": 0, "totalPaginas": 0 }`. `totalPaginas = ceil(totalItens / pageSize)` (0 quando não há itens). Página além da última → 200 com `itens: []` e os totais reais.

## Respostas confirmadas em teste (05-test-engineer, 2026-10-16)

Pelo pipeline HTTP real, com a persistência substituída: 201 com `Location` e `categoriaNome`, sem `dataRemocao` no corpo; 400 `validacao` com chaves camelCase (`categoriaId`, `nome`, `sku`, `preco`, `ativo`, `ordenarPor`, `direcao`, `page`, `pageSize`), inclusive `"TEC-001\n"`/`"AB\n"` em `errors.sku`; 400 `requisicao_invalida` para `page=abc`, `ativo=talvez` e `categoriaId=x`; 422 `produto.categoria_invalida`; 409 `produto.sku_duplicado` (checagem e corrida em `UX_Produtos_Sku`) e `produto.nome_duplicado`; 404 `produto.nao_encontrado`; PUT ignora `sku`/`ativo` do corpo; PATCH 204; DELETE 204. Sem parâmetros, a listagem chega à consulta como `ListarProdutosQuery()` (1, 20, `ativo` nulo → `true` na consulta). Ordenação, filtros CI, desempate e reuso de SKU no banco real ainda não foram executados ([[Testes de integração com SQL Server pendentes]]). cURL de cada endpoint: `docs/api/testes-api.md`. Casos: [[CT BR-001 Nome do produto]], [[CT BR-002 Preço]], [[CT BR-003 SKU]], [[CT BR-004 Categoria válida]], [[CT BR-005 Exclusão lógica]], [[CT REQ-003 e REQ-004 Listagem e status]].

Tabelas: [[Modelo de Dados do Catálogo]]. Relacionado: [[API de Categorias]], [[ADR-008 Unicidade case-insensitive via collation]], [[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]].
