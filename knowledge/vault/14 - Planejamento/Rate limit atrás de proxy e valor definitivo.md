---
tipo: planejamento
status: em-aberto
data: 2026-10-16
tags: [seguranca, rate-limiting, em-aberto]
---

# Rate limit atrás de proxy e valor definitivo

> Fonte: docs/SPEC.md (Segurança: "Rate limiting (ex.: 100 req/min por IP)") e lacuna L-10 do 01-analyst-sdd.

## Em aberto
- A spec dá o limite de 100 req/min apenas como exemplo. Até alguém confirmar o valor real, esse é o padrão configurável.
- A topologia de deploy (proxy reverso, load balancer) não foi informada. Se houver proxy, é preciso configurar `ForwardedHeaders` com a lista de proxies confiáveis. Sem isso, todos os clientes compartilham o mesmo limite. Essa configuração não faz parte desta rodada.

## Relacionado
- [[ADR-011 Rate limiting por IP]]
