const fs = require("fs");

const data = JSON.parse(
  fs.readFileSync("backend_uncalled_routes.json", "utf8"),
);
const byFile = {};
data.forEach((item) => {
  byFile[item.file] = byFile[item.file] || [];
  byFile[item.file].push(`${item.method} ${item.route}`);
});

console.log(
  "Controllers with endpoints not directly called by frontend client:",
);
Object.keys(byFile)
  .sort()
  .forEach((f) => {
    console.log(`\n=== ${f} (${byFile[f].length} routes) ===`);
    byFile[f].forEach((r) => console.log("  " + r));
  });
