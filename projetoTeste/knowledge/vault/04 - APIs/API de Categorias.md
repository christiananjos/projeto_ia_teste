---
tipo: api
status: implementado
data: 2026-10-16
tags: [api, categoria, rest]
---

# API de Categorias

> Fonte: docs/SPEC.md (REQ-001, Endpoints › Categorias), [[ADR-005 Contrato de Categoria]]. Implementado pelo 03-dotnet-specialist em 2026-10-16 em `src/API/Endpoints/CategoriaEndpoints.cs`.

Base: `/api/v1/categorias`. Rotas com `{id}` usam `{id:guid}`: id que não é GUID não casa com a rota e dá 404.
Erros sempre em ProblemDetails, ver [[Tratamento de Erros e ProblemDetails]]. Autorização em [[Provedor de Identidade JWT]].

| Método e rota | Policy | Sucesso | Erros | Caso de uso |
|---|---|---|---|---|
| `GET /` `?page&pageSize&nome&ativa` | `Leitura` | 200 `PaginaResponse<CategoriaResponse>` | 400, 401, 429 | `ListarCategoriasUseCase` |
| `GET /{id}` | `Leitura` | 200 `CategoriaResponse` | 401, 404, 429 | `ObterCategoriaUseCase` |
| `POST /` | `Escrita` | 201 + `Location: /api/v1/categorias/{id}` + corpo | 400, 401, 403, 409, 429 | `CriarCategoriaUseCase` |
| `PUT /{id}` | `Escrita` | 200 `CategoriaResponse` | 400, 401, 403, 404, 409, 429 | `AtualizarCategoriaUseCase` |
| `DELETE /{id}` | `Escrita` | 204 | 401, 403, 404, 409, 429 | `RemoverCategoriaUseCase` |

## Contratos

- `POST` corpo: `{ "nome": string, "descricao": string? }`. Nasce com `ativa = true`.
- `PUT` corpo: `{ "nome": string, "descricao": string?, "ativa": bool }`. `ativa` é obrigatório (ausente → 400 `errors.ativa`). `descricao` omitida limpa o campo (substituição completa).
- Resposta: `{ id, nome, descricao, ativa, dataCriacao, dataAtualizacao }`, datas ISO 8601 com `Z`.
- Listagem: `page` (padrão 1, ≥ 1), `pageSize` (padrão 20, 1–100), `nome` (contém, sem diferenciar maiúsculas; vazio/espaços é ignorado), `ativa` (omitido = todas). Ordem fixa `nome asc`, desempate `id asc`. Formato da página em [[API de Produtos#Paginação]].

## Regras aplicadas (ordem de checagem)

- **Criar:** validação (nome 3–100 sem espaços nas pontas, descrição ≤ 500) → 409 `categoria.nome_duplicado` (checagem prévia e índice único em corrida).
- **Atualizar:** validação → 404 `categoria.nao_encontrada` → 409 `categoria.nome_duplicado`.
- **Remover:** 404 → 409 `categoria.possui_produtos` se houver produto não removido (ativo ou inativo) → exclusão física da categoria junto com os produtos já removidos logicamente, numa transação ([[ADR-001 Remoção de categoria com produtos removidos]]). Corrida com produto novo (FK 547) → 409 `categoria.possui_produtos`.
- Inativar a categoria não afeta os produtos já vinculados; só impede criar/mover produto para ela (BR-004).

## Respostas confirmadas em teste (05-test-engineer, 2026-10-16)

Pelo pipeline HTTP real, com a persistência substituída: 201 com `Location` relativo `/api/v1/categorias/{id}` e datas com sufixo `Z`; datas enviadas pelo cliente são ignoradas; 400 `validacao` com `errors.nome`/`errors.ativa`/`errors.page`/`errors.pageSize`; 400 `requisicao_invalida` para JSON malformado e `ativa=talvez`; 404 `categoria.nao_encontrada`; 404 `nao_encontrado` para id não-GUID; 409 `categoria.nome_duplicado` (`detail` "Já existe uma categoria com este nome."); 409 `categoria.possui_produtos` (inclusive na corrida de FK); 204 no DELETE. As respostas que dependem do SQL Server (unicidade CI, ADR-001) ainda não foram executadas ([[Testes de integração com SQL Server pendentes]]). cURL de cada endpoint: `docs/api/testes-api.md`. Casos: [[CT REQ-001 Categorias]], [[CT BR-006 Remoção de categoria]].

Tabelas: [[Modelo de Dados do Catálogo]]. Relacionado: [[API de Produtos]], [[ADR-007 Espaços nas pontas rejeitados com 400]], [[ADR-008 Unicidade case-insensitive via collation]].
