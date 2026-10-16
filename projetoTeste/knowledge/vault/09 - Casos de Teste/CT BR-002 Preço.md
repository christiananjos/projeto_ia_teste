---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, produto, preco, br-002]
---

# CT BR-002 Preço

> Fonte: docs/SPEC.md (BR-002), [[ADR-014 Validação de preço]]. Testes do 05-test-engineer, 2026-10-16.

**Regra:** obrigatório, `> 0`, `≤ 9999999999999999.99` (limite de `decimal(18,2)`), no máximo 2 casas decimais.

| Cenário | Esperado | Teste | Executado |
|---|---|---|---|
| `0`, `-0.01`, `-10` | 400 "O preço deve ser maior que zero." / `DomainException` | `PrecoInvalido_TemErroNoPreco`, `ProdutoTests.Criar_ComPrecoInvalido_*` | Sim |
| `0.001`, `349.999`, `10.999` | 400 "no máximo 2 casas decimais" | os mesmos | Sim |
| `10000000000000000.00` | 400 "menor ou igual a ..." | `PrecoInvalido_TemErroNoPreco`, `Criar_ComPrecoAcimaDoLimiteDoDecimal18_2_*` | Sim |
| `0.01`, `349.90`, máximo exato | aceito | `PrecoValido_NaoTemErro`, `Criar_ComPrecoValido_Aceita` | Sim |
| Ausente (POST e PUT) | 400 "O preço é obrigatório." | `SemPreco_TemErro`, `Put_SemPreco_Retorna400` | Sim |
| `10.500` (escala 3, valor com 2 casas) | aceito | `LimitesCamposTests.TemNoMaximoCasasDecimais_*` | Sim |
| Coluna `decimal(18,2)` | precisão 18, escala 2 | `ModeloEfTests.Preco_EhDecimal18Com2Casas` | Sim |

**Observação (não é falha):** a mensagem do limite máximo interpola `LimitesCampos.PrecoMaximo` com a cultura do servidor (em pt-BR sai `9999999999999999,99`). O teste compara só o início da mensagem.

Relacionado: [[API de Produtos]], [[Estratégia e execução dos testes]].
