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
const frontEndpoints = new Map(); // endpoint -> [files]

const frontRegex =
  /apiClient\.(get|post|put|delete|patch)(?:<[^>]+>)?\s*\(\s*[`'"]([^`'"?#)\s]+)/g;

frontFiles.forEach((f) => {
  const content = fs.readFileSync(f, "utf8");
  let match;
  while ((match = frontRegex.exec(content)) !== null) {
    const method = match[1].toUpperCase();
    let endpoint = match[2];
    if (!endpoint.startsWith("/")) endpoint = "/" + endpoint;
    const key = `${method} ${endpoint}`;
    if (!frontEndpoints.has(key)) {
      frontEndpoints.set(key, []);
    }
    frontEndpoints.get(key).push(f);
  }
});

// 2. Scan Backend Controllers
const backFiles = getFiles("backend/src/PawTrack.API/Controllers", [".cs"]);
const backEndpoints = [];

backFiles.forEach((f) => {
  const content = fs.readFileSync(f, "utf8");
  // Look for Route attribute on class
  const routeMatch = /\[Route\("([^"]+)"\)\]/.exec(content);
  let baseRoute = routeMatch ? routeMatch[1] : "";

  // Look for HttpMethod attributes
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
    // Clean double slashes
    full = full.replace(/\/+/g, "/");
    backEndpoints.push({
      method,
      route: full,
      file: f,
    });
  }
});

console.log("=== FRONTEND ENDPOINTS COUNT ===", frontEndpoints.size);
console.log("=== BACKEND ENDPOINTS COUNT ===", backEndpoints.length);

const output = {
  frontendEndpoints: Array.from(frontEndpoints.keys()).sort(),
  backendEndpoints: backEndpoints.map((b) => `${b.method} ${b.route}`).sort(),
};

fs.writeFileSync("endpoints_audit.json", JSON.stringify(output, null, 2));
console.log("Saved endpoints_audit.json");
