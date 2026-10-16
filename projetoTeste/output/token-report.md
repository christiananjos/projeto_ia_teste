# Relatório de Uso de Tokens

_Gerado e atualizado automaticamente pelo hook `Stop` após cada execução completa do pipeline `/inicia-orquestracao`. Números vêm diretamente dos transcripts da sessão — não são estimados pelo modelo. Horário local da máquina._

## Última rodada — 2026-10-16 15:17

| Métrica | Tokens |
|---|---|
| Entrada (input) | 68 |
| Saída (output) | 63.259 |
| Cache — criação | 143.409 |
| Cache — leitura | 1.697.370 |
| **Total** | **1.904.106** |

| Custo estimado | Valor |
|---|---|
| Dólar (USD) | US$ 2.25 |
| Real (BRL) | R$ 11,92 |

_Estimativa a preço de tarifa da API da Anthropic (US$/milhão de tokens, calculada pelo modelo que respondeu cada mensagem — inclui cache, fast mode e web search; cotação fixa US$ 1 = R$ 5,30) — não é uma fatura real, e não reflete plano de assinatura (Pro/Max), descontos nem a tabela própria do Amazon Bedrock/Google Cloud._

> ⚠️ Modelo(s) sem preço próprio na tabela: claude-sonnet-5-5 (calculado com o preço de claude-sonnet-5). O custo desses entra no total, mas é aproximado — confira https://platform.claude.com/docs/en/about-claude/pricing e atualize `PRICING` em `.claude/hooks/generate-token-report.cjs`.

### Por agente

| Agente | Modelo(s) | Tokens | Custo (USD) |
|---|---|---|---|
| inicia-orquestracao (agente principal) | claude-sonnet-5-5 | 834.310 | US$ 0.31 |
| 02-architect-sdd | claude-opus-5-5 | 1.006.665 | US$ 1.74 |
| 01-analyst-sdd | claude-opus-5-5 | 63.131 | US$ 0.20 |
| 03-dotnet-specialist | — | 0 | US$ 0.00 |

### Por modelo

| Modelo | Tokens | Custo (USD) |
|---|---|---|
| claude-opus-5-5 | 1.069.796 | US$ 1.94 |
| claude-sonnet-5-5 | 834.310 | US$ 0.31 |

## Histórico de rodadas

| Data | Total de tokens | Custo (USD) |
|---|---|---|
| 2026-10-16 15:17 | 1.904.106 | US$ 2.25 |
| 2026-10-16 15:06 | 369.451 | US$ 0.40 |

## Total acumulado (todas as rodadas)

_Soma de todas as 2 rodada(s) que este relatório já registrou, desde 2026-10-16 15:06. Recalculado automaticamente a cada nova rodada — nunca perde rodadas antigas, mesmo as que já saíram da tabela de histórico acima._

| Métrica | Tokens |
|---|---|
| Entrada (input) | 86 |
| Saída (output) | 68.752 |
| Cache — criação | 200.195 |
| Cache — leitura | 2.004.524 |
| **Total** | **2.273.557** |

| Custo estimado acumulado | Valor |
|---|---|
| Dólar (USD) | US$ 2.65 |
| Real (BRL) | R$ 14,07 |
