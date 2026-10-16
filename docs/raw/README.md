# 📥 Pasta de Documentação Bruta (docs/raw/)

Coloque aqui **toda** a documentação original do projeto, em qualquer formato:

- Word (`.docx`), PDF, Markdown, texto solto
- Planilhas (`.xlsx`, `.csv`)
- Imagens e diagramas (`.png`, `.jpg`, prints de wireframe, diagramas exportados)
- Atas de reunião, especificações, conversas com o cliente

O formato não importa. O objetivo é reunir tudo o que descreve o projeto num único lugar.

## O que acontece com esses arquivos

Ao rodar `/inicia-orquestracao`, se esta pasta tiver pelo menos um arquivo, a **primeira etapa do pipeline** (`01-analyst-sdd`)
roda automaticamente, antes de qualquer outro agente:

1. Lê e interpreta todos os documentos
2. Consolida e organiza o conteúdo em `knowledge/vault/` — uma base de conhecimento em Markdown,
   compatível com Obsidian, com links internos entre os documentos
3. Cria glossário, índice, grafo de relacionamentos e um contexto resumido por área (backend, frontend, QA, etc.)
4. Detecta lacunas e inconsistências entre os documentos recebidos

O resultado vira a **fonte única de verdade** consultada por todos os agentes do pipeline
(Analyst, Architect, Specialist, Reviewer, Test Engineer, Security Scan) durante todo o desenvolvimento.

Se esta pasta estiver **vazia**, o pipeline simplesmente pula a consolidação e segue direto a partir de `docs/SPEC.md`,
como no fluxo original.

## Formatos com limitações

- `.docx`, `.xlsx`, `.pptx`: o agente tenta converter o conteúdo; se não conseguir no ambiente atual,
  o arquivo fica listado como pendência no relatório do Analyst (`output/1-analyst.md`) (prefira exportar como PDF ou Markdown)
- Áudio/vídeo (reuniões gravadas): não são transcritos automaticamente — se possível, forneça a transcrição em texto

Os arquivos originais **nunca são alterados**. Eles ficam preservados também em `knowledge/source/`,
como referência permanente.
