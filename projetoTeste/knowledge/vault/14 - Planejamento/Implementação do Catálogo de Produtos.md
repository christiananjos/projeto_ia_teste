---
tipo: planejamento
status: planejado
data: 2026-10-16
tags: [proximo-passo, implementacao]
---

# Implementação do Catálogo de Produtos

> Fonte: docs/SPEC.md (REQ-001 a REQ-004, BR-001 a BR-007). Arquitetura desenhada pelo 02-architect-sdd em 2026-10-16.

## Situação
A arquitetura está desenhada, mas **nenhum código foi escrito ainda**: as pastas `src/` estão vazias. O próximo passo é o 03-dotnet-specialist. Ao implementar cada item, ele marca o item como concluído aqui ou remove a nota.

## Componentes previstos
- [ ] Solução e infraestrutura de build: `Catalogo.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json` ([[ADR-002 Estrutura da solução e dependências]])
- [ ] Domain: `Categoria`, `Produto`, `Sku`, as interfaces de repositório, `IUnitOfWork` e as exceções
- [ ] Application: 11 casos de uso, 7 validators, DTOs, `Resultado`/`Erro` e as interfaces de consulta ([[ADR-003 Casos de uso, Result pattern e CQRS leve]])
- [ ] Infrastructure: `CatalogoDbContext`, as configurações com índices e collation, os repositórios, as consultas e a migration `Inicial` ([[Persistência EF Core]])
- [ ] API: endpoints de categoria e produto, ProblemDetails, JWT, policies e rate limiting ([[Tratamento de Erros e ProblemDetails]], [[Provedor de Identidade JWT]])
- [ ] Testes unitários e de integração com Testcontainers ([[ADR-013 Estratégia de testes com Testcontainers]])

## Relacionado
- [[Arquitetura da Solução]]
- [[Confirmar decisões que alteram comportamento]]
