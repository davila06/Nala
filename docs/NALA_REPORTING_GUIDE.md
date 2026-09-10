# Guia Canonica de NALA y Reportes

**Estado:** activo  
**Audiencia:** Admin, Municipality, Clinic y actores institucionales autorizados  
**Corte:** 2026-09-09

## NALA

`/nala` muestra overview, tendencias y capas geograficas generalizadas. No es
un panel de operacion de casos ni un sustituto de la municipalidad, clinica o
aliado.

Los indicadores incluyen, segun alcance y suppression: perdidas activas,
reunificaciones, capturas, adopciones, bienestar, microchips y certificados.
Las consultas respetan canton/organizacion y no deben reconstruir individuos
mediante areas pequeñas consecutivas.

## Reportes institucionales

El flujo de `/reportes-institucionales` permite consultar definiciones,
previews, solicitar exports y descargar resultados cuando el actor tiene el
scope. Los exports son agregados y `SENASA-ready`; no son envios oficiales ni
constituyen aprobacion regulatoria.

## Scopes y privacidad

- `Admin`: alcance global segun politica privilegiada.
- `Municipality`: propia organizacion y cantones autorizados.
- `Clinic`/`Ally`/`Shelter`: solo reportes expresamente permitidos.
- `Owner`, `Store` y `ServiceProvider`: no acceden a reportes institucionales.

Aplicar suppression de grupos pequeños, no exportar GPS exacto, reportantes,
domicilios, evidencia sensible ni identificadores de propietario. Toda
solicitud y descarga debe quedar auditada.

Ver [GUIA_SCOPES_REPORTES.md](GUIA_SCOPES_REPORTES.md),
[RUNBOOK_REPORTES_INSTITUCIONALES.md](RUNBOOK_REPORTES_INSTITUCIONALES.md) y
[MANUAL_NALA.md](MANUAL_NALA.md).
