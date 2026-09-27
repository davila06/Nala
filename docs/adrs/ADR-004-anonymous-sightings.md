# ADR-004: Avistamientos sin identidad del reportante

**Estado:** DRAFT. **Fecha/owner/aprobación:** no verificados. **Fuente inicial:** [MANUAL_TECNICO.md §20](../Manuales/MANUAL_TECNICO.md).

## Contexto

El manual declara que `Sighting` no almacena datos identificables del reportante y motiva el diseño por privacidad/participación comunitaria.

## Decisión registrada, pendiente de ratificación

Preservar anonimato del reportante en el modelo funcional. La implementación concreta, logs técnicos, telemetría y retención deben revisarse para identificar datos indirectos.

## Alternativas

El manual contrasta la pérdida de trazabilidad individual con riesgo de exponer PII, pero no registra opción de pseudónimo o verificación separada. No asumir esas alternativas evaluadas.

## Consecuencias

Reduce la identificación directa almacenada pero limita contacto posterior, moderación y auditoría; requiere controles de abuso que no reidentifiquen innecesariamente.

## Evidencia

[Sighting.cs](../../backend/src/PawTrack.Domain/Sightings/Sighting.cs), [controller](../../backend/src/PawTrack.API/Controllers/SightingsController.cs), [modelo de datos](../DICCIONARIO_DATOS.md), [pruebas](../../backend/tests/PawTrack.UnitTests/Sightings/).
