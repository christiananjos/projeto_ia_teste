---
tipo: seguranca
status: aprovado-com-ressalvas
data: 2026-10-16
tags: [seguranca, auditoria, jwt]
---

# Auditoria 2026-10-16

> Fonte: auditoria do 06-security-scan sobre o código de `src/`, as configs, `docs/` e o histórico Git (9 commits). Relatório completo em `docs/security-audit/relatorio-auditoria-seguranca.html`.

**Resultado:** aprovado com ressalvas. Não há achado Crítico, Alto ou Médio, e nada foi corrigido nesta etapa.

## Mapeamento das cinco categorias para esta stack
- **Isolamento de inquilino/dono:** não se aplica. O catálogo é global, sem dono nem tenant (ver [[Modelo de Dados do Catálogo]]). O único filtro global é o de exclusão lógica ([[Persistência EF Core]]).
- **Permissão no navegador:** não se aplica, porque não há frontend. A autorização do servidor está correta em todos os endpoints ([[API de Categorias]], [[API de Produtos]]).
- **IDOR:** os 7 handlers com `{id:guid}` foram verificados. O acesso é por papel (leitura autenticada, escrita `Admin`) e não existe posse por usuário.
- **Chaves expostas:** não há segredo em código, config ou histórico. A chave JWT e a connection string vêm de secrets ou variáveis de ambiente.
- **XSS/injeção:** a API só responde JSON e não executa SQL bruto (só LINQ parametrizado).

## Achados abertos
| # | Severidade | Arquivo:linha | Descrição |
|---|-----------|---------------|-----------|
| 1 | Baixa | `src/API/Configuracao/AutenticacaoExtensions.cs:20-26` | O startup aceita qualquer chave JWT com 32 bytes ou mais, inclusive a chave de teste versionada (`TokenJwt.cs:15`). Pendência em [[Rejeitar chaves JWT conhecidas no startup]] |

## Informativos (sem ação)
- `src/Tests/Catalogo.IntegrationTests/Infra/TokenJwt.cs:15`: chave fixa usada só nos testes.
- `src/Infrastructure/Persistencia/CatalogoDbContextFactory.cs:12-13`: LocalDB de design-time, sem credencial.

## Pontos fortes confirmados
- As policies `Leitura`/`Escrita` e a `FallbackPolicy` autenticada cobrem os 11 endpoints ([[Provedor de Identidade JWT]], [[ADR-010 Autenticação JWT com chave simétrica]]).
- `ValidAlgorithms = [HmacSha256]`, validação de issuer, audience e lifetime, e `ValidateOnStart`.
- O 500 é genérico, sem vazar detalhes ([[Tratamento de Erros e ProblemDetails]]).

## Decisões
- Nada foi corrigido: não há Critical/High, e Medium/Low ficam só reportados por regra do gate.
- Observação fora do escopo das cinco categorias: rate limit atrás de proxy, já em [[Rate limit atrás de proxy e valor definitivo]].

## Para a próxima auditoria
- Se o achado 1 continuar aberto, ele vale do jeito que está e não deve ser reportado como novo.
- Reavaliar as categorias 1, 2 e 5 se surgir frontend, multi-tenant ou posse de registro por usuário.
