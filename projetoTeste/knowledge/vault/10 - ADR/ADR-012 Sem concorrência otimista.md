---
tipo: adr
status: aceito
data: 2026-10-16
tags: [concorrencia, L-11]
---

# ADR-012: Sem concorrência otimista nesta versão

> Fonte: lacuna L-11 do 01-analyst-sdd (a spec não fala de concorrência). Decisão do 02-architect-sdd.

## Status
Aceito. O escopo foi adiado; ver [[Concorrência otimista]].

## Contexto
A spec não fala em controle de concorrência.

## Decisão
- Sem `rowversion`, ETag ou `If-Match`.
- O PUT segue a regra last-write-wins.
- A unicidade continua garantida pelos índices.

## Alternativas Consideradas
- `rowversion` com ETag e resposta 412: mudaria o contrato de todos os clientes.

## Consequências
- Duas edições simultâneas podem se sobrescrever.

## Relacionado
- [[ADR-006 Contrato de Produto]]
