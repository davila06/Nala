import { readdir } from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

export async function loadTools(directory) {
  const entries = (await readdir(directory, { withFileTypes: true }))
    .filter((entry) => entry.isFile() && entry.name.endsWith(".mjs"))
    .map((entry) => entry.name)
    .sort();

  const tools = [];
  for (const entry of entries) {
    const module = await import(
      pathToFileURL(path.join(directory, entry)).href
    );
    if (module.default?.name && typeof module.default.handler === "function") {
      tools.push(module.default);
    }
  }
  return tools;
}
