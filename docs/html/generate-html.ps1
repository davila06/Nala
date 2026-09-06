# generate-html.ps1 — convierte todos los .md de /docs a /docs/html
# Requiere: marcado via marked.js CDN (necesita internet la primera vez que se abre el HTML)
# Uso: .\docs\html\generate-html.ps1

param([string]$DocsPath = "C:\Nala\docs")

$sourceDir = if (Test-Path (Join-Path $DocsPath "FEATURES.md")) {
  $DocsPath
} else {
  Join-Path $DocsPath "docs"
}
$htmlDir = Join-Path $sourceDir "html"

$docs = @(
  @{ file = "FEATURES.md";                    title = "Features por Plan";                 icon = "🧭"; back = "../FEATURES.md" }
  @{ file = "planes.md";                     title = "Planes y Precios";                  icon = "💳"; back = "../planes.md" }
  @{ file = "precios.md";                    title = "Precios y Modelo Comercial";       icon = "📊"; back = "../precios.md" }
  @{ file = "NALA.md";                       title = "Producto PawTrack CR";             icon = "🐾"; back = "../NALA.md" }
  @{ file = "Manuales/MANUAL_USUARIO.md";            title = "Manual de Usuario";                icon = "📱"; back = "../Manuales/MANUAL_USUARIO.md" }
  @{ file = "Manuales/MANUAL_ADMINISTRADOR.md";      title = "Manual de Administrador";          icon = "⚙️"; back = "../Manuales/MANUAL_ADMINISTRADOR.md" }
  @{ file = "Manuales/MANUAL_ALIADOS.md";            title = "Manual de Aliados";                icon = "🤝"; back = "../Manuales/MANUAL_ALIADOS.md" }
  @{ file = "Manuales/MANUAL_CLINICAS.md";           title = "Manual de Clínicas Veterinarias";  icon = "🏥"; back = "../Manuales/MANUAL_CLINICAS.md" }
  @{ file = "Manuales/MANUAL_TECNICO.md";            title = "Manual Técnico";                   icon = "🛠️"; back = "../Manuales/MANUAL_TECNICO.md" }
  @{ file = "GUIA_ONBOARDING_DEV.md";       title = "Guía de Onboarding Dev";           icon = "🚀"; back = "../GUIA_ONBOARDING_DEV.md" }
  @{ file = "GUIA_DEPLOY_PASO_A_PASO.md";   title = "Guía de Deploy Paso a Paso";       icon = "☁️"; back = "../GUIA_DEPLOY_PASO_A_PASO.md" }
  @{ file = "RUNBOOK_OPERACIONES.md";       title = "Runbook de Operaciones";           icon = "📟"; back = "../RUNBOOK_OPERACIONES.md" }
  @{ file = "DEPLOY_INFO.md";               title = "Datos de Despliegue Beta";         icon = "🗂️"; back = "../DEPLOY_INFO.md" }
  @{ file = "POLITICA_DE_PRIVACIDAD.md";    title = "Política de Privacidad";           icon = "🔒"; back = "../POLITICA_DE_PRIVACIDAD.md" }
  @{ file = "TERMINOS_DE_USO.md";           title = "Términos de Uso";                  icon = "📋"; back = "../TERMINOS_DE_USO.md" }
)

$navLinks = $docs | ForEach-Object {
  $htmlFile = [System.IO.Path]::GetFileNameWithoutExtension($_.file) + ".html"
  "    <li><a href=`"$htmlFile`" class=`"h2`">$($_.icon) $($_.title)</a></li>"
}
$navLinks = @('    <li><a href="PLANES_FEATURES.html" class="h2">🧭 Planes interactivos</a></li>') + $navLinks
$navLinksStr = ($navLinks -join "`n")

foreach ($doc in $docs) {
  $mdPath  = Join-Path $sourceDir $doc.file
  $htmlOut = Join-Path $htmlDir ([System.IO.Path]::GetFileNameWithoutExtension($doc.file) + ".html")

  if (-not (Test-Path $mdPath)) {
    Write-Warning "Not found: $mdPath"
    continue
  }

  $mdContent = Get-Content $mdPath -Raw -Encoding UTF8
  # JSON-encode the markdown so all backticks, quotes, and newlines are safely escaped
  $mdJson = $mdContent | ConvertTo-Json -Compress

  $html = @"
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>$($doc.title) — PawTrack CR</title>
  <link rel="stylesheet" href="style.css">
  <script src="https://cdn.jsdelivr.net/npm/marked@12/marked.min.js"></script>
  <script src="https://cdn.jsdelivr.net/npm/dompurify@3/dist/purify.min.js"></script>
</head>
<body>
<aside id="sidebar">
  <div class="brand"><span>🐾</span><strong>PawTrack CR<br>Documentación</strong></div>
  <nav><ul>
    <li><a href="index.html" class="h2">← Inicio</a></li>
$navLinksStr
  </ul></nav>
</aside>
<main id="main">
  <div id="content"><p style="color:#888;padding:2rem">Cargando…</p></div>
</main>
<script>
const md = $mdJson;
const html = DOMPurify.sanitize(marked.parse(md));
const content = document.getElementById('content');
content.innerHTML = html;

// Build sidebar nav from rendered headings
const headings = content.querySelectorAll('h2, h3');
const sidebar  = document.querySelector('#sidebar nav ul');
headings.forEach((h, i) => {
  if (!h.id) {
    h.id = 'h-' + i + '-' + h.textContent.toLowerCase()
      .replace(/[^a-z0-9\s]/g, '').replace(/\s+/g, '-').slice(0, 50);
  }
  const li = document.createElement('li');
  const a  = document.createElement('a');
  a.href      = '#' + h.id;
  a.textContent = h.textContent.replace(/^#+\s*/, '');
  a.className = h.tagName.toLowerCase();
  li.appendChild(a);
  sidebar.appendChild(li);
});

// Highlight active link on scroll
const links = sidebar.querySelectorAll('a[href^="#"]');
const obs = new IntersectionObserver(entries => {
  entries.forEach(e => {
    if (e.isIntersecting) {
      links.forEach(l => l.classList.remove('active'));
      const active = sidebar.querySelector('a[href="#' + e.target.id + '"]');
      if (active) active.classList.add('active');
    }
  });
}, { rootMargin: '-20% 0px -75% 0px' });
content.querySelectorAll('h2, h3').forEach(h => obs.observe(h));
</script>
</body>
</html>
"@

  $html | Set-Content -Path $htmlOut -Encoding UTF8
  Write-Host "Generated: $([System.IO.Path]::GetFileName($htmlOut))"
}

Write-Host "`nDone. Open docs/html/index.html in a browser."
