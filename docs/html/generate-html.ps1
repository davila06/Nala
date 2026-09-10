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
  @{ file = "PRICING_AND_PLANS.md";           title = "Planes y Capacidades";             icon = "💳"; back = "../PRICING_AND_PLANS.md" }
  @{ file = "planes.md";                      title = "Catálogo de Planes y Tiers";       icon = "📋"; back = "../planes.md" }
  @{ file = "precios.md";                     title = "Precios y Modelo Comercial";       icon = "📊"; back = "../precios.md" }
  @{ file = "publicidad.md";                  title = "Guía de Publicidad y Vallas";      icon = "🪧"; back = "../publicidad.md" }
  @{ file = "inversionistas.md";              title = "Pitch para Inversionistas";         icon = "💼"; back = "../inversionistas.md" }
  @{ file = "influencer.md";                  title = "Colaboración con Influencers";      icon = "🤝"; back = "../influencer.md" }
  @{ file = "API_REFERENCE.md";               title = "Referencia API";                    icon = "🔌"; back = "../API_REFERENCE.md" }
  @{ file = "B2B_ESTADO_ACTUAL.md";           title = "Estado Actual B2B";                 icon = "🏢"; back = "../B2B_ESTADO_ACTUAL.md" }
  @{ file = "CONSOLIDACION_DOCUMENTAL.md";    title = "Consolidación Documental";         icon = "🗂️"; back = "../CONSOLIDACION_DOCUMENTAL.md" }
  @{ file = "COLLAR_CURRENT_STATE.md";        title = "Estado Actual de Collares";        icon = "📡"; back = "../COLLAR_CURRENT_STATE.md" }
  @{ file = "ADOPTIONS_CURRENT_STATE.md";    title = "Estado Actual de Adopciones";      icon = "🏡"; back = "../ADOPTIONS_CURRENT_STATE.md" }
  @{ file = "NALA_REPORTING_GUIDE.md";       title = "NALA y Reportes";                  icon = "📊"; back = "../NALA_REPORTING_GUIDE.md" }
  @{ file = "sinpe.md";                      title = "Automatización SINPE Móvil";        icon = "💸"; back = "../sinpe.md" }
  @{ file = "NALA.md";                       title = "Producto PawTrack CR";             icon = "🐾"; back = "../NALA.md" }
  @{ file = "Manuales/MANUAL_USUARIO.md";            title = "Manual de Usuario";                icon = "📱"; back = "../Manuales/MANUAL_USUARIO.md" }
  @{ file = "Manuales/MANUAL_ADMINISTRADOR.md";      title = "Manual de Administrador";          icon = "⚙️"; back = "../Manuales/MANUAL_ADMINISTRADOR.md" }
  @{ file = "Manuales/MANUAL_ALIADOS.md";            title = "Manual de Aliados";                icon = "🤝"; back = "../Manuales/MANUAL_ALIADOS.md" }
  @{ file = "Manuales/MANUAL_CLINICAS.md";           title = "Manual de Clínicas Veterinarias";  icon = "🏥"; back = "../Manuales/MANUAL_CLINICAS.md" }
  @{ file = "Manuales/MANUAL_MUNICIPALIDADES.md";    title = "Manual de Municipalidades";        icon = "🏛️"; back = "../Manuales/MANUAL_MUNICIPALIDADES.md" }
  @{ file = "Manuales/MANUAL_TIENDAS.md";            title = "Manual de Tiendas";                icon = "🏪"; back = "../Manuales/MANUAL_TIENDAS.md" }
  @{ file = "Manuales/MANUAL_PROVEEDORES.md";        title = "Manual de Proveedores";            icon = "🧰"; back = "../Manuales/MANUAL_PROVEEDORES.md" }
  @{ file = "Manuales/MANUAL_SOPORTE.md";            title = "Manual de Soporte";                icon = "🎧"; back = "../Manuales/MANUAL_SOPORTE.md" }
  @{ file = "Manuales/MANUAL_TECNICO.md";            title = "Manual Técnico";                   icon = "🛠️"; back = "../Manuales/MANUAL_TECNICO.md" }
  @{ file = "GUIA_ONBOARDING_DEV.md";       title = "Guía de Onboarding Dev";           icon = "🚀"; back = "../GUIA_ONBOARDING_DEV.md" }
  @{ file = "GUIA_DEPLOY_PASO_A_PASO.md";   title = "Guía de Deploy Paso a Paso";       icon = "☁️"; back = "../GUIA_DEPLOY_PASO_A_PASO.md" }
  @{ file = "RUNBOOK_OPERACIONES.md";       title = "Runbook de Operaciones";           icon = "📟"; back = "../RUNBOOK_OPERACIONES.md" }
  @{ file = "DEPLOY_INFO.md";               title = "Datos de Despliegue Beta";         icon = "🗂️"; back = "../DEPLOY_INFO.md" }
  @{ file = "RUNBOOK_DEPLOYMENT.md";         title = "Runbook de Deployment";             icon = "🚢"; back = "../RUNBOOK_DEPLOYMENT.md" }
  @{ file = "RUNBOOK_SEGURIDAD_INCIDENTES.md"; title = "Seguridad e Incidentes";          icon = "🛡️"; back = "../RUNBOOK_SEGURIDAD_INCIDENTES.md" }
  @{ file = "RUNBOOK_BACKUPS_RECUPERACION.md"; title = "Backups y Recuperación";          icon = "💾"; back = "../RUNBOOK_BACKUPS_RECUPERACION.md" }
  @{ file = "MATRIZ_RETENCION_DATOS.md";    title = "Retención de Datos";                icon = "🧾"; back = "../MATRIZ_RETENCION_DATOS.md" }
  @{ file = "GUIA_QA_E2E.md";               title = "Guía QA y E2E";                     icon = "🧪"; back = "../GUIA_QA_E2E.md" }
  @{ file = "GUIA_INTEGRACIONES_WEBHOOKS.md"; title = "Integraciones y Webhooks";         icon = "🔗"; back = "../GUIA_INTEGRACIONES_WEBHOOKS.md" }
  @{ file = "RUNBOOK_JOBS_BACKGROUND.md";   title = "Jobs en Background";                icon = "⏱️"; back = "../RUNBOOK_JOBS_BACKGROUND.md" }
  @{ file = "RUNBOOK_MODERACION_Y_BIENESTAR.md"; title = "Moderación y Bienestar";         icon = "🐾"; back = "../RUNBOOK_MODERACION_Y_BIENESTAR.md" }
  @{ file = "CUMPLIMIENTO_PROTECCION_DATOS.md"; title = "Cumplimiento Protección de Datos"; icon = "📜"; back = "../CUMPLIMIENTO_PROTECCION_DATOS.md" }
  @{ file = "POLITICA_DE_PRIVACIDAD.md";    title = "Política de Privacidad";           icon = "🔒"; back = "../POLITICA_DE_PRIVACIDAD.md" }
  @{ file = "TERMINOS_DE_USO.md";           title = "Términos de Uso";                  icon = "📋"; back = "../TERMINOS_DE_USO.md" }
)

foreach ($doc in $docs) {
  $mdPath  = Join-Path $sourceDir $doc.file
  $outName = if ($doc.outFile) { $doc.outFile } else { [System.IO.Path]::GetFileNameWithoutExtension($doc.file) + ".html" }
  $htmlOut = Join-Path $htmlDir $outName

  if (-not (Test-Path $mdPath)) {
    Write-Warning "Not found: $mdPath"
    continue
  }

  $mdContent = Get-Content $mdPath -Raw -Encoding UTF8
  # JSON-encode the markdown so all backticks, quotes, and newlines are safely escaped
  $mdJson = $mdContent | ConvertTo-Json -Compress

  $currentNavLinks = $docs | Where-Object { -not $_.outFile } | ForEach-Object {
    $itemFile = [System.IO.Path]::GetFileNameWithoutExtension($_.file) + ".html"
    $activeClass = if ($itemFile -eq $outName) { "h2 active" } else { "h2" }
    "    <li><a href=`"$itemFile`" class=`"$activeClass`">$($_.icon) $($_.title)</a></li>"
  }
  $currentNavLinks = @('    <li><a href="PLANES_FEATURES.html" class="h2">🧭 Planes interactivos</a></li>') + $currentNavLinks
  $navLinksStr = ($currentNavLinks -join "`n")

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

// Rewrite relative .md links to .html within docs/html
content.querySelectorAll('a').forEach(a => {
  const href = a.getAttribute('href');
  if (href && !href.startsWith('http://') && !href.startsWith('https://') && !href.startsWith('#') && !href.startsWith('mailto:')) {
    const m = href.match(/^(?:(?:\.\.\/)*)?(?:(?:docs|Manuales)\/)*([A-Za-z0-9_-]+)\.md(#.*)?$/i);
    if (m) {
      a.setAttribute('href', m[1] + '.html' + (m[2] || ''));
    }
  }
});

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

# Sync sponsors.html from inversionistas.html so legacy links continue to work
$invHtml = Join-Path $htmlDir "inversionistas.html"
$sponsorsHtml = Join-Path $htmlDir "sponsors.html"
if (Test-Path $invHtml) {
    Copy-Item -Path $invHtml -Destination $sponsorsHtml -Force
    Write-Host "Updated: sponsors.html (synced from inversionistas.html)"
}

Write-Host "`nDone. Open docs/html/index.html in a browser."
