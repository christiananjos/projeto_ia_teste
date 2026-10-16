---
tipo: planejamento
status: em-aberto
data: 2026-10-16
tags: [seguranca, jwt, em-aberto]
---

# Provedor de identidade

> Fonte: lacuna L-10 do 01-analyst-sdd. A spec não define emissor, audience, claim de role nem endpoint de login.

## Em aberto
- Não está definido quem emite os tokens nem qual é o valor real de `Issuer` em cada ambiente.
- A versão atual valida tokens com chave simétrica ([[ADR-010 Autenticação JWT com chave simétrica]]). Falta decidir se a validação passa para Authority/OIDC com JWKS quando houver um provedor definido.
- Não há gestão de usuários nem endpoint de login. Isso está fora do escopo da spec.

## Relacionado
- [[Provedor de Identidade JWT]]
