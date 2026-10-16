# Relatório de Uso de Tokens

_Gerado e atualizado automaticamente pelo hook `Stop` após cada execução completa do pipeline `/inicia-orquestracao`. Números vêm diretamente dos transcripts da sessão — não são estimados pelo modelo. Horário local da máquina._

## Última rodada — 2026-10-16 16:28

| Métrica | Tokens |
|---|---|
| Entrada (input) | 182 |
| Saída (output) | 135.662 |
| Cache — criação | 376.618 |
| Cache — leitura | 14.468.179 |
| **Total** | **14.980.641** |

| Custo estimado | Valor |
|---|---|
| Dólar (USD) | US$ 7.46 |
| Real (BRL) | R$ 39,52 |

_Estimativa a preço de tarifa da API da Anthropic (US$/milhão de tokens, calculada pelo modelo que respondeu cada mensagem — inclui cache, fast mode e web search; cotação fixa US$ 1 = R$ 5,30) — não é uma fatura real, e não reflete plano de assinatura (Pro/Max), descontos nem a tabela própria do Amazon Bedrock/Google Cloud._

> ⚠️ Modelo(s) sem preço próprio na tabela: claude-sonnet-5-5 (calculado com o preço de claude-sonnet-5). O custo desses entra no total, mas é aproximado — confira https://platform.claude.com/docs/en/about-claude/pricing e atualize `PRICING` em `.claude/hooks/generate-token-report.cjs`.

### Por agente

| Agente | Modelo(s) | Tokens | Custo (USD) |
|---|---|---|---|
| inicia-orquestracao (agente principal) | claude-sonnet-5-5 | 538.623 | US$ 0.16 |
| 05-test-engineer | claude-opus-5-5 | 14.031.063 | US$ 6.88 |
| 04-reviewer-sdd | claude-opus-5-5 | 396.331 | US$ 0.36 |
| 06-security-scan-sdd | claude-opus-5-5 | 14.624 | US$ 0.05 |

### Por modelo

| Modelo | Tokens | Custo (USD) |
|---|---|---|
| claude-opus-5-5 | 14.442.018 | US$ 7.29 |
| claude-sonnet-5-5 | 538.623 | US$ 0.16 |

## Histórico de rodadas

| Data | Total de tokens | Custo (USD) |
|---|---|---|
| 2026-10-16 16:28 | 14.980.641 | US$ 7.46 |
| 2026-10-16 16:00 | 1.335.442 | US$ 0.95 |
| 2026-10-16 15:47 | 2.702.886 | US$ 1.75 |
| 2026-10-16 15:41 | 8.383.470 | US$ 4.95 |
| 2026-10-16 15:38 | 7.879.747 | US$ 5.63 |
| 2026-10-16 15:17 | 1.904.106 | US$ 2.25 |
| 2026-10-16 15:06 | 369.451 | US$ 0.40 |

## Total acumulado (todas as rodadas)

_Soma de todas as 7 rodada(s) que este relatório já registrou, desde 2026-10-16 15:06. Recalculado automaticamente a cada nova rodada — nunca perde rodadas antigas, mesmo as que já saíram da tabela de histórico acima._

| Métrica | Tokens |
|---|---|
| Entrada (input) | 714 |
| Saída (output) | 451.521 |
| Cache — criação | 1.504.768 |
| Cache — leitura | 35.598.740 |
| **Total** | **37.555.743** |

| Custo estimado acumulado | Valor |
|---|---|
| Dólar (USD) | US$ 23.39 |
| Real (BRL) | R$ 123,96 |
