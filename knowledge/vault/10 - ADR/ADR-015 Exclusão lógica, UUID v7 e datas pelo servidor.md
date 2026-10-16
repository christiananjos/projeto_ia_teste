---
tipo: adr
status: aceito
data: 2026-10-16
tags: [dominio, exclusao-logica, BR-005, BR-007]
---

# ADR-015: Exclusão lógica com global query filter, UUID v7 e datas pelo servidor

> Fonte: docs/SPEC.md (BR-005, BR-007, `Id (UUID, PK)`). Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
A spec define exclusão lógica para produtos (BR-005), ids UUID e datas preenchidas pelo servidor em UTC (BR-007).

## Decisão
- `HasQueryFilter(p => p.DataRemocao == null)` em Produto.
- `IgnoreQueryFilters()` só em [[ADR-001 Remoção de categoria com produtos removidos]].
- Os ids são gerados no domínio com `Guid.CreateVersion7()`.
- As datas vêm de um `TimeProvider` injetado e chegam às entidades como `agoraUtc`.
- As datas são gravadas em `datetime2` com conversor UTC e serializadas com o sufixo `Z`.

## Alternativas Consideradas
- Filtrar `DataRemocao` à mão em cada consulta: fácil de esquecer.
- `NEWSEQUENTIALID()`: o id só seria conhecido depois do insert.
- `DateTime.UtcNow` direto no código: impede controlar a data nos testes.

## Consequências
- Produtos removidos não aparecem em nenhuma consulta, a menos que o filtro seja ignorado de propósito.

## Relacionado
- [[Persistência EF Core]]
