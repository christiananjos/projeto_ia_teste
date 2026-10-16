# Relatório de Uso de Tokens

_Gerado e atualizado automaticamente pelo hook `Stop` após cada execução completa do pipeline `/inicia-orquestracao`. Números vêm diretamente dos transcripts da sessão — não são estimados pelo modelo. Horário local da máquina._

## Última rodada — 2026-10-16 16:00

| Métrica | Tokens |
|---|---|
| Entrada (input) | 64 |
| Saída (output) | 11.351 |
| Cache — criação | 99.437 |
| Cache — leitura | 1.224.590 |
| **Total** | **1.335.442** |

| Custo estimado | Valor |
|---|---|
| Dólar (USD) | US$ 0.95 |
| Real (BRL) | R$ 5,02 |

_Estimativa a preço de tarifa da API da Anthropic (US$/milhão de tokens, calculada pelo modelo que respondeu cada mensagem — inclui cache, fast mode e web search; cotação fixa US$ 1 = R$ 5,30) — não é uma fatura real, e não reflete plano de assinatura (Pro/Max), descontos nem a tabela própria do Amazon Bedrock/Google Cloud._

> ⚠️ Modelo(s) sem preço próprio na tabela: claude-sonnet-5-5 (calculado com o preço de claude-sonnet-5). O custo desses entra no total, mas é aproximado — confira https://platform.claude.com/docs/en/about-claude/pricing e atualize `PRICING` em `.claude/hooks/generate-token-report.cjs`.

### Por agente

| Agente | Modelo(s) | Tokens | Custo (USD) |
|---|---|---|---|
| inicia-orquestracao (agente principal) | claude-sonnet-5-5 | 411.757 | US$ 0.12 |
| 03-dotnet-specialist | claude-opus-5-5 | 527.354 | US$ 0.47 |
| 04-reviewer-sdd | claude-opus-5-5 | 396.331 | US$ 0.36 |
| 05-test-engineer | — | 0 | US$ 0.00 |

### Por modelo

| Modelo | Tokens | Custo (USD) |
|---|---|---|
| claude-opus-5-5 | 923.685 | US$ 0.83 |
| claude-sonnet-5-5 | 411.757 | US$ 0.12 |

## Histórico de rodadas

| Data | Total de tokens | Custo (USD) |
|---|---|---|
| 2026-10-16 16:00 | 1.335.442 | US$ 0.95 |
| 2026-10-16 15:47 | 2.702.886 | US$ 1.75 |
| 2026-10-16 15:41 | 8.383.470 | US$ 4.95 |
| 2026-10-16 15:38 | 7.879.747 | US$ 5.63 |
| 2026-10-16 15:17 | 1.904.106 | US$ 2.25 |
| 2026-10-16 15:06 | 369.451 | US$ 0.40 |

## Total acumulado (todas as rodadas)

_Soma de todas as 6 rodada(s) que este relatório já registrou, desde 2026-10-16 15:06. Recalculado automaticamente a cada nova rodada — nunca perde rodadas antigas, mesmo as que já saíram da tabela de histórico acima._

| Métrica | Tokens |
|---|---|
| Entrada (input) | 532 |
| Saída (output) | 315.859 |
| Cache — criação | 1.128.150 |
| Cache — leitura | 21.130.561 |
| **Total** | **22.575.102** |

| Custo estimado acumulado | Valor |
|---|---|
| Dólar (USD) | US$ 15.93 |
| Real (BRL) | R$ 84,44 |
