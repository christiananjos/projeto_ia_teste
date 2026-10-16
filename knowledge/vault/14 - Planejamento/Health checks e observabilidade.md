---
tipo: planejamento
status: adiado
data: 2026-10-16
tags: [observabilidade, escopo-adiado]
---

# Health checks e observabilidade

> Fonte: decisão do 02-architect-sdd. A spec não pede health check, métricas nem tracing.

## Escopo adiado
Esta rodada não cria endpoint `/health` (nem verificação do banco), métricas ou OpenTelemetry. O que existe é o log via `ILogger` e o `traceId` nos ProblemDetails ([[Tratamento de Erros e ProblemDetails]]). Isso volta à pauta quando a topologia de deploy for definida.
