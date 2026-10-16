# 📌 Comandos do Pipeline SDD

Stack deste projeto: **.NET 10 (somente backend)**

## 🎯 Fluxo Recomendado

1. **(Opcional) Jogue sua documentação bruta em `docs/raw/`**
   ```
   cp suas-especificacoes.docx docs/raw/
   ```
   Word, PDF, planilhas, imagens — o que tiver. Veja `docs/raw/README.md`.

2. **Edite sua especificação**
   ```
   nano docs/SPEC.md
   ```

3. **Execute o orchestrador**
   ```
   /inicia-orquestracao
   ```

4. **Pronto!** Os subagentes (pasta `.claude/agents/`) rodam automaticamente em cascata — começando pelo
   `01-analyst-sdd`, que consolida `docs/raw/` (se tiver arquivos) e valida a spec

## 📚 Estrutura

- **`.claude/commands/`** — Comandos que você chama diretamente (`/inicia-orquestracao`)
- **`.claude/agents/`** — Os subagentes especializados que o `/inicia-orquestracao` invoca automaticamente. Você não precisa chamá-los manualmente, mas ficam aqui documentados caso precise entender ou ajustar o comportamento de um deles no futuro.
- **`docs/raw/`** — Documentação bruta de entrada (opcional). Se usada, vira a Base de Conhecimento em `knowledge/`.

## 🤖 Os Agentes (em `.claude/agents/`)

| Agente | Responsabilidade |
|--------|-------------------|
| `01-analyst-sdd` | Consolida `docs/raw/` numa Base de Conhecimento em `knowledge/` (se tiver arquivos) e valida a especificação |
| `02-architect-sdd` | Gera arquitetura técnica |
| `03-dotnet-specialist` | Implementa backend .NET |
| `04-reviewer-sdd` | Revisa conformidade com a spec e qualidade do código, numa leitura só |
| `05-test-engineer` | Escreve e roda os testes (unit + integração), mede cobertura e gera o workflow de testes da API |
| `06-security-scan-sdd` | Audita 5 falhas de segurança e gera relatório HTML |

O commit não é um subagente: depois de cada etapa aprovada, o `/inicia-orquestracao` commita localmente na conversa principal (fluxo do `/commit`, com gate de segredos) e só dá push no fim, depois do `06-security-scan-sdd`.


## 🧩 Comandos avulsos

- `/commit` — a qualquer momento, fora do pipeline: gera a mensagem de commit a partir do diff atual e faz push na branch atual (nunca cita Claude, Anthropic ou qualquer outra IA na mensagem).
- `/raio-x-projeto` — em projeto legado sem documentação: faz uma varredura técnica completa (arquitetura, banco de dados, interfaces, services, infraestrutura) e grava tudo em `docs/raw/`, separado por tema.

## ⏱️ Tempo

- Pipeline completo (`/inicia-orquestracao`): 15-25 minutos

## 💡 Dicas

1. Use `/inicia-orquestracao` para rodar o pipeline completo
2. Revise resultados em `output/` a cada etapa
3. Se precisar reexecutar só uma etapa específica, você pode pedir ao Claude para usar aquele agente novamente pelo nome

---

**Comece aqui:** `/inicia-orquestracao`
