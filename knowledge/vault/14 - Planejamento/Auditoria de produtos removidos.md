---
tipo: planejamento
status: adiado
data: 2026-10-16
tags: [auditoria, escopo-adiado]
---

# Auditoria de produtos removidos

> Fonte: consequência de [[ADR-001 Remoção de categoria com produtos removidos]]. A spec não define requisito de auditoria.

## Escopo adiado
Quando uma categoria é excluída, os produtos dela que já estavam removidos logicamente são apagados fisicamente, e o histórico deles se perde. A spec não pede auditoria nem histórico. Se isso passar a ser necessário, a ADR-001 terá de ser revista: por exemplo, com uma tabela de histórico ou trocando a remoção física da categoria por exclusão lógica.
