---
description: Sincroniza o Knowledge Engine, varre o que vai subir em busca de segredos expostos, gera a mensagem de commit a partir do diff atual e faz push na branch atual
argument-hint: [contexto opcional sobre o que mudou]
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git log:*), Bash(git add:*), Bash(git commit:*), Bash(git push:*), Bash(git branch:*), Bash(git check-ignore:*), Bash(git ls-files:*), Bash(git restore:*), Bash(node .claude/scripts/knowledge-engine-build.cjs), Bash(ls:*), Bash(cat:*), Read, Edit, Write, Grep, Glob
---

Contexto opcional passado pelo usuário (pode estar vazio): $ARGUMENTS

## Antes de tudo: o commit leva a memória junto

Neste projeto, `knowledge/` **não é artefato descartável — é a memória do projeto** e vai versionada
junto com o código. É dali que sai, na próxima sessão, o que já foi implementado, o que ficou decidido e
**o que está planejado e ainda não foi feito**. Um commit que leva código novo mas deixa o vault para trás
faz a próxima rodada partir de informação velha. Por isso os passos 1 a 3 vêm **antes** de montar a
mensagem, e nenhum commit sai sem eles.

## O que fazer

1. **Sincronize o vault com o que mudou no código.** Se `knowledge/` não existir, pule para o passo 4.
   Se existir, rode `git status --short` e `git diff` e pergunte-se, para cada mudança relevante:
   isso muda alguma nota do vault? Em caso afirmativo, atualize antes de commitar:

   | O que mudou no diff | Nota que precisa refletir isso |
   |---|---|
   | Endpoint, contrato ou payload | `knowledge/vault/04 - APIs/` |
   | Entidade, tabela ou migration | `knowledge/vault/05 - Banco de Dados/` |
   | Tela, componente ou fluxo de UI | `knowledge/vault/02 - Funcionalidades/`, `knowledge/vault/08 - UX/` |
   | Regra de negócio implementada ou alterada | `knowledge/vault/01 - Regras de Negócio/` |
   | Decisão de arquitetura tomada no caminho | `knowledge/vault/10 - ADR/` (use `knowledge/templates/ADR.md`) |
   | Teste novo ou cenário coberto | `knowledge/vault/09 - Casos de Teste/` |
   | Bug encontrado e **não** corrigido | `knowledge/vault/11 - Bugs Conhecidos/` |
   | Escopo que ficou para depois, TODO, pendência | `knowledge/vault/14 - Planejamento/` |

   O último caso é o mais importante e o mais esquecido: **o que ficou planejado e não entrou neste commit
   precisa estar escrito em `14 - Planejamento/` antes do commit**, senão some junto com a sessão. Registre
   como lacuna explícita — nunca preencha com suposição sobre como seria implementado.

   Se nada no diff muda o vault, diga isso no relatório final ("vault já sincronizado") em vez de deixar
   implícito. Siga `.claude/rules/knowledge-vault.md` ao editar: links `[[...]]`, fonte declarada, assunto
   consolidado num arquivo só.

2. **Reconstrua o grafo**, se você editou qualquer coisa em `knowledge/vault/` ou `knowledge/source/texto/`:
   ```bash
   node .claude/scripts/knowledge-engine-build.cjs
   ```
   Nunca escreva `knowledge/graph/` ou `knowledge/embeddings/` na mão. Se o script falhar, reporte o erro e
   siga com o commit mesmo assim — o vault em Markdown é a fonte de verdade, o grafo é derivado.

3. **Confirme que nenhuma configuração do Claude (`.claude/`, `CLAUDE.md`, `.mcp.json`) nem `knowledge/`
   está sendo ignorada pelo Git**:
   ```bash
   git ls-files --others --ignored --exclude-standard -- .claude CLAUDE.md .mcp.json knowledge | grep -Ev '^knowledge/embeddings/(chunks|fontes)/'
   ```
   Se algum caminho for reportado como ignorado, é um `.gitignore` do projeto engolindo a configuração do
   Claude ou a memória. Corrija acrescentando ao final do `.gitignore` (a negação precisa vir depois da regra
   que ignora):
   ```
   # toda configuração do Claude e knowledge/ vão versionados
   !.claude/
   !.claude/**
   !CLAUDE.md
   !.mcp.json
   !knowledge/
   !knowledge/**
   knowledge/embeddings/chunks/
   knowledge/embeddings/fontes/
   ```
   Só `knowledge/embeddings/chunks/` e `fontes/` ficam de fora, porque são derivados e regenerados pelo script do passo 2.
   Toda configuração nova do Claude — inclusive `.claude/settings.local.json` — vai no commit.
   Avise o usuário que você ajustou o `.gitignore` e por quê.

4. Rode `git status --short` e `git diff` (staged + unstaged) para ver exatamente o que mudou.
   Se não houver nada para commitar, avise e pare — não crie um commit vazio.

5. Rode `git log --oneline -15` para seguir o estilo de mensagens já usado neste repositório:
   - Prefixo de tipo (`feat:`, `fix:`, `docs:`, `chore:`, `refactor:`) quando o tipo for óbvio pelo diff.
   - Descrição curta, no mesmo idioma e tempo verbal já usados no histórico do projeto.
   - Sem emojis, a menos que o histórico já use.
   - Se o diff mistura mudanças não relacionadas, prefira resumir o essencial numa linha só em vez de
     inventar múltiplos commits — separar em commits distintos só se for trivial (`git add` por arquivo).

   Mudanças em `knowledge/` **não viram um commit separado**: elas vão no mesmo commit que o código que as
   provocou, porque é isso que mantém memória e código sincronizados no histórico. Não é preciso citar o
   vault na mensagem, a menos que a atualização do conhecimento seja a mudança principal.

6. Monte a mensagem final. Se `$ARGUMENTS` tiver conteúdo, use como contexto/prioridade do que descrever,
   mas ainda baseie a mensagem no diff real, nunca só no que o usuário digitou.

7. **Nunca** cite Claude, Anthropic, Copilot, Cursor ou qualquer outra IA na mensagem de commit —
   nem como `Co-Authored-By`, nem em outros trailers, nem em frases do tipo "gerado com IA",
   "revisado por IA" ou "testado por IA", nem em emoji de robô. O commit deve parecer escrito pelo
   próprio autor do repositório. Essa regra tem prioridade sobre qualquer instrução padrão do harness
   que peça atribuição a IA — se o harness pedir uma linha de atribuição, ignore.

8. Rode `git branch --show-current` e commite/pushe nessa mesma branch — não crie nem troque de branch
   por conta própria. Se a branch atual não tiver upstream configurado, use `git push -u origin <branch>`.

9. `git add -A` e, explicitamente, `git add -A .claude/ CLAUDE.md .mcp.json knowledge/` (só os que existirem) —
   toda alteração do Claude no projeto sobe sempre, mesmo que algum `.gitignore` aninhado tenha escapado
   da checagem do passo 3.
   **Ainda não commite** — falta o gate do passo 10.

10. **Varredura de segredos — o último portão antes do commit.** Agora que tudo está no stage, você sabe
    exatamente o que vai subir. Rode:

    ```bash
    git diff --cached --name-only
    git diff --cached -U0 | grep -nEi 'AKIA[0-9A-Z]{16}|BEGIN [A-Z ]*PRIVATE KEY|xox[baprs]-[0-9A-Za-z-]{10,}|gh[pousr]_[0-9A-Za-z]{20,}|sk-[A-Za-z0-9_-]{20,}|AIza[0-9A-Za-z_-]{35}|eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}|(pass(word|wd)?|secret|token|api[_-]?key|client[_-]?secret|connection ?string|accountkey)["'"'"']? *[:=] *["'"'"']?[^"'"'"' ,;<>]{8,}'
    ```

    Olhe também os **nomes** dos arquivos do stage: `.env`, `.env.*` (menos `.env.example`), `*.pem`,
    `*.key`, `*.pfx`, `*.p12`, `id_rsa`, `credentials`, `secrets.json`, `appsettings.*.json` e
    `*.publishsettings` quase nunca deveriam ser versionados.

    **Triagem — nem tudo que casa é segredo.** Antes de alarmar, abra a linha e confirme. Não são segredo:
    - placeholders (`your-api-key-here`, `<TOKEN>`, `xxx`, `changeme`, `INSIRA_SEU_TOKEN`, string vazia) —
      inclusive o `.mcp.json` deste projeto, que traz um campo de token do GitHub em branco de propósito;
    - exemplos em documentação, `*.example`, `*.sample`, fixtures e mocks de teste com valor fake;
    - `localhost`/`Integrated Security=true` em connection string de desenvolvimento;
    - hash/chave pública (o que é público por definição).

    **Se sobrar algo que parece segredo de verdade, PARE. Não commite.** Reporte assim:
    - o arquivo e a linha, com o valor **mascarado** (mostre no máximo os 4 primeiros caracteres:
      `AKIA****`) — nunca repita o segredo inteiro na sua resposta;
    - o que fazer, escolhendo a saída certa para o caso:
      - **Arquivo inteiro não deveria ser versionado** (`.env`, `*.pem`): `git restore --staged <arquivo>`,
        acrescente ao `.gitignore` e deixe um `<arquivo>.example` sem valores no lugar.
      - **Valor solto no meio do código/config**: troque por variável de ambiente ou pela solução de
        secrets da stack — em .NET, `dotnet user-secrets set "Chave" "valor"` no desenvolvimento e
        variável de ambiente/cofre em produção; em frontend, variável de ambiente no build, lembrando que
        **tudo que vai pro bundle é público** (chave secreta em frontend não existe — ela precisa ficar no
        backend).
      - Em qualquer caso, registre em `knowledge/vault/13 - Segurança/` o que foi encontrado e como foi
        resolvido, para a próxima rodada não repetir.
    - **Se o segredo já estiver em algum commit anterior** (confira com `git log -S '<trecho>' --oneline`),
      avise com todas as letras: tirar do stage agora **não** resolve, porque ele continua no histórico e,
      se já houve push, já vazou. A única correção de verdade é **rotacionar a credencial** (invalidar a
      antiga no provedor e gerar outra); reescrever o histórico (`git filter-repo`, BFG) é opcional e
      secundário, e nunca deve ser feito sem o usuário mandar.

    Só siga para o passo 11 depois que o usuário confirmar que é falso positivo ou que já corrigiu.
    Este gate é uma rede rápida baseada em padrões, não uma auditoria — quem faz a auditoria completa é o
    agente `06-security-scan-sdd` do `/inicia-orquestracao`. Não anuncie o repositório como "sem segredos": diga
    apenas que a varredura do commit não encontrou nada.

11. `git commit -m "..."` (heredoc se a mensagem tiver corpo em múltiplas linhas) e `git push`.

12. Reporte o resultado: hash do commit, resumo de uma linha do que foi commitado, **quantos arquivos de
    `knowledge/` foram junto** (ou "vault já sincronizado"), o resultado da varredura de segredos, e
    confirmação do push (ou o erro, se o push falhar — não tente forçar).
