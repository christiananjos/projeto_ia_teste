---
tipo: adr
status: aceito
data: 2026-10-16
tags: [listagem, paginacao, REQ-003, L-07]
---

# ADR-009: Paginação, filtros e ordenação

> Fonte: docs/SPEC.md (REQ-003, REQ-004) e lacuna L-07 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
A spec não define o que fazer com parâmetros fora do limite, o critério de desempate da ordenação nem como listar ativos e inativos juntos.

## Decisão
- `page < 1`, `pageSize < 1` e `pageSize > 100` devolvem **400**. O valor não é corrigido em silêncio.
- A ordenação usa `ordenarPor` = `nome` | `preco` (padrão `nome`) e `direcao` = `asc` | `desc` (padrão `asc`), sem diferenciar maiúsculas. Qualquer outro valor devolve 400.
- O desempate é sempre `id asc` (UUID v7). Observação da implementação (2026-10-16): o SQL Server ordena `uniqueidentifier` começando pelos últimos 6 bytes, então essa ordem é estável e determinística, mas **não** acompanha a ordem de criação. Ver [[Modelo de Dados do Catálogo]].
- `ativo` omitido equivale a `true`. Listar ativos e inativos juntos fica **fora do escopo**; ver [[Listagem de produtos ativos e inativos]].
- `page` além da última página devolve 200 com `itens: []` e os totais reais.
- `nome` vazio é ignorado.
- `totalPaginas = ceil(totalItens / pageSize)`, ou 0 quando não há itens.

## Alternativas Consideradas
- Truncar `pageSize` em 100: esconde o erro do cliente.
- Parâmetro único `sort=campo:dir`: exige mais parsing sem ganho real.

## Consequências
- Quem precisar de ativos e inativos faz duas chamadas.

## Relacionado
- [[ADR-005 Contrato de Categoria]]
- [[ADR-006 Contrato de Produto]]
