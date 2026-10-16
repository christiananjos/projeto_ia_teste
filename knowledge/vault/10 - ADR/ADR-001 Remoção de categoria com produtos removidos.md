---
tipo: adr
status: aceito
data: 2026-10-16
tags: [categoria, exclusao-logica, fk, L-01]
---

# ADR-001: Remoção de categoria com produtos só removidos logicamente

> Fonte: docs/SPEC.md (BR-005, BR-006, modelo de dados) e lacuna L-01 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito, mas **revisável pelo PO**, porque altera comportamento visível. Ver [[Confirmar decisões que alteram comportamento]].

## Contexto
Pela BR-006, uma categoria que tenha ao menos um produto **não removido** devolve 409. Sem produtos, ela é removida fisicamente. Pela BR-005, os produtos removidos continuam no banco com `DataRemocao` preenchida. Como `Produto.CategoriaId` é FK obrigatória, uma categoria que só tenha produtos removidos logicamente não conseguiria ser apagada fisicamente.

## Decisão
Opção (a). Se não houver nenhum produto não removido (ativo ou inativo), a operação abre **uma única transação**, apaga fisicamente os produtos da categoria com `DataRemocao IS NOT NULL` e em seguida apaga a categoria. A FK fica `Restrict` (sem cascade). Se houver corrida com a inserção de um produto novo, a FK devolve o erro 547, e a API responde 409 `categoria.possui_produtos`.

## Alternativas Consideradas
- (b) 409 enquanto existir qualquer produto, inclusive removido: contradiz o texto da BR-006 e deixaria a categoria impossível de excluir para sempre.
- (c) Exclusão lógica também para a categoria: contradiz "removida fisicamente" e exige mudar o modelo.
- (d) FK anulável com `SET NULL`: contradiz `CategoriaId required`.
- (e) `ON DELETE CASCADE`: apagaria produtos não removidos se a checagem falhasse.

## Consequências
- O histórico dos produtos removidos dessa categoria é perdido. Ver [[Auditoria de produtos removidos]].
- Os testes de integração precisam cobrir três casos: categoria com produtos só removidos (204, produtos apagados), categoria com produto ativo ou inativo (409) e categoria vazia (204).

## Relacionado
- [[Persistência EF Core]]
- [[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]]
- [[Arquitetura da Solução]]
