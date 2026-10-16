---
tipo: adr
status: aceito
data: 2026-10-16
tags: [produto, contrato, L-03, L-04, L-08, L-09]
---

# ADR-006: Contrato de Produto

> Fonte: docs/SPEC.md (REQ-002, REQ-004, BR-003, endpoints de produto) e lacunas L-03, L-04, L-08 e L-09 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito. A parte de L-03 é **revisável pelo PO**; ver [[Confirmar decisões que alteram comportamento]].

## Contexto
A spec não define quais campos o PUT pode editar, o formato da resposta, o tratamento de produto inativo ou removido e os erros de autenticação.

## Decisão
- **PUT (L-03):** edita `categoriaId`, `nome`, `descricao` e `preco`. Todos são obrigatórios, exceto `descricao`. `sku` e `ativo` não fazem parte do PUT; se vierem no corpo, são ignorados sem erro.
- **Resposta (L-04):** `{ id, categoriaId, categoriaNome, nome, sku, descricao, preco, ativo, dataCriacao, dataAtualizacao }`. `dataRemocao` nunca é exposta. O POST devolve 201 com `Location` e corpo; o PUT devolve 200 com corpo.
- **Inativo e removido (L-08):** GET de produto inativo devolve 200. Produto removido devolve 404 em GET, PUT, PATCH e DELETE. O PATCH de status é idempotente: repetir o mesmo valor devolve 204 sem alterar `dataAtualizacao`.
- **Erros de corpo e autenticação (L-09):** PATCH com corpo inválido devolve 400. Qualquer endpoint sem token devolve 401. Endpoints de escrita sem a role `Admin` devolvem 403. Todos podem devolver 429.
- **Ordem das checagens no POST:** 400 → 422 → 409 SKU → 409 nome.

## Alternativas Consideradas
- PUT aceitando `ativo`: criaria dois caminhos para a mesma mudança.
- 400 quando o PUT traz `sku`: a spec diz que o campo é ignorado.
- 404 para produto inativo: a spec só esconde inativos da listagem.

## Consequências
- Mudar o status do produto exige o endpoint `PATCH /produtos/{id}/status`.

## Relacionado
- [[ADR-007 Espaços nas pontas rejeitados com 400]]
- [[ADR-014 Validação de preço]]
- [[Tratamento de Erros e ProblemDetails]]
