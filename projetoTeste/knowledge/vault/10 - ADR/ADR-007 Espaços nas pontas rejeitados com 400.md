---
tipo: adr
status: aceito
data: 2026-10-16
tags: [validacao, BR-001, L-05]
---

# ADR-007: Espaços nas pontas do nome rejeitados com 400

> Fonte: docs/SPEC.md (BR-001, "sem espaços nas pontas") e lacuna L-05 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito, **revisável pelo PO**. Ver [[Confirmar decisões que alteram comportamento]].

## Contexto
A spec não diz se a API deve rejeitar o nome com espaços nas pontas ou aplicar trim automaticamente.

## Decisão
- A API **rejeita** com 400 e devolve `errors.nome = "O nome não pode começar nem terminar com espaços."`.
- A regra vale para o nome de produto e para o nome de categoria.
- Nenhum campo recebe trim automático.
- A descrição não tem essa regra.

## Alternativas Consideradas
- Trim silencioso: altera o dado sem avisar o cliente e pode gerar um 409 difícil de entender.

## Consequências
- Os clientes precisam enviar o nome já sem espaços nas pontas.

## Relacionado
- [[ADR-006 Contrato de Produto]]
- [[ADR-005 Contrato de Categoria]]
