---
tipo: planejamento
status: em-aberto
data: 2026-10-16
tags: [testes, testcontainers, sql-server, proximo-passo]
---

# Testes de integração com SQL Server pendentes

> Fonte: execução do 05-test-engineer em 2026-10-16. O Docker CLI está instalado, mas o daemon não estava rodando (`DockerUnavailableException`: falha ao conectar em `npipe://./pipe/docker_engine`). Também não há SQL Server/LocalDB na máquina.

## Situação
Os 27 testes de `src/Tests/Catalogo.IntegrationTests/Persistencia/` estão **escritos e compilando, mas nunca foram executados**. Neste ambiente eles aparecem como ignorados, com o motivo. Por isso nada do que depende do banco real foi validado até agora: índices filtrados, collation CI, tradução de 2601/2627/547 com o nome da restrição, `ExecuteDelete` transacional da [[ADR-001 Remoção de categoria com produtos removidos]], materialização do `Sku` pelo conversor, ordenação e desempate por `uniqueidentifier`, e escape de `%`/`_` no `LIKE`.

## Próximo passo
1. Subir o Docker Desktop e rodar `dotnet test --collect:"XPlat Code Coverage"` na raiz.
2. Na CI, definir `CATALOGO_EXIGIR_DOCKER=true`, para que a falta de Docker reprove o build em vez de ignorar os testes.
3. Se algum teste falhar, registrar em `11 - Bugs Conhecidos/` com o comando que reproduz.
4. Medir de novo a cobertura da Infrastructure (hoje em 41,2%, só com os testes que não usam banco).

## Comando que reproduz a situação atual
```bash
dotnet test src/Tests/Catalogo.IntegrationTests --filter "FullyQualifiedName~Persistencia" --logger "console;verbosity=detailed"
```

## Relacionado
- [[Estratégia e execução dos testes]]
- [[ADR-013 Estratégia de testes com Testcontainers]]
- [[Implementação do Catálogo de Produtos]]
- [[SQL Server]]
