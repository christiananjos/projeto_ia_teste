#!/usr/bin/env node
// Reconstrói knowledge/graph/ e knowledge/embeddings/ a partir de knowledge/vault/.
// Determinístico e sem dependências — nunca deve falhar o pipeline.

const fs = require("fs");
const path = require("path");

const ROOT = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const VAULT_DIR = path.join(ROOT, "knowledge", "vault");
const GRAPH_DIR = path.join(ROOT, "knowledge", "graph");
const EMBED_DIR = path.join(ROOT, "knowledge", "embeddings");
const CHUNKS_DIR = path.join(EMBED_DIR, "chunks");
const SOURCE_TEXT_DIR = path.join(ROOT, "knowledge", "source", "texto");
const SOURCE_CHUNKS_DIR = path.join(EMBED_DIR, "fontes");

function walkMarkdown(dir) {
  let results = [];
  let entries;
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return results;
  }
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results = results.concat(walkMarkdown(full));
    } else if (entry.isFile() && entry.name.toLowerCase().endsWith(".md")) {
      results.push(full);
    }
  }
  return results;
}

function slugify(s) {
  return s
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[^a-zA-Z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .toLowerCase();
}

function extractTitle(content, fallback) {
  const match = content.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : fallback;
}

function extractLinks(content) {
  const links = [];
  const re = /\[\[([^\]|#]+)(?:[|#][^\]]*)?\]\]/g;
  let m;
  while ((m = re.exec(content)) !== null) {
    links.push(m[1].trim());
  }
  return links;
}

function main() {
  if (!fs.existsSync(VAULT_DIR)) {
    console.log("knowledge/vault/ não existe ainda — nada para processar.");
    return;
  }

  const files = walkMarkdown(VAULT_DIR);
  if (files.length === 0) {
    console.log("knowledge/vault/ está vazio — nada para processar.");
    return;
  }

  const nodes = [];
  const titleToId = new Map();
  const docs = [];

  for (const file of files) {
    const relPath = path.relative(VAULT_DIR, file).split(path.sep).join("/");
    const id = relPath.replace(/\.md$/i, "");
    const content = fs.readFileSync(file, "utf-8");
    const domain = relPath.includes("/") ? relPath.split("/")[0] : "";
    const title = extractTitle(content, path.basename(file, ".md"));

    nodes.push({ id, title, path: relPath, domain });
    titleToId.set(title.trim().toLowerCase(), id);
    titleToId.set(path.basename(file, ".md").trim().toLowerCase(), id);
    docs.push({ id, relPath, content });
  }

  const edges = [];
  for (const doc of docs) {
    const links = extractLinks(doc.content);
    for (const link of links) {
      const resolvedId = titleToId.get(link.trim().toLowerCase());
      edges.push({
        source: doc.id,
        target: resolvedId || link,
        resolved: Boolean(resolvedId),
      });
    }
  }

  fs.mkdirSync(GRAPH_DIR, { recursive: true });
  fs.writeFileSync(path.join(GRAPH_DIR, "nodes.json"), JSON.stringify(nodes, null, 2), "utf-8");
  fs.writeFileSync(path.join(GRAPH_DIR, "edges.json"), JSON.stringify(edges, null, 2), "utf-8");

  // ---- Chunking para embeddings ----
  const metadata = writeChunks(docs, CHUNKS_DIR, "knowledge/vault", "chunks", "vault");

  // Texto integral das fontes originais (knowledge/source/texto/), gravado pelo
  // 01-analyst-sdd ao extrair cada PDF/DOCX/e-mail.
  const sourceDocs = walkMarkdown(SOURCE_TEXT_DIR).map((file) => {
    const relPath = path.relative(SOURCE_TEXT_DIR, file).split(path.sep).join("/");
    return { id: relPath.replace(/\.md$/i, ""), relPath, content: fs.readFileSync(file, "utf-8") };
  });
  metadata.push(...writeChunks(sourceDocs, SOURCE_CHUNKS_DIR, "knowledge/source/texto", "fontes", "fonte"));

  fs.writeFileSync(path.join(EMBED_DIR, "metadata.json"), JSON.stringify(metadata, null, 2), "utf-8");

  const readmePath = path.join(EMBED_DIR, "README.md");
  if (!fs.existsSync(readmePath)) {
    fs.writeFileSync(
      readmePath,
      [
        "# embeddings/",
        "",
        "Os arquivos em `chunks/` e `metadata.json` são gerados automaticamente por",
        "`.claude/scripts/knowledge-engine-build.cjs` a partir de `knowledge/vault/`;",
        "`fontes/` sai do texto integral dos documentos originais em `knowledge/source/texto/`.",
        "Para achar um detalhe, `grep -ril \"termo\" knowledge/embeddings/` e leia só os chunks que casarem.",
        "",
        "Este pipeline **não calcula vetores reais** — isso exigiria uma API ou modelo de",
        "embeddings de verdade, fora do escopo de um agente baseado em texto. Os chunks já",
        "estão no tamanho e formato certos para alimentar qualquer pipeline de embeddings",
        "(local ou via API) que você queira plugar depois; `vectors.bin` fica como extensão",
        "futura, não como dado fabricado.",
      ].join("\n"),
      "utf-8"
    );
  }

  console.log(
    `Knowledge graph: ${nodes.length} nós, ${edges.length} links (${edges.filter((e) => e.resolved).length} resolvidos). ` +
      `Embeddings: ${metadata.length} chunks a partir de ${docs.length} documentos do vault e ${sourceDocs.length} fontes.`
  );
}

function writeChunks(docs, dir, sourcePrefix, dirName, tipo) {
  fs.mkdirSync(dir, { recursive: true });
  // limpa chunks antigos para não acumular lixo de rodadas anteriores
  for (const f of fs.readdirSync(dir)) {
    try {
      fs.unlinkSync(path.join(dir, f));
    } catch {
      // ignora
    }
  }

  const metadata = [];
  for (const doc of docs) {
    const parts = doc.content.split(/\n(?=##\s+)/g).filter((p) => p.trim().length > 0);
    const chunks = parts.length > 1 ? parts : chunkByLength(doc.content, 800);
    const baseSlug = slugify(doc.id) || "doc";
    chunks.forEach((chunkText, i) => {
      const chunkFileName = `${baseSlug}--${i + 1}.md`;
      const headingMatch = chunkText.match(/^##?\s+(.+)$/m);
      fs.writeFileSync(
        path.join(dir, chunkFileName),
        `<!-- fonte: ${sourcePrefix}/${doc.relPath} -->\n\n${chunkText.trim()}\n`,
        "utf-8"
      );
      metadata.push({
        chunkId: `${baseSlug}--${i + 1}`,
        tipo,
        sourceDoc: `${sourcePrefix}/${doc.relPath}`,
        heading: headingMatch ? headingMatch[1].trim() : null,
        order: i + 1,
        charCount: chunkText.length,
        file: `knowledge/embeddings/${dirName}/${chunkFileName}`,
      });
    });
  }
  return metadata;
}

function chunkByLength(text, maxLen) {
  const paragraphs = text.split(/\n{2,}/);
  const chunks = [];
  let current = "";
  for (const p of paragraphs) {
    if ((current + "\n\n" + p).length > maxLen && current.length > 0) {
      chunks.push(current);
      current = p;
    } else {
      current = current ? current + "\n\n" + p : p;
    }
  }
  if (current) chunks.push(current);
  return chunks.length > 0 ? chunks : [text];
}

try {
  main();
} catch (err) {
  console.error("knowledge-engine-build falhou (não bloqueante):", err.message);
}
