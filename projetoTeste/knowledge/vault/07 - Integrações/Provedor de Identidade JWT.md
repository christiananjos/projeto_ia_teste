---
tipo: integracao
status: implementado
data: 2026-10-16
tags: [seguranca, jwt, autenticacao]
---

# Provedor de Identidade JWT

> Fonte: docs/SPEC.md (Segurança: JWT, GET autenticado, escrita com role Admin, chave via secrets) e [[ADR-010 Autenticação JWT com chave simétrica]].
> Situação: **implementado** em 2026-10-16 em `src/API/Configuracao/AutenticacaoExtensions.cs` (`JwtOptions`, `Politicas`). O provedor real ainda não foi definido; ver [[Provedor de identidade]].

- A API **só valida** tokens. Não existe endpoint de login nem emissão de token.
- O token é validado com HMAC-SHA256 usando a chave `Autenticacao:Jwt:ChaveAssinatura`, que tem no mínimo 32 bytes e vem de secrets ou variável de ambiente. Também são validados `Issuer`, `Audience` (padrão `catalogo-api`) e o lifetime, com ClockSkew de 30 s.
- O claim de role é `role` e o de nome é `sub` (`MapInboundClaims = false`).
- Policies: `Leitura` (usuário autenticado) para os GET e `Escrita` (role `Admin`) para POST, PUT, PATCH e DELETE. A `FallbackPolicy` exige usuário autenticado.
- As opções são validadas no startup (`ValidateDataAnnotations` + checagem de 32 bytes + `ValidateOnStart`): sem `Issuer`, `Audience` ou chave válida, a API não sobe. Como o `appsettings.json` traz `Issuer` vazio, em dev é preciso configurá-lo por user-secrets (`UserSecretsId` já está no `Catalogo.Api.csproj`).
- OpenAPI (`/openapi/v1.json`) só existe em Development, é anônimo e documenta o esquema Bearer.
- Nos testes de integração, o próprio teste gera o token com a chave de teste ([[ADR-013 Estratégia de testes com Testcontainers]]).
