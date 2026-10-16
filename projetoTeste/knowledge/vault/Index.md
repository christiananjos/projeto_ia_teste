# Index do Knowledge Vault

> Fonte: índice mantido pelos agentes do pipeline SDD. Criado pelo 02-architect-sdd em 2026-10-16; notas de 04 e 05 criadas pelo 03-dotnet-specialist em 2026-10-16. A Parte A do Analyst foi pulada porque `docs/raw/` estava vazia, por isso ainda não há notas em 00 a 03.

## 04 - APIs
- [[API de Categorias]]
- [[API de Produtos]]

## 05 - Banco de Dados
- [[Modelo de Dados do Catálogo]]

## 06 - Arquitetura
- [[Arquitetura da Solução]]
- [[Persistência EF Core]]
- [[Tratamento de Erros e ProblemDetails]]

## 07 - Integrações
- [[Provedor de Identidade JWT]]
- [[SQL Server]]

## 10 - ADR
- [[ADR-001 Remoção de categoria com produtos removidos]]
- [[ADR-002 Estrutura da solução e dependências]]
- [[ADR-003 Casos de uso, Result pattern e CQRS leve]]
- [[ADR-004 Minimal APIs com ProblemDetails]]
- [[ADR-005 Contrato de Categoria]]
- [[ADR-006 Contrato de Produto]]
- [[ADR-007 Espaços nas pontas rejeitados com 400]]
- [[ADR-008 Unicidade case-insensitive via collation]]
- [[ADR-009 Paginação, filtros e ordenação]]
- [[ADR-010 Autenticação JWT com chave simétrica]]
- [[ADR-011 Rate limiting por IP]]
- [[ADR-012 Sem concorrência otimista]]
- [[ADR-013 Estratégia de testes com Testcontainers]]
- [[ADR-014 Validação de preço]]
- [[ADR-015 Exclusão lógica, UUID v7 e datas pelo servidor]]

## 11 - Bugs Conhecidos
- [[SKU aceita quebra de linha no final]] (resolvido em 2026-10-16)

## 14 - Planejamento
- [[Implementação do Catálogo de Produtos]]
- [[Confirmar decisões que alteram comportamento]]
- [[Provedor de identidade]]
- [[Rate limit atrás de proxy e valor definitivo]]
- [[Concorrência otimista]]
- [[Auditoria de produtos removidos]]
- [[Listagem de produtos ativos e inativos]]
- [[Health checks e observabilidade]]
