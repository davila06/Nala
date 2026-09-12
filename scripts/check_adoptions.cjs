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

const content = fs.readFileSync(
  "backend/src/PawTrack.API/Controllers/AdoptionsController.cs",
  "utf8",
);
const classChunks = content.split(/public\s+(?:sealed\s+)?class\s+/);
console.log("chunks length:", classChunks.length);
for (let i = 1; i < classChunks.length; i++) {
  const prevHeader = classChunks[i - 1];
  const routeMatch = /\[Route\("([^"]+)"\)\]/.exec(prevHeader.slice(-300));
  console.log(`Class ${i} baseRoute:`, routeMatch ? routeMatch[1] : "NONE");
}
