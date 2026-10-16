# Checklist de Plano de Teste

Use este checklist ao estruturar o plano de teste completo de uma feature nova.

## 1. Escopo
- [ ] O que será testado está claramente listado (funcionalidades, integrações, plataformas/navegadores se aplicável).
- [ ] O que **não** será testado nesta rodada está explícito (evita ambiguidade sobre cobertura assumida).
- [ ] Dependências externas envolvidas (APIs de terceiros, filas, outros serviços) estão identificadas.

## 2. Análise de risco
- [ ] Áreas de alto impacto de negócio identificadas (o que mais dói se quebrar).
- [ ] Áreas de alta complexidade técnica identificadas (mais propensas a bug por natureza).
- [ ] Cruzamento impacto x probabilidade feito pra priorizar onde investir mais teste.

## 3. Casos de teste
- [ ] Fluxo feliz (caminho principal, entrada válida) coberto.
- [ ] Casos negativos/de erro (entrada inválida, recurso inexistente, permissão negada) cobertos.
- [ ] Valores limite testados (mínimo, máximo, um abaixo, um acima).
- [ ] Combinações de regras de negócio relevantes cobertas (via tabela de decisão se a complexidade justificar).
- [ ] Casos de concorrência/condição de corrida considerados, se a feature envolve múltiplos usuários/processos simultâneos.

## 4. Automação
- [ ] Definido o que será automatizado (unitário/integração) vs o que ficará manual/exploratório.
- [ ] Testes automatizados novos seguem o padrão AAA e nomenclatura descritiva de comportamento.
- [ ] Dados de teste (fixtures, seeds) definidos e isolados entre execuções (sem dependência de ordem de execução ou estado deixado por outro teste).

## 5. Ambiente e dados
- [ ] Ambiente de teste definido (local, staging, com quais dados).
- [ ] Massa de dados necessária identificada e disponível antes do início dos testes.
- [ ] Acesso/credenciais necessários pra testar todos os perfis de usuário relevantes (admin, usuário comum, etc.) garantidos.

## 6. Critério de saída
- [ ] Definido o que significa "testado o suficiente" (ex: todos os casos críticos passando, sem bug de severidade alta/crítica aberto).
- [ ] Bugs conhecidos e aceitos pra este release (se houver) documentados explicitamente, não deixados implícitos.

## 7. Comunicação
- [ ] Responsável por cada frente de teste (automação, exploratório, regressão) identificado.
- [ ] Canal/processo definido pra reportar bugs encontrados durante a execução do plano.
