# Estado Actual de Adopciones

**Estado:** activo  
**Corte:** 2026-09-09

## Actores

- Visitante: consulta directorio, detalle y ferias.
- `Owner`: aplica para adoptar y recibe notificaciones.
- `Ally` verificado con `AllyType = Shelter`: publica animales, administra
  aplicaciones y ferias.
- `Admin`: consulta estadisticas y modera operaciones administrativas.

## Rutas

Publicas: `/adopciones`, `/adopciones/:id`, `/adopciones/ferias`.
Owner: `/mis-adopciones`. Shelter: `/shelter/dashboard`,
`/shelter/publicar` y `/shelter/animales/:id/aplicaciones`.

## Estados del animal

`Available`, `InProcess`, `Adopted`, `Paused`, `Removed`. Publicar requiere
shelter verificado. El acceso base limita animales activos; `ShelterPlus`
habilita animales ilimitados, ferias y destaque segun los gates actuales.

## Aplicaciones

El adoptante envia una solicitud con nota. El shelter revisa, aprueba o
rechaza, recibe y envia notificaciones, y marca el animal adoptado cuando la
entrega termina. Las consultas de aplicaciones son paginadas y deben validar
ownership del shelter.

## Privacidad

Publicar una zona de referencia no equivale a publicar la direccion exacta del
refugio. Fotos y notas deben pasar por Blob Storage y sanitizacion. No publicar
PII del adoptante o del shelter sin necesidad.

La especificacion tecnica historica es
[adopciones.md](adopciones.md); el manual operativo esta en
[Manuales/MANUAL_ALIADOS.md](Manuales/MANUAL_ALIADOS.md).
