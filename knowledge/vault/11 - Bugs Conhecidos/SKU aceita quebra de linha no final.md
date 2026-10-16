---
tipo: bug
status: resolvido
severidade: critica
data: 2026-10-16
resolvido-em: 2026-10-16
tags: [bug, produto, sku, validacao, br-003, resolvido]
---

# SKU aceita quebra de linha no final

> Fonte: revisão do 04-reviewer-sdd em 2026-10-16 (código em `src/Domain/Regras/LimitesCampos.cs`), confirmada executando a regex no .NET 10. Regra afetada: BR-003 de `docs/SPEC.md`. Corrigido pelo 03-dotnet-specialist em 2026-10-16.

## Status: resolvido
`LimitesCampos.SkuRegex` passou a ser `^[A-Za-z0-9-]+\z`. A constante é a única fonte da regex, então a correção vale para o `CriarProdutoRequestValidator` e para o `Sku.Criar` ao mesmo tempo. A [[API de Produtos]] e a especificação técnica (§3.2/§4.4) foram atualizadas.

**Regressão coberta pelo 05-test-engineer em 2026-10-16** (todos executados e aprovados): `"TEC-001\n"`, `"AB\n"` e `"TEC-001\r\n"` dão erro em `sku` no validator, `DomainException` no `Sku.Criar`, 400 no caso de uso sem acessar o banco e 400 `errors.sku` via HTTP. Detalhes em [[CT BR-003 SKU]].

## Sintoma (antes da correção)
`POST /api/v1/produtos` com `"sku": "TEC-001\n"` passava na validação e era gravado. Como o valor difere de `TEC-001`, o índice `UX_Produtos_Sku` não acusava duplicidade. Também passava `"AB\n"`, que tem 3 caracteres, mas só 2 deles válidos.

## Causa
A regex antiga era `^[A-Za-z0-9-]+$`. No .NET, sem `RegexOptions.Multiline`, o `$` casa no fim da string **ou antes de um `\n` final**. Já o `\z` só casa no fim absoluto. A constante é usada em dois lugares:
- `CriarProdutoRequestValidator` (`.Matches(LimitesCampos.SkuRegex)`)
- `Sku.Criar` (`[GeneratedRegex(LimitesCampos.SkuRegex)]`)

## Observação
O conversor EF do `Sku` chama `Sku.Criar` ao ler do banco. Se algum SKU com `\n` tivesse sido gravado antes da correção, a leitura desse produto lançaria `DomainException`. Isso não acontece hoje porque o banco ainda não foi usado em produção.

## Relacionado
- [[API de Produtos]]
- [[Modelo de Dados do Catálogo]]
- [[ADR-008 Unicidade case-insensitive via collation]]
- [[Implementação do Catálogo de Produtos]]
