---
tipo: integracao
status: planejado
data: 2026-10-16
tags: [seguranca, jwt, autenticacao]
---

# Provedor de Identidade JWT

> Fonte: docs/SPEC.md (Segurança: JWT, GET autenticado, escrita com role Admin, chave via secrets) e [[ADR-010 Autenticação JWT com chave simétrica]].
> Situação: **planejado, ainda não implementado**. O provedor real ainda não foi definido; ver [[Provedor de identidade]].

- A API **só valida** tokens. Não existe endpoint de login nem emissão de token.
- O token é validado com HMAC-SHA256 usando a chave `Autenticacao:Jwt:ChaveAssinatura`, que tem no mínimo 32 bytes e vem de secrets ou variável de ambiente. Também são validados `Issuer`, `Audience` (padrão `catalogo-api`) e o lifetime, com ClockSkew de 30 s.
- O claim de role é `role` e o de nome é `sub` (`MapInboundClaims = false`).
- Policies: `Leitura` (usuário autenticado) para os GET e `Escrita` (role `Admin`) para POST, PUT, PATCH e DELETE. A `FallbackPolicy` exige usuário autenticado.
- As opções são validadas no startup, e a API não sobe sem a chave.
- Nos testes de integração, o próprio teste gera o token com a chave de teste ([[ADR-013 Estratégia de testes com Testcontainers]]).
