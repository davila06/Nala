# Guía de scopes de reportes

## Scopes

- `Public`: indicadores agregados y suppression obligatoria.
- `Institutional`: datos operativos agregados dentro del cantón/organización
  autorizada.
- `Nala`: overview y capas nacionales para roles NALA/Admin.
- `Admin`: operación y soporte interno con auditoría.

## Reglas

- El backend determina autorización; el frontend nunca decide el scope.
- Municipalidad: cantón propio o cantones incluidos en su tier autorizado.
- Clínica/aliado/refugio: reportes propios o casos asignados; no pueden escoger
  otra organización mediante `organizationId`.
- Admin/NALA: acceso nacional sujeto a auditoría y minimización.
- Un export ajeno responde como inexistente para evitar enumeración de IDs.

## Filtros

Los filtros aceptados son periodo, cantón, especie, estado y organización cuando
el reporte lo soporte. Un filtro fuera del scope autorizado se rechaza en el
backend con Problem Details seguro.
