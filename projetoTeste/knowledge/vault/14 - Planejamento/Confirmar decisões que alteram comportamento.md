---
tipo: planejamento
status: pendente
data: 2026-10-16
tags: [pendencia, po, decisao]
---

# Confirmar decisões que alteram comportamento

> Fonte: recomendação do 01-analyst-sdd (spec aprovada com ressalvas): L-01, L-03 e L-05 deveriam voltar ao usuário. O 02-architect-sdd decidiu as três para não travar o pipeline, e elas continuam aguardando confirmação do PO.

## Pendência
O PO ainda não confirmou as três decisões abaixo. Se alguma for revertida, o ADR correspondente passa a "Substituído", e o código e os testes precisam mudar junto.

| Lacuna | Decisão tomada | ADR |
|---|---|---|
| L-01 | Excluir a categoria apaga fisicamente os produtos que já estavam removidos logicamente | [[ADR-001 Remoção de categoria com produtos removidos]] |
| L-03 | O PUT de produto edita só `categoriaId`, `nome`, `descricao` e `preco`. `ativo` muda só pelo PATCH, e `sku` é ignorado | [[ADR-006 Contrato de Produto]] |
| L-05 | Nome com espaços nas pontas é rejeitado com 400, sem trim automático | [[ADR-007 Espaços nas pontas rejeitados com 400]] |
