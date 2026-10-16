---
tipo: integracao
status: planejado
data: 2026-10-16
tags: [sql-server, banco]
---

# SQL Server

> Fonte: docs/SPEC.md (Persistência: EF Core com SQL Server, connection string via configuração/secrets).
> Situação: **planejado, ainda não implementado**.

- Provider `Microsoft.EntityFrameworkCore.SqlServer` 10.0.x, com `EnableRetryOnFailure`.
- Connection string `ConnectionStrings:Catalogo`. Em dev vem de `dotnet user-secrets`; em produção, da variável `ConnectionStrings__Catalogo`.
- As colunas de texto únicas usam collation explícita (`Latin1_General_100_CI_AS`), sem depender do padrão do servidor. Ver [[ADR-008 Unicidade case-insensitive via collation]].
- Os testes de integração rodam contra SQL Server real em container (Testcontainers), o que exige Docker. Ver [[ADR-013 Estratégia de testes com Testcontainers]].
- Mapeamento das tabelas: [[Persistência EF Core]].
