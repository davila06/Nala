# Runbook de Moderacion y Bienestar Animal

**Estado:** activo  
**Audiencia:** Support, Admin, aliados y municipalidades  
**Corte:** 2026-09-09

## Entrada de casos

Los casos pueden venir de usuarios, municipalidades, aliados o flujos de
captura. Registrar hechos, evidencia, ubicacion aproximada y origen sin pedir
PII innecesaria.

## Flujo de triage

1. Abrir la cola y validar que el caso no sea duplicado.
2. Iniciar triage y asignar severidad.
3. Revisar evidencia sensible solo con necesidad operacional.
4. Asignar organizacion o derivar con destino y motivo.
5. Agregar notas factuales.
6. Resolver, descartar o cerrar sin accion con razon obligatoria.

## Severidad

La severidad debe reflejar riesgo para el animal, urgencia, evidencia y
capacidad de respuesta. No usarla para castigar a un reportante ni para
priorizar una organizacion por preferencia personal.

## Evidencia

Se aceptan tipos definidos por el backend. Mantener archivos en Blob privado,
limitar descargas, no compartir URLs directas y conservar cadena de auditoria.
Si la evidencia sugiere riesgo humano, fraude grave o delito, escalar a Admin y
seguir protocolos externos aplicables.

## Roles

- `Support`: triage y operación de colas autorizadas.
- `Admin`: asignación global, políticas, acceso y escalamiento.
- `Municipality`: captura propia y conversión autorizada a bienestar.
- `Ally`: acciones sobre casos expresamente asignados.

Ver [MANUAL_SOPORTE.md](Manuales/MANUAL_SOPORTE.md),
[MANUAL_MUNICIPALIDADES.md](Manuales/MANUAL_MUNICIPALIDADES.md) y
[MANUAL_ALIADOS_REFUGIOS.md](MANUAL_ALIADOS_REFUGIOS.md).
