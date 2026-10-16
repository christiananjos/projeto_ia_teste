#!/usr/bin/env node
// Apaga os relatórios intermediários de output/, mantendo token-report.md.
// Não mexe em subpastas (backups do /atualizar-versao) nem nas cópias COMECE-AQUI.removido-*.
// Nunca deve falhar o pipeline.

const fs = require("fs");
const path = require("path");

function limparOutput(projectDir) {
  const outputDir = path.join(projectDir, "output");
  let entries = [];
  try {
    entries = fs.readdirSync(outputDir, { withFileTypes: true });
  } catch {
    return []; // sem output/, nada a limpar
  }
  const removidos = [];
  for (const e of entries) {
    if (!e.isFile()) continue;
    if (e.name === "token-report.md" || e.name.startsWith("COMECE-AQUI.removido-")) continue;
    try {
      fs.unlinkSync(path.join(outputDir, e.name));
      removidos.push(e.name);
    } catch {
      // arquivo preso/sem permissão — segue com os outros
    }
  }
  return removidos;
}

module.exports = { limparOutput };

if (require.main === module) {
  const removidos = limparOutput(process.env.CLAUDE_PROJECT_DIR || process.cwd());
  console.log(removidos.length ? `output/ limpo: ${removidos.join(", ")}` : "output/ já estava limpo");
}
