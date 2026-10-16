#!/usr/bin/env node
// Hook "Stop" — gera/atualiza output/token-report.md com o uso de tokens do pipeline /inicia-orquestracao.
// Nunca deve falhar o pipeline: qualquer erro é engolido e, na pior hipótese, o script simplesmente não escreve nada.

const fs = require("fs");
const path = require("path");

function readStdinJson() {
  const raw = fs.readFileSync(0, "utf-8");
  return JSON.parse(raw);
}

function fmt(n) {
  if (typeof n !== "number" || Number.isNaN(n)) return "n/d";
  return n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".");
}

// Formata valor monetário com separador de milhar/decimal conforme a convenção de cada moeda
// (US$ 1,234.56 — ponto decimal; R$ 1.234,56 — vírgula decimal).
function fmtMoney(n, symbol, decimalSep, thousandSep) {
  if (typeof n !== "number" || Number.isNaN(n)) return "n/d";
  const fixed = n.toFixed(2);
  const [intPart, decPart] = fixed.split(".");
  const withThousands = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, thousandSep);
  return `${symbol} ${withThousands}${decimalSep}${decPart}`;
}
const fmtUsd = (n) => fmtMoney(n, "US$", ".", ",");
const fmtBrl = (n) => fmtMoney(n, "R$", ",", ".");

// Data/hora local (não UTC) — evita o relatório mostrar um horário 3h à frente para quem está
// no fuso do Brasil (UTC-3). Usa os getters locais do Date, então segue o fuso da própria máquina.
function formatLocal(d) {
  const pad = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

// ---- Preços por token (US$ por milhão de tokens) ----
// Fonte: tabela oficial da API da Anthropic (https://platform.claude.com/docs/en/about-claude/pricing),
// conferida em 2026-09-10. O custo é calculado pelo modelo que REALMENTE respondeu cada mensagem
// (campo `model` do transcript) — não pelo que está no frontmatter do agente —, então o relatório
// continua certo seja qual for o modelo de cada subagente (Opus, Sonnet, Haiku, Fable...) e mesmo
// que ele mude de uma rodada para outra. Isto é uma ESTIMATIVA no valor de tarifa da API — quem usa
// plano Pro/Max (assinatura) não é cobrado por token, então o valor aqui não é uma fatura real.
//
// in/out: preço base de entrada/saída. cacheRead: multiplicador da leitura de cache sobre o preço de
// entrada (padrão 0.1x). fast: preço do fast mode (só Opus 5.5, Opus 5 e Opus 4.8).
const PRICING = {
  "claude-fable-5-1": { in: 10, out: 50, cacheRead: 0.025 },
  "claude-mythos-5-1": { in: 10, out: 50, cacheRead: 0.025 },
  "claude-fable-5": { in: 10, out: 50 },
  "claude-mythos-5": { in: 10, out: 50 },
  "claude-opus-5-5": { in: 4, out: 20, cacheRead: 0.05, fast: { in: 8, out: 40 } },
  "claude-opus-5": { in: 5, out: 25, fast: { in: 10, out: 50 } },
  "claude-opus-4-8": { in: 5, out: 25, fast: { in: 10, out: 50 } },
  "claude-opus-4-7": { in: 5, out: 25 },
  "claude-opus-4-6": { in: 5, out: 25 },
  "claude-opus-4-5": { in: 5, out: 25 },
  "claude-opus-4-1": { in: 15, out: 75 },
  "claude-opus-4-0": { in: 15, out: 75 },
  "claude-opus-4": { in: 15, out: 75 }, // "claude-opus-4-20250514" depois de tirar a data
  "claude-sonnet-5": { in: 2, out: 10 },
  "claude-sonnet-4-6": { in: 3, out: 15 },
  "claude-sonnet-4-5": { in: 3, out: 15 },
  "claude-sonnet-4-0": { in: 3, out: 15 },
  "claude-sonnet-4": { in: 3, out: 15 }, // "claude-sonnet-4-20250514" depois de tirar a data
  "claude-haiku-4-5": { in: 1, out: 5 },
  "claude-3-5-haiku": { in: 0.8, out: 4 },
};

// Modelo que ainda não está na tabela (ex.: lançado depois de 2026-09-10) é calculado com o preço
// do modelo mais recente da mesma família — e o relatório avisa que aquele valor é aproximado.
const FAMILY_FALLBACK = [
  { family: /fable|mythos/, ref: "claude-fable-5-1" },
  { family: /opus/, ref: "claude-opus-5-5" },
  { family: /sonnet/, ref: "claude-sonnet-5" },
  { family: /haiku/, ref: "claude-haiku-4-5" },
];
const DEFAULT_REF = "claude-opus-5-5"; // último recurso: nem a família do modelo foi reconhecida

const WEB_SEARCH_USD_PER_REQUEST = 10 / 1000; // web search: US$ 10 por 1.000 buscas, além dos tokens
const US_ONLY_INFERENCE_MULTIPLIER = 1.1; // inference_geo "us" (modelos 4.6+): 1.1x em todos os tokens

// Reduz qualquer formato de id ao id base da API da Anthropic, pra achar o preço certo:
//   "us.anthropic.claude-opus-5" (Amazon Bedrock)          -> "claude-opus-5"
//   "claude-haiku-4-5@20251001" (Google Cloud)              -> "claude-haiku-4-5"
//   "claude-haiku-4-5-20251001" (snapshot com data)         -> "claude-haiku-4-5"
//   "anthropic.claude-3-5-haiku-20241022-v1:0" (Bedrock)    -> "claude-3-5-haiku"
//   "claude-opus-4-6[1m]"                                   -> "claude-opus-4-6"
function normalizeModelId(raw) {
  if (typeof raw !== "string") return "";
  let id = raw.trim().toLowerCase();
  const start = id.indexOf("claude-");
  if (start === -1) return id;
  id = id.slice(start);
  id = id.replace(/\[.*?\]/g, "");
  id = id.split("@")[0];
  id = id.replace(/:.*$/, "");
  id = id.replace(/-v\d+$/, "");
  id = id.replace(/-\d{8}$/, "");
  return id;
}

// Retorna { price, ref, exact }: `ref` é o id cujo preço foi usado e `exact` diz se o modelo estava
// na tabela (false = preço aproximado pela família). Nunca retorna vazio — sempre há um preço.
function priceForModel(modelId) {
  const id = normalizeModelId(modelId);
  if (PRICING[id]) return { price: PRICING[id], ref: id, exact: true };
  const fb = FAMILY_FALLBACK.find((f) => f.family.test(id));
  const ref = fb ? fb.ref : DEFAULT_REF;
  return { price: PRICING[ref], ref, exact: false };
}

// Cotação fixa USD -> BRL. O hook roda 100% offline (sem chamada de rede), então a cotação não
// se atualiza sozinha — ajuste esta constante manualmente se quiser mais precisão.
// Definida em 2026-09-01.
const USD_TO_BRL = 5.3;

// Campos numéricos de um bucket de uso (um bucket = um modelo + modo fast/padrão + região).
const BUCKET_FIELDS = ["input_tokens", "output_tokens", "cache_5m", "cache_1h", "cache_read", "web_search"];

function emptyBucket(model, fast, usOnly) {
  return { model, fast, usOnly, input_tokens: 0, output_tokens: 0, cache_5m: 0, cache_1h: 0, cache_read: 0, web_search: 0 };
}

function bucketKey(model, fast, usOnly) {
  return `${model}|${fast ? "fast" : "padrao"}|${usOnly ? "us" : "global"}`;
}

function hasUsage(b) {
  return BUCKET_FIELDS.some((f) => b[f] > 0);
}

function bucketTokens(b) {
  return b.input_tokens + b.output_tokens + b.cache_5m + b.cache_1h + b.cache_read;
}

// Nome legível do modelo de um bucket, pras tabelas "Por agente" e "Por modelo".
function bucketLabel(b) {
  const tags = [];
  if (b.fast) tags.push("fast mode");
  if (b.usOnly) tags.push("só EUA");
  return tags.length ? `${b.model} (${tags.join(", ")})` : b.model;
}

// Custo estimado (USD) de um bucket de uso. Multiplicadores de cache sobre o preço de entrada:
// escrita 5min = 1.25x, escrita 1h = 2x, leitura = 0.1x (0.025x no Fable 5.1 / Mythos 5.1). Fast mode
// troca o preço base; inferência só nos EUA multiplica os tokens por 1.1x; web search é cobrada à
// parte, por busca.
function costUsdForBucket(b) {
  const { price } = priceForModel(b.model);
  const base = b.fast && price.fast ? price.fast : price;
  const perTokIn = base.in / 1e6;
  const perTokOut = base.out / 1e6;
  const cacheReadMult = typeof price.cacheRead === "number" ? price.cacheRead : 0.1;
  let usd =
    b.input_tokens * perTokIn +
    b.output_tokens * perTokOut +
    b.cache_5m * perTokIn * 1.25 +
    b.cache_1h * perTokIn * 2 +
    b.cache_read * perTokIn * cacheReadMult;
  if (b.usOnly) usd *= US_ONLY_INFERENCE_MULTIPLIER;
  return usd + b.web_search * WEB_SEARCH_USD_PER_REQUEST;
}

// Soma o custo estimado de um Map<chave, bucket>. Também devolve o total de buscas na web e os
// modelos que não estão na tabela de preços (calculados com o preço aproximado da família).
function costFromByModel(byModel) {
  let usd = 0;
  let webSearches = 0;
  const approx = new Map(); // id original do modelo -> id cujo preço foi usado
  for (const b of byModel.values()) {
    if (!hasUsage(b)) continue;
    usd += costUsdForBucket(b);
    webSearches += b.web_search;
    const p = priceForModel(b.model);
    if (!p.exact) approx.set(b.model, p.ref);
  }
  return { usd, webSearches, approx };
}

// Lista os modelos usados por um agente, ex.: "claude-opus-5, claude-haiku-4-5-20251001".
function modelsOf(usage) {
  const labels = [...new Set([...usage.byModel.values()].filter(hasUsage).map(bucketLabel))];
  return labels.length ? labels.join(", ") : "—";
}

function emptyUsage() {
  return {
    totals: { input_tokens: 0, output_tokens: 0, cache_creation_input_tokens: 0, cache_read_input_tokens: 0 },
    byModel: new Map(),
  };
}

// Combina um usage { totals, byModel } dentro de um acumulador do mesmo formato.
function mergeUsage(acc, src) {
  if (!src) return;
  for (const k of Object.keys(acc.totals)) acc.totals[k] += src.totals[k] || 0;
  for (const [key, u] of src.byModel.entries()) {
    if (!acc.byModel.has(key)) acc.byModel.set(key, emptyBucket(u.model, u.fast, u.usOnly));
    const m = acc.byModel.get(key);
    for (const f of BUCKET_FIELDS) m[f] += u[f] || 0;
  }
}

// Soma o uso de todas as linhas "assistant" de um transcript .jsonl, opcionalmente só considerando
// mensagens com timestamp > sinceMs. Retorna totais gerais e uso detalhado por modelo (necessário
// pra estimar custo, já que cada modelo tem preço diferente).
//
// O Claude Code grava uma mesma resposta (mesmo message.id) em várias linhas — uma por bloco de
// conteúdo — e as primeiras podem trazer output_tokens parcial (ex.: 3 na 1ª linha e 188 na última).
// Por isso cada message.id conta uma vez só, com o MAIOR valor visto de cada campo.
function sumTranscriptUsage(filePath, sinceMs) {
  let content;
  try {
    content = fs.readFileSync(filePath, "utf-8");
  } catch {
    return null; // arquivo indisponível
  }
  const perMessage = new Map(); // message.id -> uso consolidado daquela resposta
  for (const line of content.split("\n")) {
    if (!line.trim()) continue;
    let obj;
    try {
      obj = JSON.parse(line);
    } catch {
      continue; // linha corrompida/truncada — ignora e segue
    }
    if (obj.type !== "assistant") continue;
    if (sinceMs && obj.timestamp) {
      const ts = Date.parse(obj.timestamp);
      if (!Number.isNaN(ts) && ts <= sinceMs) continue;
    }
    const msg = obj.message || {};
    const usage = msg.usage;
    if (!usage || !msg.id) continue;

    const cc = usage.cache_creation;
    const hasTtlBreakdown = cc && (typeof cc.ephemeral_5m_input_tokens === "number" || typeof cc.ephemeral_1h_input_tokens === "number");
    const serverTools = usage.server_tool_use || {};
    const cur = {
      model: typeof msg.model === "string" ? msg.model : "desconhecido",
      fast: usage.speed === "fast",
      usOnly: usage.inference_geo === "us",
      input_tokens: usage.input_tokens || 0,
      output_tokens: usage.output_tokens || 0,
      cache_creation_input_tokens: usage.cache_creation_input_tokens || 0,
      cache_read: usage.cache_read_input_tokens || 0,
      // Transcript antigo, sem detalhamento por janela de cache — assume a janela padrão (5min).
      cache_5m: hasTtlBreakdown ? cc.ephemeral_5m_input_tokens || 0 : usage.cache_creation_input_tokens || 0,
      cache_1h: hasTtlBreakdown ? cc.ephemeral_1h_input_tokens || 0 : 0,
      web_search: serverTools.web_search_requests || 0,
    };
    const prev = perMessage.get(msg.id);
    if (!prev) {
      perMessage.set(msg.id, cur);
      continue;
    }
    for (const k of Object.keys(cur)) {
      if (typeof cur[k] === "number") prev[k] = Math.max(prev[k], cur[k]);
    }
    prev.fast = prev.fast || cur.fast;
    prev.usOnly = prev.usOnly || cur.usOnly;
  }

  const totals = { input_tokens: 0, output_tokens: 0, cache_creation_input_tokens: 0, cache_read_input_tokens: 0 };
  const byModel = new Map();
  for (const m of perMessage.values()) {
    totals.input_tokens += m.input_tokens;
    totals.output_tokens += m.output_tokens;
    totals.cache_creation_input_tokens += m.cache_creation_input_tokens;
    totals.cache_read_input_tokens += m.cache_read;
    const key = bucketKey(m.model, m.fast, m.usOnly);
    if (!byModel.has(key)) byModel.set(key, emptyBucket(m.model, m.fast, m.usOnly));
    const b = byModel.get(key);
    for (const f of BUCKET_FIELDS) b[f] += m[f];
  }
  return { totals, byModel };
}

function totalOf(u) {
  if (!u) return 0;
  return (u.input_tokens || 0) + (u.output_tokens || 0) + (u.cache_creation_input_tokens || 0) + (u.cache_read_input_tokens || 0);
}

function main() {
  let payload;
  try {
    payload = readStdinJson();
  } catch {
    return; // sem payload legível, não há o que fazer
  }

  const projectDir = process.env.CLAUDE_PROJECT_DIR || payload.cwd || process.cwd();
  const outputDir = path.join(projectDir, "output");
  const hooksDir = path.join(projectDir, ".claude", "hooks");
  const statePath = path.join(hooksDir, ".token-report-state.json");
  const reportPath = path.join(outputDir, "token-report.md");

  let outputFiles = [];
  try {
    outputFiles = fs
      .readdirSync(outputDir)
      .filter((f) => f.toLowerCase().endsWith(".md") && f !== "token-report.md")
      .map((f) => {
        try {
          return { name: f, mtimeMs: fs.statSync(path.join(outputDir, f)).mtimeMs };
        } catch {
          return null;
        }
      })
      .filter(Boolean);
  } catch {
    return; // sem pasta output/, não houve pipeline ainda
  }
  if (outputFiles.length === 0) return;

  const maxOutputMtime = Math.max(...outputFiles.map((f) => f.mtimeMs));

  let state = {};
  try {
    state = JSON.parse(fs.readFileSync(statePath, "utf-8"));
  } catch {
    state = {};
  }

  let checkpoint = state.lastRunTimestampMs;
  if (typeof checkpoint !== "number") {
    // Primeira vez que o hook roda nesta sessão/projeto: usa o início do transcript
    // principal como baseline, pra não perder a primeira rodada do pipeline.
    checkpoint = 0;
    try {
      const firstLine = fs.readFileSync(payload.transcript_path, "utf-8").split("\n").find((l) => l.trim());
      if (firstLine) {
        const first = JSON.parse(firstLine);
        const ts = Date.parse(first.timestamp);
        if (!Number.isNaN(ts)) checkpoint = ts;
      }
    } catch {
      // segue com checkpoint = 0
    }
  }

  // Nada mudou em output/ desde a última rodada processada -> este Stop não é do /inicia-orquestracao, ignora.
  if (maxOutputMtime <= checkpoint) return;

  // ---- Uso do agente principal (transcript da conversa) ----
  let mainUsage = null;
  try {
    mainUsage = sumTranscriptUsage(payload.transcript_path, checkpoint);
  } catch {
    mainUsage = null;
  }

  // ---- Uso dos subagentes desta rodada ----
  const subagentsDir = path.join(path.dirname(payload.transcript_path), payload.session_id, "subagents");
  const perAgent = []; // { label, usage, ok }
  let subagentsOk = true;
  try {
    const files = fs.readdirSync(subagentsDir);
    const jsonlFiles = files.filter((f) => f.startsWith("agent-") && f.endsWith(".jsonl"));
    for (const f of jsonlFiles) {
      const full = path.join(subagentsDir, f);
      let mtimeMs = 0;
      try {
        mtimeMs = fs.statSync(full).mtimeMs;
      } catch {
        continue;
      }
      if (mtimeMs <= checkpoint) continue; // agente de uma rodada anterior, não desta

      const id = f.slice("agent-".length, -".jsonl".length);
      let label = id;
      try {
        const meta = JSON.parse(fs.readFileSync(path.join(subagentsDir, `agent-${id}.meta.json`), "utf-8"));
        label = meta.agentType || meta.description || id;
      } catch {
        // sem meta.json, usa o id mesmo
      }

      const usage = sumTranscriptUsage(full, 0);
      perAgent.push({ label, usage, ok: usage !== null });
      if (usage === null) subagentsOk = false;
    }
  } catch {
    subagentsOk = false; // pasta subagents/ não encontrada/ilegível
  }

  // Agrupa por label (caso o mesmo agente tenha rodado mais de uma vez nesta rodada)
  const grouped = new Map();
  for (const { label, usage } of perAgent) {
    if (!grouped.has(label)) grouped.set(label, emptyUsage());
    mergeUsage(grouped.get(label), usage);
  }

  const grandUsage = emptyUsage();
  mergeUsage(grandUsage, mainUsage);
  for (const u of grouped.values()) mergeUsage(grandUsage, u);
  const grandTotal = totalOf(grandUsage.totals);
  const grandCost = costFromByModel(grandUsage.byModel);
  const grandCostBrl = grandCost.usd * USD_TO_BRL;

  // ---- Acumulado: soma de TODAS as rodadas já registradas por este relatório ----
  // Guardado no arquivo de estado (não recalculado a partir do histórico visível do relatório,
  // que mantém só as últimas ~30 rodadas) — assim o total de baixo nunca perde rodadas antigas
  // mesmo depois que elas saem da tabela de histórico.
  let cumulative = state.cumulative;
  if (!cumulative || typeof cumulative !== "object" || !cumulative.totals) {
    // Primeira rodada com esta funcionalidade neste projeto — começa a acumular a partir de agora.
    // Não dá pra reconstruir com precisão o detalhamento input/output/cache de rodadas anteriores
    // a esta versão do hook, já que o histórico visível guarda só o total e o custo por rodada.
    cumulative = {
      since: new Date().toISOString(),
      totals: { input_tokens: 0, output_tokens: 0, cache_creation_input_tokens: 0, cache_read_input_tokens: 0 },
      costUsd: 0,
      runs: 0,
    };
  }
  for (const k of Object.keys(cumulative.totals)) cumulative.totals[k] += grandUsage.totals[k] || 0;
  cumulative.costUsd += grandCost.usd;
  cumulative.runs += 1;
  const cumulativeTotal = totalOf(cumulative.totals);
  const cumulativeCostBrl = cumulative.costUsd * USD_TO_BRL;
  const cumulativeSinceStamp = formatLocal(new Date(cumulative.since));

  // ---- Monta o relatório ----
  const now = new Date();
  const stamp = formatLocal(now);

  const lines = [];
  lines.push("# Relatório de Uso de Tokens");
  lines.push("");
  lines.push("_Gerado e atualizado automaticamente pelo hook `Stop` após cada execução completa do pipeline `/inicia-orquestracao`. Números vêm diretamente dos transcripts da sessão — não são estimados pelo modelo. Horário local da máquina._");
  lines.push("");
  lines.push(`## Última rodada — ${stamp}`);
  lines.push("");
  lines.push("| Métrica | Tokens |");
  lines.push("|---|---|");
  lines.push(`| Entrada (input) | ${fmt(grandUsage.totals.input_tokens)} |`);
  lines.push(`| Saída (output) | ${fmt(grandUsage.totals.output_tokens)} |`);
  lines.push(`| Cache — criação | ${fmt(grandUsage.totals.cache_creation_input_tokens)} |`);
  lines.push(`| Cache — leitura | ${fmt(grandUsage.totals.cache_read_input_tokens)} |`);
  lines.push(`| **Total** | **${fmt(grandTotal)}** |`);
  lines.push("");
  lines.push("| Custo estimado | Valor |");
  lines.push("|---|---|");
  lines.push(`| Dólar (USD) | ${fmtUsd(grandCost.usd)} |`);
  lines.push(`| Real (BRL) | ${fmtBrl(grandCostBrl)} |`);
  lines.push("");
  lines.push(
    `_Estimativa a preço de tarifa da API da Anthropic (US$/milhão de tokens, calculada pelo modelo que respondeu cada mensagem — inclui cache, fast mode e web search; cotação fixa US$ 1 = R$ ${USD_TO_BRL.toFixed(2).replace(".", ",")}) — não é uma fatura real, e não reflete plano de assinatura (Pro/Max), descontos nem a tabela própria do Amazon Bedrock/Google Cloud.${grandCost.webSearches > 0 ? ` Inclui ${fmt(grandCost.webSearches)} busca(s) na web (US$ 10 por 1.000).` : ""}_`
  );
  lines.push("");
  const mainOk = mainUsage !== null;
  const warnings = [];
  if (!mainOk || !subagentsOk) {
    const parts = [];
    if (!mainOk) parts.push("uso do agente principal");
    if (!subagentsOk) parts.push("uso de um ou mais subagentes");
    warnings.push(`⚠️ Não foi possível ler o ${parts.join(" e o ")} desta rodada (arquivo indisponível ou formato mudou). O total acima pode estar subestimado.`);
  }
  if (grandCost.approx.size > 0) {
    const list = [...grandCost.approx.entries()].map(([model, ref]) => `${model} (calculado com o preço de ${ref})`).join(", ");
    warnings.push(`⚠️ Modelo(s) sem preço próprio na tabela: ${list}. O custo desses entra no total, mas é aproximado — confira https://platform.claude.com/docs/en/about-claude/pricing e atualize \`PRICING\` em \`.claude/hooks/generate-token-report.cjs\`.`);
  }
  for (const w of warnings) {
    lines.push(`> ${w}`);
    lines.push("");
  }
  lines.push("### Por agente");
  lines.push("");
  lines.push("| Agente | Modelo(s) | Tokens | Custo (USD) |");
  lines.push("|---|---|---|---|");
  const mainCost = mainUsage ? costFromByModel(mainUsage.byModel) : null;
  lines.push(`| inicia-orquestracao (agente principal) | ${mainUsage ? modelsOf(mainUsage) : "n/d"} | ${mainUsage ? fmt(totalOf(mainUsage.totals)) : "n/d"} | ${mainCost ? fmtUsd(mainCost.usd) : "n/d"} |`);
  const sortedAgents = [...grouped.entries()].sort((a, b) => totalOf(b[1].totals) - totalOf(a[1].totals));
  for (const [label, usage] of sortedAgents) {
    const cost = costFromByModel(usage.byModel);
    lines.push(`| ${label} | ${modelsOf(usage)} | ${fmt(totalOf(usage.totals))} | ${fmtUsd(cost.usd)} |`);
  }
  lines.push("");

  // Mesmo uso da rodada, agrupado por modelo — mostra quanto cada modelo pesou no custo.
  lines.push("### Por modelo");
  lines.push("");
  lines.push("| Modelo | Tokens | Custo (USD) |");
  lines.push("|---|---|---|");
  const modelRows = [...grandUsage.byModel.values()]
    .filter(hasUsage)
    .map((b) => ({ label: bucketLabel(b), tokens: bucketTokens(b), usd: costUsdForBucket(b) }))
    .sort((a, b) => b.usd - a.usd);
  for (const r of modelRows) lines.push(`| ${r.label} | ${fmt(r.tokens)} | ${fmtUsd(r.usd)} |`);
  if (modelRows.length === 0) lines.push("| — | 0 | US$ 0.00 |");
  lines.push("");

  // ---- Histórico: preserva linhas já existentes no relatório anterior ----
  let historyRows = [];
  try {
    const prev = fs.readFileSync(reportPath, "utf-8");
    const marker = "| Data | Total de tokens | Custo (USD) |";
    const idx = prev.indexOf(marker);
    if (idx !== -1) {
      const after = prev.slice(idx + marker.length).split("\n");
      if (after[0] === "") after.shift(); // artefato do slice: o 1º item é sempre "" (cortou em cima do \n do cabeçalho)
      // Só a tabela imediatamente após o cabeçalho — para no primeiro "|---" (linha separadora,
      // já pulada) e no primeiro fim de bloco (linha em branco), sem varrer o resto do arquivo.
      // Isso importa desde que a seção "Total acumulado" passou a existir depois desta tabela:
      // sem esse limite, linhas de outras tabelas mais abaixo entrariam como se fossem histórico.
      for (const l of after) {
        const trimmed = l.trim();
        if (trimmed.startsWith("|---")) continue;
        if (!trimmed.startsWith("|")) break;
        historyRows.push(l);
        if (historyRows.length >= 29) break; // mantém só as últimas rodadas junto com a nova
      }
    }
  } catch {
    historyRows = [];
  }

  lines.push("## Histórico de rodadas");
  lines.push("");
  lines.push("| Data | Total de tokens | Custo (USD) |");
  lines.push("|---|---|---|");
  lines.push(`| ${stamp} | ${fmt(grandTotal)} | ${fmtUsd(grandCost.usd)} |`);
  for (const row of historyRows) lines.push(row);
  lines.push("");

  lines.push("## Total acumulado (todas as rodadas)");
  lines.push("");
  lines.push(
    `_Soma de todas as ${fmt(cumulative.runs)} rodada(s) que este relatório já registrou, desde ${cumulativeSinceStamp}. Recalculado automaticamente a cada nova rodada — nunca perde rodadas antigas, mesmo as que já saíram da tabela de histórico acima._`
  );
  lines.push("");
  lines.push("| Métrica | Tokens |");
  lines.push("|---|---|");
  lines.push(`| Entrada (input) | ${fmt(cumulative.totals.input_tokens)} |`);
  lines.push(`| Saída (output) | ${fmt(cumulative.totals.output_tokens)} |`);
  lines.push(`| Cache — criação | ${fmt(cumulative.totals.cache_creation_input_tokens)} |`);
  lines.push(`| Cache — leitura | ${fmt(cumulative.totals.cache_read_input_tokens)} |`);
  lines.push(`| **Total** | **${fmt(cumulativeTotal)}** |`);
  lines.push("");
  lines.push("| Custo estimado acumulado | Valor |");
  lines.push("|---|---|");
  lines.push(`| Dólar (USD) | ${fmtUsd(cumulative.costUsd)} |`);
  lines.push(`| Real (BRL) | ${fmtBrl(cumulativeCostBrl)} |`);
  lines.push("");

  try {
    fs.mkdirSync(outputDir, { recursive: true });
    fs.writeFileSync(reportPath, lines.join("\n"), "utf-8");
  } catch {
    return; // não conseguiu escrever o relatório — não falha o hook por isso
  }

  try {
    fs.mkdirSync(hooksDir, { recursive: true });
    fs.writeFileSync(statePath, JSON.stringify({ lastRunTimestampMs: maxOutputMtime, cumulative }), "utf-8");
  } catch {
    // se não salvar o checkpoint, a próxima rodada recalcula um período maior — não é grave
  }

  // Rodada concluída (o /inicia-orquestracao grava o marcador depois do push final): os relatórios
  // intermediários já cumpriram o papel e o que importa está em knowledge/ — sobra só o token-report.md.
  // Rodada que parou num gate reprovado não tem marcador, então os relatórios ficam para explicar o motivo.
  if (fs.existsSync(path.join(outputDir, ".rodada-concluida"))) {
    try {
      require(path.join(projectDir, ".claude", "scripts", "limpar-output.cjs")).limparOutput(projectDir);
    } catch {
      // sem o script (projeto antigo) — os relatórios ficam e a próxima rodada limpa
    }
  }
}

try {
  main();
} catch {
  // hook nunca deve derrubar o pipeline
}
