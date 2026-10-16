# Seu Projeto SDD

Projeto criado com **Pipeline SDD** — Stack: .NET 10 (Clean Architecture, somente backend)

Este é o único documento de entrada do projeto: o passo a passo para começar está aqui embaixo, e
`CLAUDE.md` (ao lado) é a memória que o Claude lê sozinho em toda sessão — você raramente precisa abrir.

---



## 📄 Passo 1 — (Opcional) Jogue sua documentação bruta em `docs/raw/`

Tem Word, PDF, planilhas, prints de wireframe, atas de reunião? Jogue tudo em `docs/raw/`
(veja `docs/raw/README.md`). Se essa pasta tiver arquivos, o `01-analyst-sdd` transforma tudo
numa Base de Conhecimento em `knowledge/` antes de qualquer outra coisa — e pode até deixar um rascunho
de `docs/SPEC.md` pronto pra você revisar. Os originais nunca são alterados.

## ✍️ Passo 2 — Edite a especificação

Abra `docs/SPEC.md` e descreva sua aplicação: requisitos funcionais, regras de negócio, modelo de dados,
endpoints.

## 🚀 Passo 3 — Rode o orchestrador

```
/inicia-orquestracao
```

Ele tem uma única pausa manual, logo após validar a spec — o resto roda automático (~15-25 min), só
parando de novo se um gate de qualidade (reviewer, testes, security scan) falhar.

## ✅ Passo 4 — Depois de executar

Você terá o código em src/ com build e testes passando de verdade, o relatório HTML da auditoria de segurança em docs/security-audit/, o workflow de testes da API em docs/api/ e os commits já aplicados com push. Arquitetura, decisões, regras, casos de teste e achados de segurança ficam em
`knowledge/` — a Base de Conhecimento que **persiste entre execuções** e que os agentes continuam
consultando conforme o projeto evolui. Em `output/` sobra só o `token-report.md`, com o custo em tokens
de cada rodada.

## 💾 Passo 5 — Commite

```
/commit
```

O `/commit` sincroniza o `knowledge/` com o que mudou no código **antes** de montar a mensagem, e leva o
vault no mesmo commit. É isso que faz a próxima sessão saber o que já foi feito e o que ficou planejado.

---

## 📁 Estrutura

```
seu-projeto/
├── README.md          📖 este arquivo — por onde começar
├── CLAUDE.md          🧠 memória do projeto (Claude lê a cada sessão)
├── .mcp.json          🔌 servidores MCP do projeto (docs atualizadas, GitHub...)
│
├── .claude/
│   ├── commands/       📌 COMANDOS DO PIPELINE
│   │   ├── inicia-orquestracao.md (comece por aqui!)
│   │   ├── commit.md       (commita código + memória juntos)
│   │   └── README.md
│   ├── agents/         (subagentes especializados, invocados pelo /inicia-orquestracao)
│   ├── rules/           (convenções aplicadas só quando Claude mexe nos arquivos certos)
│   ├── hooks/            (relatório de tokens + rebuild do grafo a cada edição do vault)
│   ├── scripts/           (reconstrução do grafo/embeddings do Knowledge Engine + limpeza de output/)
│   └── settings.json       (permissões + hook de tokens)
│
├── docs/
│   ├── SPEC.md       (sua especificação)
│   └── raw/           (opcional: sua documentação bruta — Word, PDF, planilhas...)
│
├── knowledge/         ✅ VERSIONADA — a memória do projeto
│   ├── source/        (documentos originais preservados; texto/ = texto integral pesquisável)
│   ├── vault/          (conteúdo organizado em Markdown, compatível com Obsidian)
│   │                    inclui "14 - Planejamento/": o que falta implementar
│   ├── graph/          (grafo de relacionamentos entre documentos)
│   ├── embeddings/     (chunks prontos para busca semântica — chunks/ não vai pro Git)
│   ├── cache/           (contexto resumido por agente)
│   └── templates/       (modelos Feature/API/ADR/Bug/TestCase)
│
├── output/           ✅ só token-report.md vai pro Git (relatórios da rodada ficam de fora)
│
└── src/              (.NET Clean Architecture)
    ├── Domain/
    ├── Application/
    ├── Infrastructure/
    ├── API/
    └── Tests/
```

## 🧠 O que vai pro Git: `knowledge/` inteira, `output/` só com o relatório de tokens

`output/` é o canal de passagem entre os agentes de **uma** rodada do `/inicia-orquestracao`: relatório de
cada agente, spec técnica, matriz de rastreabilidade. Quando a rodada termina, esses relatórios são apagados
(o que vale guardar já foi para o vault) e sobra só o `token-report.md`; se um gate reprovar, eles ficam lá
pra explicar o motivo. Por isso o `.gitignore` versiona só o `token-report.md`. Como ele é atualizado pelo
hook quando a rodada já terminou, entra no primeiro commit da rodada seguinte (ou no próximo `/commit`).

`knowledge/` é o contrário: é a memória acumulada do projeto, e **vai versionada junto com o código**
(menos `knowledge/embeddings/chunks/`, que é derivado e se regenera com
`node .claude/scripts/knowledge-engine-build.cjs`). Todo agente do pipeline atualiza o vault antes de
encerrar, e o `/commit` leva essas mudanças no mesmo commit do código que as provocou. Em especial,
`knowledge/vault/14 - Planejamento/` guarda **o que está planejado e ainda não foi implementado** — sem
isso, o plano morreria junto com a sessão.

Não acrescente `knowledge/` ao `.gitignore`.

## ⚙️ CLAUDE.md, .mcp.json, rules e permissões

Este projeto já sai alinhado à estrutura recomendada pela documentação oficial do Claude Code:

- **`CLAUDE.md`** — memória do projeto, carregada em toda sessão. Edite à vontade conforme o projeto
  evolui (tem uma seção "Convenções deste projeto" reservada pra isso no fim do arquivo).
- **`.mcp.json`** — servidores MCP do projeto (documentação atualizada de bibliotecas via `context7`, e um
  exemplo de GitHub pronto pra você só preencher o token). Na primeira vez que abrir a pasta, o Claude Code
  pede aprovação desses servidores (workspace trust) — é esperado, não é erro.
- **`.claude/rules/`** — convenções (Clean Architecture, componentes/estado, vault, etc.) que só entram no
  contexto quando o Claude mexe em arquivos que batem o padrão certo, em vez de pesar em toda sessão.
- **`.claude/settings.json`** — já sai com um bloco `permissions` liberando leitura e as ações que o
  próprio pipeline precisa (escrita em `output/`, `docs/`, `knowledge/`, `src/`, build/test da stack), pra
  `/inicia-orquestracao` não ficar parando pra pedir aceite o tempo todo. Aprovações extras que você conceder
  durante a sessão ("don't ask again") caem em `.claude/settings.local.json`, que também vai versionado.

**Trabalhar em duas frentes ao mesmo tempo?** Rode `claude --worktree nome-da-frente` — cada sessão trabalha
num checkout isolado do Git, então duas rodadas de `/inicia-orquestracao` (ex: duas features diferentes) não
esbarram nos mesmos arquivos.

## 🔄 Manter o pipeline atualizado

```
/atualizar-versao
```

Atualiza o plugin `sdd` e reaplica a estrutura do template neste projeto, preservando `docs/raw/`,
`knowledge/`, `src/` e seus documentos.

---

**Comece agora:**

```
/inicia-orquestracao
```

---

**Projeto criado com Claude SDD v5.1.0**
