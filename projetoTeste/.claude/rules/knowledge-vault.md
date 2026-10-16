---
paths:
  - "knowledge/vault/**/*.md"
---

# Convenções do Knowledge Vault

- Use links internos no estilo Obsidian (`[[Nome do Outro Documento]]`) para conectar regras, funcionalidades,
  APIs, tabelas e testes relacionados entre si.
- Todo documento deve indicar sua origem (ex.: `> Fonte: Especificacao.docx`) para manter rastreabilidade.
- Nunca invente informação: se algo estiver ambíguo ou faltando, registre como lacuna em vez de completar com
  suposição.
- Se o mesmo assunto aparecer em documentos diferentes, consolide num único arquivo em vez de duplicar.
- Depois de editar o vault, rode `node .claude/scripts/knowledge-engine-build.cjs` para reconstruir o grafo e
  os chunks de embeddings — não escreva `knowledge/graph/` ou `knowledge/embeddings/` manualmente.
- **O vault é a fonte de conhecimento do projeto, não a documentação bruta.** Todo agente lê `knowledge/`
  primeiro, na ordem cache → vault → chunks (`grep` em `knowledge/embeddings/chunks/` e
  `knowledge/embeddings/fontes/`) → `knowledge/source/texto/`. Nunca reabre `docs/raw/` nem converte os
  binários para pasta temporária — o texto integral das fontes já está em `knowledge/source/texto/`.
- **Antes de dar algo como pendente ou sem decisão, confira a fonte** (chunks de `fontes/` e
  `knowledge/source/texto/`): o vault é resumo e pode ter deixado passar a resposta.
- **Toda implementação atualiza o vault.** Qualquer agente que produza ou altere algo (código, arquitetura,
  testes, achados de segurança) confere ao terminar se aquilo muda alguma nota e atualiza antes de encerrar —
  cada agente tem a seção "Sincronização do Knowledge Engine" dizendo quais pastas são dele. Se nada mudou,
  ele diz isso explicitamente no relatório, para não restar dúvida se foi esquecido.
- **O que está planejado e ainda não foi implementado mora em `14 - Planejamento/`.** Escopo adiado, próximo
  passo, pendência e decisão em aberto vão para lá, uma nota por assunto, linkando `[[...]]` para a
  funcionalidade/API/regra correspondente. Isso existe porque os relatórios de `output/` são por rodada e ficam fora do Git:
  sem essa pasta, o plano morre com a sessão. Quando algo dali for implementado, remova a nota (ou marque
  como concluída) no mesmo commit da implementação.
- **O vault é versionado junto com o código.** `knowledge/` vai no commit, não no `.gitignore` — só
  `knowledge/embeddings/chunks/` fica de fora, por ser derivado. O `/commit` deste projeto sincroniza o
  vault antes de montar a mensagem; o commit que leva código novo leva a memória junto.
