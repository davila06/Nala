const fs = require("fs");

const uncalled = JSON.parse(
  fs.readFileSync("detailed_uncalled_backend.json", "utf8"),
);

console.log("=== BREAKDOWN OF UNCALLED BACKEND ENDPOINTS BY CATEGORY ===\n");

// Group into categories
const categories = {
  "Webhooks & Integraciones Externas (Meta, Telegram, Pasarelas)": [],
  "Endpoints de Dispositivos / Hardware IoT": [],
  "Endpoints Públicos de Escaneo QR / Redirección / Health": [],
  "APIs B2B Machine-to-Machine (Integración CRM Clínicas con API Key)": [],
  "Endpoints de Administración / Moderación": [],
  "Endpoints Generales / Secundarios": [],
};

Object.keys(uncalled).forEach((file) => {
  const routes = uncalled[file];
  routes.forEach((r) => {
    const item = `${file}: ${r}`;
    if (
      r.includes("webhook") ||
      r.includes("bot") ||
      r.includes("callback") ||
      file.includes("Bot")
    ) {
      categories[
        "Webhooks & Integraciones Externas (Meta, Telegram, Pasarelas)"
      ].push(item);
    } else if (
      r.includes("location") &&
      (file.includes("Collar") || file.includes("Ingest"))
    ) {
      categories["Endpoints de Dispositivos / Hardware IoT"].push(item);
    } else if (
      r.includes("/health") ||
      r.includes("/scan") ||
      r.includes("/qr") ||
      r.includes("/p/")
    ) {
      categories[
        "Endpoints Públicos de Escaneo QR / Redirección / Health"
      ].push(item);
    } else if (
      file.includes("PetLookup") ||
      file.includes("ClinicApi") ||
      file.includes("Widget")
    ) {
      categories[
        "APIs B2B Machine-to-Machine (Integración CRM Clínicas con API Key)"
      ].push(item);
    } else if (file.startsWith("Admin")) {
      categories["Endpoints de Administración / Moderación"].push(item);
    } else {
      categories["Endpoints Generales / Secundarios"].push(item);
    }
  });
});

Object.keys(categories).forEach((cat) => {
  console.log(`### ${cat} (${categories[cat].length} endpoints)`);
  categories[cat].slice(0, 10).forEach((i) => console.log("  * " + i));
  if (categories[cat].length > 10) {
    console.log(`  ... y ${categories[cat].length - 10} más.`);
  }
  console.log("");
});
