# /inicia-orquestracao - Executar Pipeline SDD

> Execute os agentes automaticamente para gerar código baseado em sua especificação.

## 📋 Como Usar

1. **(Opcional) Documentação bruta** — se você tiver Word, PDF, planilhas, prints de wireframe, atas de
   reunião etc., coloque tudo em `docs/raw/` (veja `docs/raw/README.md`). Se essa pasta tiver arquivos, o
   `01-analyst-sdd` transforma tudo numa Base de Conhecimento em `knowledge/` antes de validar a spec.

2. **Prepare sua especificação**
   - Edite `docs/SPEC.md` com seus requisitos (se usou `docs/raw/`, o Analyst pode preencher um rascunho aqui
     pra você revisar).

3. **Chame o orchestrador**
   ```
   /inicia-orquestracao
   ```

4. **Aprove a validação da especificação**
   - O pipeline roda o `Analyst` e **pausa** — mostra o relatório completo e pergunta se você aprova seguir
     (veja "Pausa de Aprovação" abaixo)

5. **Aguarde ~15-25 minutos**
   - Depois da sua aprovação, o resto executa em cascata, sem novas pausas
   - Resultados salvos em `output/`

## 🎯 O que Acontece

São **6 subagentes**. Depois de cada um, o trabalho da etapa é commitado aqui mesmo, na conversa principal
(commit local); o push acontece uma vez só, no fim:

```
docs/raw/ (opcional) + docs/SPEC.md
    ↓
🎯 01 Analyst          → Consolida docs/raw/ em knowledge/ (se houver arquivos) e valida a spec
    ↓
⏸️ Pausa — você aprova seguir? (única pausa do pipeline)
    ↓ (só continua se você aprovar)
🏛️ 02 Architect        → Gera arquitetura + matriz de rastreabilidade
    ↓
🔷 03 .NET Specialist → Implementa o código .NET em src/ (só termina com dotnet build passando)
    ↓
🔍 04 Reviewer         → Conformidade com a spec + qualidade do código, numa leitura só
    ↓
🧪 05 Test Engineer   → Escreve e RODA os testes (unit + integração), mede cobertura e gera docs/api/testes-api.md
    ↓
🛡️ 06 Security Scan    → Auditoria de segurança (5 categorias) + relatório HTML
    ↓
🚀 Push               → Sobe os commits de todas as etapas (só se todos os gates passaram)
    ↓
✅ Pronto!
```

## ⏸️ Pausa de Aprovação (única do pipeline)

Assim que o `Analyst` gerar `output/1-analyst.md`, o pipeline **para** e mostra o relatório completo
formatado (status da spec, base de conhecimento, requisitos, regras de negócio, lacunas e recomendação) —
**mesmo que o status seja ✅ APROVADO**. Em seguida pergunta objetivamente:

> "A validação da especificação ficou assim [relatório]. Aprova seguir para a arquitetura e o resto do
> pipeline?"

- Se você **aprovar**, o restante do pipeline roda **automaticamente até o fim**, sem pedir mais nenhuma
  confirmação (só interrompe de novo se um gate técnico reprovar — ver regra abaixo).
- Se você **não aprovar**, o pipeline **para ali**, sem rodar `Architect` nem nenhum agente seguinte, até
  você ajustar `docs/SPEC.md` (ou o que for apontado no relatório) e chamar `/inicia-orquestracao` de novo.

## ⚠️ Regras de Execução

- **Antes do Analyst, limpe a rodada anterior**: rode `node .claude/scripts/limpar-output.cjs`. Ele apaga os
  relatórios que sobraram em `output/` (inclusive de versões antigas do pipeline) e mantém o
  `token-report.md` — assim nenhum agente lê por engano um relatório velho.
- **Um agente por vez, na ordem acima.** Cada agente lê o relatório do anterior em `output/` — não repasse
  o conteúdo dos relatórios no prompt de invocação, só aponte o arquivo.
- **Pare em qualquer gate técnico reprovado (depois da aprovação inicial)**: se `04-reviewer-sdd`
  (❌ REPROVADO), `05-test-engineer` (❌ FAILED) ou `06-security-scan-sdd` (❌ REPROVADO) reportar falha,
  interrompa o pipeline e reporte ao usuário o que precisa ser corrigido. Não gaste as etapas seguintes nem
  commite o trabalho da etapa reprovada — ele fica no working tree pra ser corrigido.
- **Commit a cada etapa (sem subagente, sem push)**: assim que um agente terminar **e** o gate dele passar,
  commite aqui mesmo, na conversa principal, o que aquela etapa produziu — código, testes e as notas de
  `knowledge/` que ela provocou, juntos. Siga os passos 3, 4, 5, 7, 9, 10 e 11 de `.claude/commands/commit.md`
  (checagem do `.gitignore`, diff, estilo do histórico, mensagem sem citar IA, stage, **gate de segredos** e
  commit), sem pedir confirmação, com uma diferença: **não faça push** — o `git push` fica só para o fim. O
  vault já foi sincronizado pelo próprio agente, então os passos 1 e 2 não precisam rodar de novo.

  | Etapa | O que vai no commit | Tipo típico |
  |---|---|---|
  | 01 Analyst (depois da sua aprovação) | `docs/SPEC.md`, `knowledge/` e o `output/token-report.md` da rodada anterior | `docs(spec):` |
  | 02 Architect | `knowledge/` (arquitetura, ADRs, planejamento) | `docs(arquitetura):` |
  | 03 Specialist | código em `src/` + notas do vault — **um commit por unidade lógica** | `feat(...)` |
  | 04 Reviewer | correções que ele aplicou, se houver | `fix:` / `refactor:` |
  | 05 Test Engineer | testes (e `docs/api/`, se gerou) | `test(...)` |
  | 06 Security Scan | correções de segurança + relatório HTML em `docs/security-audit/` | `fix(security):` / `docs(security):` |

  Exemplo de mensagens para a etapa do specialist:

```
feat(domain): adicionar entidade Tarefa e regras de validação
feat(application): implementar casos de uso de criação e listagem de tarefas
feat(infrastructure): configurar EF Core e repositório de tarefas
feat(api): adicionar controllers REST para tarefas
test(application): adicionar testes unitários dos casos de uso de tarefas
docs(spec): adicionar especificação técnica gerada pelo pipeline SDD
```

  Se uma etapa não mudou nada versionável, pule o commit dela — nunca crie commit vazio. Se o gate de
  segredos travar, não commite aquela etapa, pare o pipeline e reporte.

  Por que commitar por etapa e só dar push no fim: cada etapa vira um ponto de restauração no histórico
  (dá pra ver e reverter o que cada agente fez), mas nada sobe para o remoto antes de passar pela revisão,
  pelos testes e pela auditoria de segurança. Se um gate reprovar, os commits das etapas anteriores ficam
  **locais** — avise o usuário disso no relatório.
- **Push no fim**: depois do commit da etapa do `06-security-scan-sdd`, rode `git branch --show-current` e
  `git push` nessa mesma branch (`git push -u origin <branch>` se não houver upstream). Não crie nem troque
  de branch por conta própria. Se o push falhar, reporte o erro — não force.
- **Marque a rodada como concluída**: só depois do push, crie o arquivo vazio
  `output/.rodada-concluida`. Ao encerrar a resposta, o hook de tokens atualiza o `token-report.md` e, vendo o
  marcador, apaga os relatórios intermediários. Rodada interrompida por gate reprovado **não** ganha marcador:
  os relatórios ficam em `output/` pra explicar o que precisa ser corrigido.

## 📁 Resultados

Durante a rodada, `output/` é o canal de passagem entre os agentes:

```
1-analyst.md                 (Base de Conhecimento + validação da spec)
TECHNICAL_SPECIFICATION.md   (Arquitetura)
TRACEABILITY_MATRIX.md       (Rastreabilidade)
TECHNICAL_DECISIONS.md       (Decisões)
3-dotnet-specialist.md        (Relatório da implementação .NET)
4-review.md                  (Conformidade + qualidade)
5-tests.md                   (Build, testes e cobertura — executados de verdade)
6-security-scan.md           (Auditoria de Segurança — 5 categorias)
```

Ao fim de uma rodada concluída, sobra só `token-report.md` (uso de tokens do pipeline): tudo o que vale
guardar desses relatórios já foi para `knowledge/`, que persiste entre execuções como base de conhecimento
viva do projeto. O relatório HTML da auditoria fica em `docs/security-audit/`.

## ✅ Pré-requisitos

- ✅ `docs/SPEC.md` preenchida **ou** `docs/raw/` com documentação bruta
- ✅ SDK da stack instalado (para build e testes rodarem de verdade — sem ele, o Test Engineer cai para revisão
  estática e avisa no relatório)

## 🚀 Comece Agora

```
/inicia-orquestracao
```
