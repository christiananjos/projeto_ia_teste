---
tipo: planejamento
status: adiado
data: 2026-10-16
tags: [concorrencia, escopo-adiado]
---

# Concorrência otimista

> Fonte: lacuna L-11 do 01-analyst-sdd. A spec não menciona controle de concorrência.

## Escopo adiado
Não há `rowversion` nem ETag/`If-Match` no PUT: a última gravação vence ([[ADR-012 Sem concorrência otimista]]). Se o negócio exigir controle de concorrência, isso muda o contrato da API (header `If-Match`, resposta 412) e precisa de uma migration.

## Relacionado
- [[ADR-006 Contrato de Produto]]
