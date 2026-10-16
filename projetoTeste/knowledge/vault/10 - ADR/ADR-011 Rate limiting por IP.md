---
tipo: adr
status: aceito
data: 2026-10-16
tags: [seguranca, rate-limiting, L-10]
---

# ADR-011: Rate limiting fixed window por IP

> Fonte: docs/SPEC.md (Segurança: "Rate limiting (ex.: 100 req/min por IP)") e lacuna L-10 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito. O valor definitivo ainda depende de confirmação; ver [[Rate limit atrás de proxy e valor definitivo]].

## Contexto
A spec cita 100 req/min por IP apenas como exemplo e não define o algoritmo.

## Decisão
- `GlobalLimiter` do tipo fixed window, particionado por `RemoteIpAddress`.
- Configurável em `RateLimiting:LimitePorJanela` (padrão 100) e `RateLimiting:JanelaSegundos` (padrão 60), sem fila.
- Ao estourar o limite, responde 429 com `Retry-After` e ProblemDetails.
- O limiter roda antes da autenticação.

## Alternativas Consideradas
- Sliding window ou token bucket: mais complexos, sem um requisito que justifique.
- Partição por usuário: deixaria requisições anônimas sem limite.

## Consequências
- Atrás de um proxy, todos os clientes aparecem com o mesmo IP. Para corrigir, é preciso configurar `ForwardedHeaders`, o que está pendente.

## Relacionado
- [[ADR-010 Autenticação JWT com chave simétrica]]
- [[Tratamento de Erros e ProblemDetails]]
