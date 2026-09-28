# Changelog documental de auditoría

## 2026-09-27

- Orquestador: creado [plan](DOCUMENTATION_UPDATE_PLAN.md), [alcance](../PRODUCT_SCOPE.md), [matriz](FEATURE_TRACEABILITY_MATRIX.md) y [reporte de brechas](DOCUMENTATION_GAP_REPORT.md); actualizado [índice](../README.md) sin borrar históricos.
- Inventario: agregada nota de corte en [FEATURES](../FEATURES.md), conservando la matriz objetivo y sus decisiones de producto.
- Arquitectura y seguridad: añadidos [resumen](../ARCHITECTURE.md) y [controles](../SECURITY.md) con subdocumentos en `docs/architecture/` y `docs/security/`; no se modificó infraestructura ni software.
- API y planes: creados [API](../API.md), [comercial](../commercial/PLANS.md), catálogos auxiliares y [CONFIGURATION](../CONFIGURATION.md), [DATA_MODEL](../DATA_MODEL.md), [INTEGRATIONS](../INTEGRATIONS.md), [TESTING](../TESTING.md), [KNOWN_LIMITATIONS](../KNOWN_LIMITATIONS.md).
- Especialistas: nuevos documentos bajo `docs/domains/`, `docs/editions/`, `docs/growth/`, `docs/ai/` y `docs/business/`, con diferencias entre capacidades y propuestas.
- Archivados: ninguno; [README](../README.md) preserva la clasificación histórica de documentos previos. Fuentes anteriores no se reemplazaron de manera silenciosa.

Cada afirmación nueva usa evidencia de código enlazada en su documento. Pruebas de integración, despliegue y proveedores siguen pendientes ([TESTING](../TESTING.md)); este changelog no los acredita.

## 2026-09-27 — AI Knowledge Hub

- Creado `/ai/` como hub Evidence First con contexto, inventario técnico, producto/historia, arquitectura, dominios, integraciones, seguridad, releases, siete prompts reutilizables, scorecard y gobernanza de agentes.
- Creados `docs/adrs/` con siete ADR en estado `DRAFT`; son transcripciones rastreables de la sección ADR del manual técnico, no decisiones formalmente aprobadas.
- Creados `docs/business/ONTOLOGY.md`, `docs/business/CAPABILITIES.md`, `docs/strategy/` y `docs/compliance/` como síntesis/propuestas con evidencia y límites explícitos.
- Actualizado el índice [docs/README.md](../README.md) para descubrir el hub. No se modificó código productivo ni se ejecutaron pruebas de producto, migraciones o despliegues.
- Validación documental final: 67 archivos revisados, 0 enlaces locales rotos, 0 problemas de formato básico y 30 capacidades asignadas una vez cada una. Diagnóstico del editor sin errores en los documentos comprobados; `git diff --check` limpio. Scorecard cualitativo 63/100; no certifica readiness de producción.

## 2026-09-28 — Revalidación focal de entitlements

- `FEATURES.md` §24: se actualizaron checks obsoletos y se clasificaron modelo/configuración, migraciones, servicio, snapshot, idempotencia, gates por dominio, UI, cuotas y aprobaciones con evidencia relativa.
- Trazabilidad F12 se cambió a `PARCIALMENTE_IMPLEMENTADO`: el servicio y varias rutas están activos, pero permanece fallback legacy y no existe CRUD verificado de definiciones de entitlement.
- `DOCUMENTATION_GAP_REPORT.md` registra medidor sin montar, ausencia de alertas de umbral, gates por dominio incompletos y límites de validación legal/comercial/producción.
- Validación del código vigente: build de `PawTrack.sln` aprobado; suite backend 1,803/1,803 (1,634 unitarias + 169 integración) aprobadas. No se ejecutaron migraciones ni despliegues.

## 2026-09-28 — Auditoría de alcance real

- Se actualizó [PRODUCT_SCOPE](../PRODUCT_SCOPE.md) con la matriz vigente de
  QR, NFC, GPS, telemedicina, expediente, recordatorios, suscripciones,
  marketplace, IA, municipalidades, refugios, veterinarias e integraciones.
- Se actualizaron [FEATURES](../FEATURES.md) y la [matriz de trazabilidad](FEATURE_TRACEABILITY_MATRIX.md)
  para separar implementación real, parcial, propuesta y capacidades ausentes.
- Se documentó que ACS, Dynamics 365 y Power Platform no tienen SDK, código,
  configuración ni endpoints en el repositorio. Se separaron adaptadores Azure
  de operación desplegada y se registraron límites de proveedor/hardware.
- Se corrigieron cifras de verificación: 1,634 unitarias, 169 integración y
  147 frontend; no se certifican staging, producción ni proveedores externos.

## 2026-09-28 — Auditoría integral de producto y estrategia

- Creados [estado real](../NALA_ACTUAL_PRODUCT_STATE.md) y [mapa de capacidades](../NALA_CAPABILITY_MAP.md), con estados separados para implementación, parcialidad, propuesta y operación externa no verificada.
- Creados [brechas competitivas](../NALA_COMPETITIVE_GAP_ANALYSIS.md), [estrategia Costa Rica](../NALA_CR_MARKET_STRATEGY.md), [expansión LATAM](../NALA_LATAM_EXPANSION.md) y [visión global](../NALA_GLOBAL_VISION.md). Las comparaciones y recomendaciones están marcadas como hipótesis estratégicas sujetas a validación.
- Creados [estrategia IA](../NALA_AI_STRATEGY.md) y [estrategia de monetización](../NALA_PRICING_STRATEGY.md), distinguiendo matching visual, entitlements y pagos existentes de RAG, copilotos, telemedicina, payout y claims comerciales aún no demostrados.
- Creada la [auditoría documental](../DOCUMENTATION_AUDIT.md) y el [resumen ejecutivo](../NALA_EXECUTIVE_SUMMARY.md). No se eliminaron documentos históricos ni se modificó código, infraestructura o pruebas.

## 2026-09-28 — Pricing, planes y monetización

- Creados 11 informes pedidos: inventario de features, mapeo de tiers, puntuación heurística por plan, análisis competitivo, precios CR/LATAM, roadmap de monetización, escenarios de unit economics, auditoría documental de pricing, resumen ejecutivo y matriz maestra de features/planes/upsell.
- La [matriz maestra](../NALA_FEATURE_PLAN_UPSELL_MATRIX.md) es navegación de estrategia, no fuente de enforcement. Se separa tier actual de paquete recomendado y se marca cada cuota sin datos como `REQUIERE VALIDACIÓN CON DATOS DE USO`.
- Actualizado [índice](../README.md) para enlazar los informes nuevos y corregido [PRICING_AND_PLANS](../PRICING_AND_PLANS.md): UserFamilia tiene límite técnico `MaxPets=25`, no “ilimitado”, según migración y fallback legacy.
- Preservados pricing briefs anteriores; no se borró/archive ningún documento. Auditoría de `docs/` contabiliza 204 Markdown, pero deja `SIN EVIDENCIA` los archivos que no se leyeron en esta ampliación.
- Competidores se contrastaron con páginas oficiales accesibles el 2026-09-28; varios sitios redirigieron, bloquearon o no dieron contenido. World Bank PPA se usa como indicador direccional, no como medida de elasticidad ni ingreso disponible.
- La tarea VS Code `test-backend` terminó con código de salida 0, pero el resumen de conteos no quedó recuperable; no se documentan cantidades por proyecto. Azure Retail Pricing para SQL `GP_S_Gen5_1`/East US devolvió cero filas. No se afirma costo real, CAC, LTV, margen, break-even, venta, producción ni aprobación legal/comercial.
- Revalidación: `Test-GoLiveGovernance.ps1` corre en el workflow de despliegue productivo antes de publicar artefactos; no se encontró enforcement de las flags en endpoints de compra, catálogo o UI. El trial Verified de 30 días en primera aprobación se confirmó en `ServiceProvider.Activate()` y no se reinicia al reactivar.
