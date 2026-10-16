---
tipo: adr
status: aceito
data: 2026-10-16
tags: [categoria, contrato, L-02]
---

# ADR-005: Contrato de Categoria

> Fonte: docs/SPEC.md (REQ-001, endpoints de categoria) e lacuna L-02 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
A spec não define o corpo do POST/PUT, a resposta, como inativar uma categoria nem como filtrar a listagem.

## Decisão
- **POST** `{ nome, descricao? }`. A categoria nasce com `ativa = true`.
- **PUT** `{ nome, descricao?, ativa }`. `ativa` é obrigatório e é a única forma de inativar ou reativar. Como o PUT substitui o recurso inteiro, `descricao` nula limpa o campo.
- **Resposta** `{ id, nome, descricao, ativa, dataCriacao, dataAtualizacao }`. O POST devolve 201 com `Location`; o PUT devolve 200.
- **GET lista** aceita `page` (padrão 1), `pageSize` (padrão 20, máx. 100), `nome` (contém, sem diferenciar maiúsculas) e `ativa` (omitido = todas). Ordena por `nome asc` e desempata por `id`.
- O nome segue a mesma regra de espaços do produto ([[ADR-007 Espaços nas pontas rejeitados com 400]]).
- Inativar uma categoria não afeta os produtos que já estão nela. Só impede criar produto nela ou mover produto para ela (BR-004). O PUT de um produto que **mantém** uma categoria já inativa é aceito.

## Alternativas Consideradas
- `PATCH /categorias/{id}/status`: endpoint fora da spec.
- `ativa` opcional no PUT: não combina com PUT como substituição.
- Filtro `ativa=true` como padrão na listagem: a spec só define esse padrão para produtos.

## Consequências
- Não há endpoint dedicado para mudar o status da categoria.

## Relacionado
- [[ADR-001 Remoção de categoria com produtos removidos]]
- [[ADR-009 Paginação, filtros e ordenação]]
