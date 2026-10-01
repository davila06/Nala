import { createHash } from "node:crypto";
import { readFile, readdir, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { parse } from "parse5";

export function createScriptHashes(html) {
  const document = parse(html);
  const hashes = new Set();

  function visit(node) {
    if (
      node.tagName === "script" &&
      !node.attrs?.some((attribute) => attribute.name === "src")
    ) {
      const script = (node.childNodes ?? [])
        .map((child) => child.value ?? "")
        .join("");
      if (script.trim()) {
        hashes.add(createHash("sha256").update(script).digest("base64"));
      }
    }

    for (const child of node.childNodes ?? []) visit(child);
  }

  visit(document);
  return [...hashes].sort();
}

async function listHtmlFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries.sort((left, right) =>
    left.name.localeCompare(right.name),
  )) {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await listHtmlFiles(path)));
    } else if (entry.isFile() && entry.name.endsWith(".html")) {
      files.push(path);
    }
  }

  return files;
}

export async function generateStaticCsp(outputDirectory = resolve("out")) {
  const htmlFiles = await listHtmlFiles(outputDirectory);
  const hashes = new Set();

  for (const file of htmlFiles) {
    for (const hash of createScriptHashes(await readFile(file, "utf8")))
      hashes.add(hash);
  }

  if (htmlFiles.length === 0 || hashes.size === 0) {
    throw new Error(
      "Static export must contain HTML pages with inline scripts before generating CSP.",
    );
  }

  const configPath = resolve(outputDirectory, "staticwebapp.config.json");
  const config = JSON.parse(await readFile(configPath, "utf8"));
  const scriptSources = [...hashes]
    .sort()
    .map((hash) => `'sha256-${hash}'`)
    .join(" ");
  config.globalHeaders["Content-Security-Policy"] = [
    "default-src 'self'",
    "base-uri 'self'",
    "object-src 'none'",
    "frame-ancestors 'none'",
    "form-action 'self'",
    "img-src 'self' data: https://images.unsplash.com",
    `script-src 'self' ${scriptSources}`,
    "style-src 'self' 'unsafe-inline'",
    "font-src 'self' data:",
    "connect-src 'self'",
  ].join("; ");

  await writeFile(configPath, `${JSON.stringify(config, null, 2)}\n`);
  return { htmlPages: htmlFiles.length, scriptHashes: hashes.size };
}

if (
  process.argv[1] &&
  resolve(process.argv[1]) === fileURLToPath(import.meta.url)
) {
  const result = await generateStaticCsp();
  console.log(
    `Generated CSP from ${result.scriptHashes} inline script hashes across ${result.htmlPages} HTML pages.`,
  );
}
