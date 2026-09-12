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

const frontRegex =
  /apiClient\.(get|post|put|delete|patch)(?:<[^>]+>)?\s*\(\s*([`'"])([^`'"]+)\2/g;

frontFiles.forEach((f) => {
  const content = fs.readFileSync(f, "utf8");
  let match;
  while ((match = frontRegex.exec(content)) !== null) {
    const method = match[1].toUpperCase();
    let rawPath = match[3];
    // strip query params
    rawPath = rawPath.split("?")[0];
    if (!rawPath.startsWith("/")) rawPath = "/" + rawPath;

    // Normalize ${...} into {param}
    const normalized = rawPath.replace(/\$\{[^}]+\}/g, "{param}");
    // Prepend /api if not present, because apiClient baseURL has /api
    const fullApi = normalized.startsWith("/api/")
      ? normalized
      : "/api" + normalized;

    frontCalls.push({
      method,
      rawPath,
      normalized: `${method} ${fullApi}`,
      file: f,
    });
  }
});

// 2. Scan Backend Controllers
const backFiles = getFiles("backend/src/PawTrack.API/Controllers", [".cs"]);
const backEndpoints = [];

backFiles.forEach((f) => {
  const content = fs.readFileSync(f, "utf8");
  const routeMatch = /\[Route\("([^"]+)"\)\]/.exec(content);
  let baseRoute = routeMatch ? routeMatch[1] : "";

  const actionRegex = /\[Http(Get|Post|Put|Delete|Patch)(?:\("([^"]*)"\))?\]/g;
  let actionMatch;
  while ((actionMatch = actionRegex.exec(content)) !== null) {
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
      normalized: `${method} ${normalized}`,
      file: f,
    });
  }
});

// Helper regex matcher
function matchesRoute(pattern, target) {
  // convert pattern with {param} into regex
  const regexStr = "^" + pattern.replace(/\{param\}/g, "[^/]+") + "$";
  const reg = new RegExp(regexStr, "i");
  return reg.test(target);
}

// 3. Find frontend calls without backend
const unmatchedFrontend = [];
const uniqueFront = Array.from(new Set(frontCalls.map((c) => c.normalized)));

uniqueFront.forEach((fc) => {
  const [method, route] = fc.split(" ");
  const match = backEndpoints.some(
    (b) =>
      b.method === method && matchesRoute(b.normalized.split(" ")[1], route),
  );
  if (!match) {
    const callers = frontCalls.filter((c) => c.normalized === fc);
    unmatchedFrontend.push({
      endpoint: fc,
      callers: callers.map((c) => c.file),
    });
  }
});

// 4. Find backend routes without frontend calls
const unmatchedBackend = [];
backEndpoints.forEach((be) => {
  const match = frontCalls.some(
    (f) =>
      f.method === be.method &&
      matchesRoute(be.normalized.split(" ")[1], f.normalized.split(" ")[1]),
  );
  if (!match) {
    unmatchedBackend.push({
      method: be.method,
      route: be.route,
      file: path.basename(be.file),
    });
  }
});

console.log("=== UNMATCHED FRONTEND CALLS (POSSIBLE 404) ===");
console.log(JSON.stringify(unmatchedFrontend, null, 2));

console.log(
  `\n=== UNMATCHED BACKEND ROUTES TOTAL: ${unmatchedBackend.length} ===`,
);
fs.writeFileSync(
  "backend_uncalled_routes.json",
  JSON.stringify(unmatchedBackend, null, 2),
);
