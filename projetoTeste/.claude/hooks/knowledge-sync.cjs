#!/usr/bin/env node
// PostToolUse (Edit|Write|MultiEdit). Nunca falha a sessão.
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("child_process");

const ROOT = process.env.CLAUDE_PROJECT_DIR || process.cwd();
let input = {};
try { input = JSON.parse(fs.readFileSync(0, "utf-8")); } catch { process.exit(0); }
const file = input.tool_input && input.tool_input.file_path;
if (!file) process.exit(0);
const rel = path.relative(ROOT, path.resolve(ROOT, file)).split(path.sep).join("/");
if (rel.startsWith("..")) process.exit(0);

if (rel.startsWith("knowledge/vault/")) {
  try {
    execFileSync(process.execPath, [path.join(ROOT, ".claude", "scripts", "knowledge-engine-build.cjs")], { cwd: ROOT, stdio: "ignore", timeout: 50000 });
  } catch {}
  process.exit(0);
}

if (/^(knowledge|output|\.git)\//.test(rel)) process.exit(0);

process.stdout.write(JSON.stringify({
  hookSpecificOutput: {
    hookEventName: "PostToolUse",
    additionalContext: `Você alterou ${rel}. Se isso muda regra, endpoint, tela, fluxo ou decisão, atualize a nota correspondente em knowledge/vault/ neste mesmo turno, sem esperar o usuário pedir (o grafo se reconstrói sozinho).`,
  },
}));
