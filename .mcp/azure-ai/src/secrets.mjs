// Localiza archivos de secretos en .local-secrets/ o credentials/ subiendo por el árbol de directorios,
// igual que Mcp.Shared.SecretsLoader (.NET). No expone el contenido en logs.
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';

export function findSecretFilePath(fileName) {
  const roots = new Set([process.cwd(), path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1'))]);
  for (const start of roots) {
    let dir = start;
    for (;;) {
      for (const sub of ['.local-secrets', 'credentials']) {
        const candidate = path.join(dir, sub, fileName);
        if (existsSync(candidate)) return path.resolve(candidate);
      }
      const parent = path.dirname(dir);
      if (parent === dir) break;
      dir = parent;
    }
  }
  return null;
}

export function loadSecret(fileName) {
  const found = findSecretFilePath(fileName);
  if (!found) throw new Error(`No se encontró el archivo de secretos '${fileName}' en .local-secrets/ ni en credentials/.`);
  return JSON.parse(readFileSync(found, 'utf8'));
}
