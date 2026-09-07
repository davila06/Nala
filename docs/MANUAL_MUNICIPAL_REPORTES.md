# Manual Municipal — Reportes y Bienestar

## Acceso

El portal municipal utiliza el usuario autenticado y el scope asignado al perfil
municipal. Una municipalidad básica solo puede consultar su cantón; tiers
superiores pueden incluir los cantones contratados. El backend valida el scope,
independientemente de los filtros de la interfaz.

## Flujo mensual

1. Registra capturas con cantón, especie, estado y fecha.
2. Convierte una captura en caso de bienestar cuando existan indicios de abandono,
   negligencia o riesgo.
3. Entra a `/reportes-institucionales`.
4. Solicita un reporte de capturas o bienestar con periodo y cantón autorizado.
5. Espera `Completed` y descarga el PDF/CSV/JSON privado.
6. Conserva el `ExportCode` y hash del archivo para auditoría.

## Privacidad

No se deben exportar direcciones exactas, teléfonos, reportantes, evidencia ni
notas clínicas. Los grupos pequeños pueden aparecer como `Suppressed`.

## Bienestar

La conversión captura -> caso conserva la relación operativa, sanitiza el texto
y permite triage, asignación, derivación y cierre por los roles autorizados.

## Incidentes

Reportar export incorrecto, Blob faltante, hash inconsistente o acceso indebido
con `ExportCode`, cantón, usuario y hora. No adjuntar tokens, SAS ni datos
personales en el ticket.
