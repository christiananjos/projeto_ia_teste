# embeddings/

Os arquivos em `chunks/` e `metadata.json` são gerados automaticamente por
`.claude/scripts/knowledge-engine-build.cjs` a partir de `knowledge/vault/`;
`fontes/` sai do texto integral dos documentos originais em `knowledge/source/texto/`.
Para achar um detalhe, `grep -ril "termo" knowledge/embeddings/` e leia só os chunks que casarem.

Este pipeline **não calcula vetores reais** — isso exigiria uma API ou modelo de
embeddings de verdade, fora do escopo de um agente baseado em texto. Os chunks já
estão no tamanho e formato certos para alimentar qualquer pipeline de embeddings
(local ou via API) que você queira plugar depois; `vectors.bin` fica como extensão
futura, não como dado fabricado.