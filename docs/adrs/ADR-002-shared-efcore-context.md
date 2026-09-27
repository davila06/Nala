# ADR-002: Un contexto EF Core compartido

**Estado:** DRAFT. **Fecha/owner/aprobación:** no verificados. **Fuente inicial:** [MANUAL_TECNICO.md §20](../Manuales/MANUAL_TECNICO.md).

## Contexto

El manual afirma un único `DbContext` debido a referencias compartidas entre dominios. El [PawTrackDbContext](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs) actual reúne entidades de numerosos módulos.

## Decisión registrada, pendiente de ratificación

Mantener un solo contexto SQL Server/EFC, de acuerdo con el texto del manual. Es un registro histórico a validar, no decisión aprobada.

## Alternativas

El manual contrasta múltiples contextos como opción no adoptada. No registra análisis de límites, transacciones, migraciones ni ownership; posibles opciones deben revisarse con arquitectura.

## Consecuencias

Facilita relaciones/transacciones compartidas pero aumenta acoplamiento de esquema, migraciones y ownership entre módulos. No significa que módulos tengan límites de persistencia independientes.

## Evidencia

[DATA_MODEL](../DATA_MODEL.md), [PawTrackDbContext](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs), [registro de infraestructura](../../backend/src/PawTrack.Infrastructure/InfrastructureServiceCollectionExtensions.cs).
