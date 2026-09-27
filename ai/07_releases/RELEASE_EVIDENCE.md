# Registro de evidencia de releases

No se encontró en el inventario de esta tarea un catálogo formal de releases firmado con artefactos, aprobadores y despliegues; por tanto este archivo registra hitos documentales, no versiones desplegadas.

| Fecha      | Hito en changelog                                                                                                                            | Alcance de evidencia                                                                                   |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| 2026-09-09 | Capacidades enterprise clínicas, grants, permisos, exportes/webhooks; collar genérico/OEM mantenido con proveedores externos deshabilitados. | [CHANGELOG](../../docs/CHANGELOG.md); no prueba operación de proveedor.                                |
| 2026-09-18 | SuperAdmin, controles privilegiados y campañas de castración.                                                                                | [CHANGELOG](../../docs/CHANGELOG.md); no es evidencia de release desplegado.                           |
| 2026-09-22 | Gates de evidencia para proveedores, telemetría y controles operativos.                                                                      | [CHANGELOG](../../docs/CHANGELOG.md); validar el detalle por entorno.                                  |
| 2026-09-23 | Estrategia competitiva y roadmap AI/Marketplace; labores aún abiertas.                                                                       | [CHANGELOG](../../docs/CHANGELOG.md), [estrategia](../../docs/COMPETITIVE_INTELLIGENCE_2026-09-23.md). |

Para convertir un hito en release verificable, adjuntar tag/commit, build y tests, SBOM/dependency scan, aprobaciones, configuración sin secretos, evidencia de despliegue/health, migraciones, monitoreo y rollback. Ver [runbook de deployment](../../docs/RUNBOOK_DEPLOYMENT.md) y [testing](../../docs/TESTING.md).
