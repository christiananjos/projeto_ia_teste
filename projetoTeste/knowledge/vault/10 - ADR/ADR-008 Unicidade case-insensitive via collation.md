---
tipo: adr
status: aceito
data: 2026-10-16
tags: [banco, unicidade, collation, L-06]
---

# ADR-008: Unicidade sem diferenciar maiúsculas via collation e índices filtrados

> Fonte: docs/SPEC.md (REQ-001, BR-001, BR-003, Índices) e lacuna L-06 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito

## Contexto
A spec pede unicidade sem diferenciar maiúsculas, mas não diz como isso vale para o SKU nem para o filtro `nome` da listagem.

## Decisão
- `Categorias.Nome`, `Produtos.Nome` e `Produtos.Sku` usam a collation explícita `Latin1_General_100_CI_AS`.
- O **SKU também não diferencia maiúsculas**: `tec-001` e `TEC-001` conflitam. O valor é guardado como foi enviado.
- O filtro `nome` da listagem também não diferencia maiúsculas. Acentos diferenciam.
- Índices `UX_Categorias_Nome`, `UX_Produtos_Sku` (filtrado) e `UX_Produtos_CategoriaId_Nome` (filtrado).
- A unicidade tem duas camadas: uma checagem prévia no caso de uso e o índice. Se o índice disparar (erro 2601/2627), a falha é traduzida para `ConflitoPersistenciaException`, e o nome do índice indica se o conflito foi de SKU ou de nome.

## Alternativas Consideradas
- Coluna sombra normalizada: duplica dado.
- `ToLower()` nas consultas: impede o uso do índice.
- Usar a collation padrão do servidor: o comportamento muda de ambiente para ambiente.

## Consequências
- Os índices filtrados e a collation só podem ser testados em SQL Server real ([[ADR-013 Estratégia de testes com Testcontainers]]).

## Relacionado
- [[Persistência EF Core]]
- [[SQL Server]]
