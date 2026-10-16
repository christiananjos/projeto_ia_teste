---
tipo: caso-de-teste
status: implementado
data: 2026-10-16
tags: [testes, seguranca, jwt, rate-limit]
---

# CT Autenticação, autorização e rate limit

> Fonte: docs/SPEC.md (Segurança), [[ADR-010 Autenticação JWT com chave simétrica]], [[ADR-011 Rate limiting por IP]]. Testes do 05-test-engineer em `src/Tests/Catalogo.IntegrationTests/Api/AutenticacaoERateLimitTests.cs`, 2026-10-16. Todos executados (não precisam de Docker).

Os tokens são gerados no teste (`Infra/TokenJwt.cs`) com HS256 e a mesma chave/issuer/audience injetados pela factory.

| Cenário | Esperado | Teste |
|---|---|---|
| Os 11 endpoints sem token | 401 `nao_autenticado` (ProblemDetails) | `SemToken_Retorna401ProblemDetails` |
| Os 7 endpoints de escrita com token sem `Admin` | 403 `acesso_negado` | `EscritaComTokenSemRoleAdmin_Retorna403` |
| GET com token sem role | 200 | `LeituraComTokenSemRole_Retorna200` |
| Token expirado, issuer desconhecido, outra chave, malformado | 401 | `TokenInvalido_Retorna401` |
| 3ª requisição com limite 2, mesmo sem token | 429 `limite_requisicoes`, `Retry-After ≥ 1` | `RateLimitTests.AcimaDoLimite_Retorna429ComRetryAfterMesmoSemToken` |

Relacionado: [[Provedor de Identidade JWT]], [[Provedor de identidade]], [[Rate limit atrás de proxy e valor definitivo]], [[Estratégia e execução dos testes]].
