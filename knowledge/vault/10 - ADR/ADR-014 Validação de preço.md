---
tipo: adr
status: aceito
data: 2026-10-16
tags: [validacao, preco, BR-002, L-13]
---

# ADR-014: Validação de preço

> Fonte: docs/SPEC.md (BR-002, `Preco decimal(18,2)`) e lacuna L-13 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
A spec não define um valor máximo para o preço.

## Decisão
- O preço é obrigatório e deve ser `> 0`.
- O máximo é `9999999999999999.99`, o limite do tipo `decimal(18,2)`.
- São aceitas no máximo 2 casas decimais, verificadas com `decimal.Round(p, 2) == p`.
- A regra fica no validator (que devolve 400) e também como guarda na entidade.

## Alternativas Consideradas
- Definir um teto de negócio menor: seria suposição, porque a spec não informa nenhum.

## Consequências
- Se o negócio definir um teto menor depois, basta mudar a constante `LimitesCampos.PrecoMaximo`.

## Relacionado
- [[ADR-006 Contrato de Produto]]
