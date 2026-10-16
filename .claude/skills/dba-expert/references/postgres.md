# Referência PostgreSQL

## Obtendo o plano de execução
- `EXPLAIN ANALYZE <query>;` — sempre use ANALYZE (não só EXPLAIN) para ver linhas reais vs estimadas, que é onde a maioria dos problemas reais aparece.
- `EXPLAIN (ANALYZE, BUFFERS)` adiciona informação de hit/read de buffer — essencial para diagnosticar queries limitadas por I/O.

## Views de sistema principais para diagnóstico
- Índices faltando/não usados: `pg_stat_user_indexes` (idx_scan = 0 — candidato a remoção)
- Bloat de tabela / saúde do vacuum: `pg_stat_user_tables` (n_dead_tup, last_autovacuum)
- Queries rodando agora: `pg_stat_activity`
- Espera de locks: `pg_locks` com join em `pg_stat_activity`
- Log de queries lentas: habilite a extensão `pg_stat_statements` para performance agregada de queries ao longo do tempo

## VACUUM / ANALYZE
- Autovacuum vem ligado por padrão — não desabilite; ajuste os thresholds (`autovacuum_vacuum_scale_factor`) se ele não estiver dando conta.
- `ANALYZE` atualiza as estatísticas do planner — estatísticas desatualizadas são uma causa comum de escolha de plano ruim após cargas em massa; rode `ANALYZE` manualmente após grandes importações.
- Bloat de tabela por updates/deletes frequentes sem vacuum suficiente — considere `VACUUM FULL` (trava a tabela) só como último recurso durante janela de manutenção.

## Dicas de indexação
- B-tree é o padrão e a escolha certa para queries de igualdade/range; GIN para JSONB/array/full-text; GiST para tipos geométricos/range.
- Índices parciais (cláusula `WHERE`) para colunas esparsas de status, mesma ideia dos índices filtrados do SQL Server.
- `CREATE INDEX CONCURRENTLY` para evitar travar a tabela ao criar índice em produção.

## Connection pooling
- Conexões no Postgres são relativamente caras (cada uma é um processo de OS completo) — use PgBouncer ou o pooling nativo do Npgsql para apps .NET sob carga.
- Pooling em modo transaction (PgBouncer) geralmente é o certo para workloads típicos de aplicação web; modo session só se precisar de recursos de nível de sessão (advisory locks, prepared statements entre requests).

## Migrations em escala
- Adicionar coluna com `DEFAULT` no Postgres 11+ é rápido (sem reescrever a tabela) para defaults constantes — mas cheque o SQL gerado pelo EF Core, pois versões antigas do provider podem não usar esse caminho.
- Adicionar `NOT NULL` em uma tabela grande existente exige varredura completa para validar — considere `NOT VALID` + `VALIDATE CONSTRAINT` (duas etapas) para evitar um lock longo.
