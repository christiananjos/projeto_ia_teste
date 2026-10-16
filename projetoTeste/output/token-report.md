# Relatório de Uso de Tokens

_Gerado e atualizado automaticamente pelo hook `Stop` após cada execução completa do pipeline `/inicia-orquestracao`. Números vêm diretamente dos transcripts da sessão — não são estimados pelo modelo. Horário local da máquina._

## Última rodada — 2026-10-16 15:47

| Métrica | Tokens |
|---|---|
| Entrada (input) | 82 |
| Saída (output) | 17.564 |
| Cache — criação | 185.804 |
| Cache — leitura | 2.499.436 |
| **Total** | **2.702.886** |

| Custo estimado | Valor |
|---|---|
| Dólar (USD) | US$ 1.75 |
| Real (BRL) | R$ 9,29 |

_Estimativa a preço de tarifa da API da Anthropic (US$/milhão de tokens, calculada pelo modelo que respondeu cada mensagem — inclui cache, fast mode e web search; cotação fixa US$ 1 = R$ 5,30) — não é uma fatura real, e não reflete plano de assinatura (Pro/Max), descontos nem a tabela própria do Amazon Bedrock/Google Cloud._

> ⚠️ Modelo(s) sem preço próprio na tabela: claude-sonnet-5-5 (calculado com o preço de claude-sonnet-5). O custo desses entra no total, mas é aproximado — confira https://platform.claude.com/docs/en/about-claude/pricing e atualize `PRICING` em `.claude/hooks/generate-token-report.cjs`.

### Por agente

| Agente | Modelo(s) | Tokens | Custo (USD) |
|---|---|---|---|
| inicia-orquestracao (agente principal) | claude-sonnet-5-5 | 494.947 | US$ 0.15 |
| 04-reviewer-sdd | claude-opus-5-5 | 1.680.585 | US$ 1.14 |
| 03-dotnet-specialist | claude-opus-5-5 | 527.354 | US$ 0.47 |

### Por modelo

| Modelo | Tokens | Custo (USD) |
|---|---|---|
| claude-opus-5-5 | 2.207.939 | US$ 1.61 |
| claude-sonnet-5-5 | 494.947 | US$ 0.15 |

## Histórico de rodadas

| Data | Total de tokens | Custo (USD) |
|---|---|---|
| 2026-10-16 15:47 | 2.702.886 | US$ 1.75 |
| 2026-10-16 15:41 | 8.383.470 | US$ 4.95 |
| 2026-10-16 15:38 | 7.879.747 | US$ 5.63 |
| 2026-10-16 15:17 | 1.904.106 | US$ 2.25 |
| 2026-10-16 15:06 | 369.451 | US$ 0.40 |

## Total acumulado (todas as rodadas)

_Soma de todas as 5 rodada(s) que este relatório já registrou, desde 2026-10-16 15:06. Recalculado automaticamente a cada nova rodada — nunca perde rodadas antigas, mesmo as que já saíram da tabela de histórico acima._

| Métrica | Tokens |
|---|---|
| Entrada (input) | 468 |
| Saída (output) | 304.508 |
| Cache — criação | 1.028.713 |
| Cache — leitura | 19.905.971 |
| **Total** | **21.239.660** |

| Custo estimado acumulado | Valor |
|---|---|
| Dólar (USD) | US$ 14.98 |
| Real (BRL) | R$ 79,41 |
