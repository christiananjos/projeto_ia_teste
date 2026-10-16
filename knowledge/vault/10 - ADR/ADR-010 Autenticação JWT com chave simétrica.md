---
tipo: adr
status: aceito
data: 2026-10-16
tags: [seguranca, jwt, L-10]
---

# ADR-010: Autenticação JWT com chave simétrica e sem endpoint de login

> Fonte: docs/SPEC.md (Segurança: JWT, role Admin, "chave JWT via configuração/secrets") e lacuna L-10 do 01-analyst-sdd. Decisão do 02-architect-sdd.

## Status
Aceito. O provedor definitivo continua em aberto; ver [[Provedor de identidade]].

## Contexto
A spec pede JWT, mas não define emissor, audience nem o claim de role, e não prevê endpoint de login.

## Decisão
- A API só valida tokens, com HMAC-SHA256.
- A chave vem de `Autenticacao:Jwt:ChaveAssinatura` e tem no mínimo 32 bytes.
- `Issuer` e `Audience` são configuráveis; a audience padrão é `catalogo-api`.
- O claim de role é `role` e o de nome é `sub`.
- Policies: `Leitura` (usuário autenticado) e `Escrita` (role `Admin`). A `FallbackPolicy` exige autenticação.
- As opções são validadas no startup com `ValidateOnStart`.

## Alternativas Consideradas
- Authority/OIDC com JWKS: é o ideal, mas ainda não há provedor definido.
- Login próprio: fora do escopo.

## Consequências
- O emissor dos tokens precisa compartilhar a chave simétrica com a API.
- A troca para OIDC fica isolada em `AutenticacaoExtensions`.

## Relacionado
- [[Provedor de Identidade JWT]]
- [[ADR-011 Rate limiting por IP]]
