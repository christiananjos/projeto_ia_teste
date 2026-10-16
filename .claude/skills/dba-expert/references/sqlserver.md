# Referência SQL Server / Azure SQL

## Obtendo o plano de execução
- Plano real: `SET STATISTICS IO, TIME ON;` e execute a query, ou peça "include actual execution plan" no SSMS.
- Plano estimado (sem executar): `SET SHOWPLAN_XML ON;`

## DMVs principais para diagnóstico
- Índices faltando: `sys.dm_db_missing_index_details`, `sys.dm_db_missing_index_group_stats`
- Uso de índices: `sys.dm_db_index_usage_stats`
- Queries caras no momento: `sys.dm_exec_query_stats` com join em `sys.dm_exec_sql_text`
- Bloqueio/deadlocks: `sys.dm_exec_requests` (coluna blocking_session_id), sessão de Extended Events `system_health` para gráficos de deadlock
- Wait stats (onde o engine está travando): `sys.dm_os_wait_stats`

## Hints e dicas de índice
- `INCLUDE` em índices não-clustered para criar índices de cobertura (covering) sem inchar a chave.
- Índices filtrados (cláusula `WHERE` no índice) são úteis para colunas booleanas/status esparsas.
- Rebuild vs reorganize: fragmentação >30% — rebuild; 5-30% — reorganize (checar `sys.dm_db_index_physical_stats`).

## Parameter sniffing
- Sintoma: query rápida para alguns valores de parâmetro, lenta para outros, mesmo plano reutilizado.
- Correções: `OPTION (RECOMPILE)` para casos pontuais, `OPTIMIZE FOR UNKNOWN`, ou planos forçados via query store para controle em escala.

## Específicos do Azure SQL
- O modelo DTU vs vCore muda como você raciocina sobre limites de recursos — vCore dá visibilidade real de CPU/memória/IO, DTU é uma abstração combinada.
- Elastic pools compartilham recursos entre bancos — cheque o consumo de DTU/vCore no nível do pool, não só métricas de um único banco, ao diagnosticar throttling.
- Query Performance Insight (Portal Azure) mostra as queries que mais consomem recursos sem precisar de acesso a DMVs.
- Automatic tuning (`ALTER DATABASE ... SET AUTOMATIC_TUNING`) pode criar/remover índices automaticamente — vale habilitar pelo menos FORCE_LAST_GOOD_PLAN.
