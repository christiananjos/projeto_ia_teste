---
tipo: planejamento
status: adiado
data: 2026-10-16
tags: [listagem, escopo-adiado]
---

# Listagem de produtos ativos e inativos

> Fonte: docs/SPEC.md (REQ-004: `ativo=true` implícito) e lacuna L-07 do 01-analyst-sdd.

## Escopo adiado
`GET /produtos` sem o parâmetro `ativo` aplica `ativo=true`. Ainda não há como listar ativos e inativos na mesma chamada: hoje é preciso fazer duas, uma com `ativo=true` e outra com `ativo=false` ([[ADR-009 Paginação, filtros e ordenação]]). Se isso for necessário, uma opção é aceitar um valor como `ativo=todos` (a definir).
