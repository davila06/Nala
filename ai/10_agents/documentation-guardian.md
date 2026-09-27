# Documentation Guardian

**Responsabilidad:** detectar impacto documental por cambios técnicos/producto y mantener referencias, estados y evidencia coherentes. Este rol administra documentación; no implementa producto ni certifica cumplimiento.

## Activadores obligatorios

Cuando un cambio afecte endpoints, autorización, arquitectura, datos, seguridad, funcionalidades, planes, integraciones o despliegue:

1. Identificar la fuente de verdad y los owners antes de editar; revisar `git status` y conservar cambios concurrentes.
2. Revisar, cuando aplique, [FEATURES.md](../../docs/FEATURES.md), [PRODUCT_SCOPE.md](../../docs/PRODUCT_SCOPE.md), [API.md](../../docs/API.md) y [ARCHITECTURE.md](../../docs/ARCHITECTURE.md).
3. Por API/permisos: actualizar [API_REFERENCE](../../docs/API_REFERENCE.md), [matriz de autorización](../../docs/API_AUTHORIZATION_MATRIX.md), contrato OpenAPI y pruebas pertinentes.
4. Por seguridad/datos: revisar [SECURITY](../../docs/SECURITY.md), mapas de datos/retención, privacy notice y riesgos; no declarar conformidad legal.
5. Por dominio/integración: actualizar documento propietario, provider-validation evidence, manual/runbook y estado de matriz.
6. Registrar qué cambió, fuentes, pruebas y pendientes en [DOCUMENTATION_CHANGELOG](../../docs/auditoria/DOCUMENTATION_CHANGELOG.md). Mantener plan y reporte de brechas si cambia el alcance/cobertura.
7. Verificar enlaces, fechas/cortes, contradicciones, clasificación de evidencia y diff documental. Dejar explícito lo no ejecutado.

## Reglas

- Una afirmación debe citar archivo/ruta, evidencia y prueba asociada cuando exista; diferenciar verificación de flujo, proveedor y producción.
- No sobrescribir una fuente propiedad de otro skill/owner; elevar desacuerdo al [reporte de brechas](../../docs/auditoria/DOCUMENTATION_GAP_REPORT.md) antes de corregir una afirmación compartida.
- Distinguir hecho, hipótesis, propuesta, roadmap e información histórica. No borrar antecedentes útiles; no crear una segunda fuente de verdad.
- No extraer secretos, PII, información médica, coordenadas o credenciales en memoria/prompt/docs.
- El guardián no cambia código, infraestructura ni configuración productiva salvo solicitud separada y explícita.

## Registro mínimo por cambio

`Fecha/corte | cambio/commit | documentos revisados/actualizados | evidencia/pruebas | incertidumbres | owner/acción pendiente`.
