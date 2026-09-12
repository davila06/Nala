const fs = require("fs");
const path = require("path");

function getFiles(dir, exts) {
  let results = [];
  if (!fs.existsSync(dir)) return results;
  const list = fs.readdirSync(dir);
  list.forEach((file) => {
    const fullPath = path.join(dir, file);
    const stat = fs.statSync(fullPath);
    if (stat && stat.isDirectory()) {
      results = results.concat(getFiles(fullPath, exts));
    } else if (exts.some((ext) => fullPath.endsWith(ext))) {
      results.push(fullPath);
    }
  });
  return results;
}

// 1. Scan Frontend API calls
const frontFiles = getFiles("frontend/src", [".ts", ".tsx"]);
const frontCalls = [];

// Matches apiClient.get("..."), apiClient.post(`...`), etc.
const frontRegex =
  /apiClient\.(get|post|put|delete|patch)(?:<[^>]+>)?\s*\(\s*([`'"])([^`'"]+)\2/g;

frontFiles.forEach((f) => {
  const content = fs.readFileSync(f, "utf8");
  let match;
  while ((match = frontRegex.exec(content)) !== null) {
    const method = match[1].toUpperCase();
    let rawPath = match[3];
    rawPath = rawPath.split("?")[0];
    if (!rawPath.startsWith("/")) rawPath = "/" + rawPath;

    // Normalize ${...} into {param}
    const normalizedPath = rawPath.replace(/\$\{[^}]+\}/g, "{param}");
    const fullApi = normalizedPath.startsWith("/api/")
      ? normalizedPath
      : "/api" + normalizedPath;

    frontCalls.push({
      method,
      rawPath,
      fullApi: fullApi.replace(/\/+/g, "/"),
      file: f,
    });
  }
});

// Also match fetch() or direct axios calls if any
const fetchRegex =
  /(?:fetch|axios\.(get|post|put|delete))\s*\(\s*([`'"])([^`'"]+)\2/g;
frontFiles.forEach((f) => {
  const content = fs.readFileSync(f, "utf8");
  let match;
  while ((match = fetchRegex.exec(content)) !== null) {
    const method = match[1] ? match[1].toUpperCase() : "GET";
    let rawPath = match[3].split("?")[0];
    if (rawPath.startsWith("http")) {
      try {
        const u = new URL(rawPath);
        rawPath = u.pathname;
      } catch {}
    }
    if (!rawPath.startsWith("/")) rawPath = "/" + rawPath;
    const normalizedPath = rawPath.replace(/\$\{[^}]+\}/g, "{param}");
    const fullApi = normalizedPath.startsWith("/api/")
      ? normalizedPath
      : "/api" + normalizedPath;
    frontCalls.push({
      method,
      rawPath,
      fullApi: fullApi.replace(/\/+/g, "/"),
      file: f,
    });
  }
});

// 2. Scan Backend Controllers
const backFiles = getFiles("backend/src/PawTrack.API/Controllers", [".cs"]);
const backEndpoints = [];

backFiles.forEach((f) => {
  const content = fs.readFileSync(f, "utf8");

  // A file can have multiple controller classes (e.g. StoresController and PublicStoresController)
  // Split by class definition or match Route attributes before classes
  const classChunks = content.split(/public\s+(?:sealed\s+)?class\s+/);

  for (let i = 1; i < classChunks.length; i++) {
    const chunk = classChunks[i];
    const prevHeader = classChunks[i - 1];

    // Route on this class
    let baseRoute = "";
    const routeMatch = /\[Route\("([^"]+)"\)\]/.exec(prevHeader.slice(-300));
    if (routeMatch) {
      baseRoute = routeMatch[1];
    }

    const actionRegex =
      /\[Http(Get|Post|Put|Delete|Patch)(?:\("([^"]*)"\))?\]/g;
    let actionMatch;
    while ((actionMatch = actionRegex.exec(chunk)) !== null) {
      const method = actionMatch[1].toUpperCase();
      const subRoute = actionMatch[2] !== undefined ? actionMatch[2] : "";

      let full = "";
      if (subRoute.startsWith("/")) {
        full = subRoute;
      } else if (baseRoute) {
        full = "/" + baseRoute + (subRoute ? "/" + subRoute : "");
      } else {
        full = "/" + subRoute;
      }
      full = full.replace(/\/+/g, "/");

      // Normalize route parameters {param:guid}, {param}, etc. into {param}
      const normalized = full.replace(/\{[^}:]+(?::[^}]+)?\}/g, "{param}");

      backEndpoints.push({
        method,
        route: full,
        normalized: normalized.replace(/\/+/g, "/"),
        file: path.basename(f),
      });
    }
  }
});

function routesEqual(frontNorm, backNorm) {
  const fParts = frontNorm.split("/").filter(Boolean);
  const bParts = backNorm.split("/").filter(Boolean);
  if (fParts.length !== bParts.length) return false;
  for (let i = 0; i < fParts.length; i++) {
    if (fParts[i] === "{param}" || bParts[i] === "{param}") continue;
    if (fParts[i].toLowerCase() !== bParts[i].toLowerCase()) return false;
  }
  return true;
}

// Map frontend calls
const missingInBackend = [];
const matchedFrontend = [];

const uniqueFront = Array.from(
  new Set(frontCalls.map((c) => `${c.method} ${c.fullApi}`)),
);

uniqueFront.forEach((fc) => {
  const [method, route] = fc.split(" ");
  const found = backEndpoints.some(
    (b) => b.method === method && routesEqual(route, b.normalized),
  );
  if (found) {
    matchedFrontend.push(fc);
  } else {
    const callers = frontCalls.filter((c) => `${c.method} ${c.fullApi}` === fc);
    missingInBackend.push({
      endpoint: fc,
      callers: Array.from(
        new Set(callers.map((c) => path.relative(".", c.file))),
      ),
    });
  }
});

// Map backend endpoints
const matchedBackend = [];
const uncalledBackend = [];

backEndpoints.forEach((be) => {
  const found = frontCalls.some(
    (f) => f.method === be.method && routesEqual(f.fullApi, be.normalized),
  );
  if (found) {
    matchedBackend.push(`${be.method} ${be.route}`);
  } else {
    uncalledBackend.push(be);
  }
});

console.log("--------------------------------------------------");
console.log("Total unique Frontend API calls:", uniqueFront.length);
console.log("Matched in Backend:", matchedFrontend.length);
console.log("Missing in Backend (Potential 404s):", missingInBackend.length);
console.log("Total Backend Endpoints:", backEndpoints.length);
console.log("Called by Frontend:", matchedBackend.length);
console.log("Not directly called by Frontend:", uncalledBackend.length);
console.log("--------------------------------------------------");

if (missingInBackend.length > 0) {
  console.log("\n❌ FRONTEND CALLS WITHOUT BACKEND:");
  console.log(JSON.stringify(missingInBackend, null, 2));
} else {
  console.log("\n✅ ALL FRONTEND API CALLS HAVE A MATCHING BACKEND ENDPOINT!");
}

// Group uncalled backend routes by controller
const byController = {};
uncalledBackend.forEach((b) => {
  byController[b.file] = byController[b.file] || [];
  byController[b.file].push(`${b.method} ${b.route}`);
});

fs.writeFileSync(
  "detailed_uncalled_backend.json",
  JSON.stringify(byController, null, 2),
);
console.log("\nWrote detailed_uncalled_backend.json");
