# Índice de decisiones arquitectónicas

Los registros siguientes se derivan de siete secciones tituladas ADR en [MANUAL_TECNICO.md](../Manuales/MANUAL_TECNICO.md). No se encontró evidencia de aprobación, autor/owner, fecha de decisión ni revisión formal; por eso se copian como `DRAFT`, no como decisiones vigentes aprobadas. Las afirmaciones se deben cotejar con arquitectura/código actual antes de ratificarlas.

| ID                                          | Tema                                      | Estado | Evidencia base                                                           |
| ------------------------------------------- | ----------------------------------------- | ------ | ------------------------------------------------------------------------ |
| [ADR-001](ADR-001-modular-monolith.md)      | Monolito modular frente a microservicios  | DRAFT  | Manual técnico, §20.                                                     |
| [ADR-002](ADR-002-shared-efcore-context.md) | Un `DbContext` compartido                 | DRAFT  | Manual técnico, §20; `PawTrackDbContext`.                                |
| [ADR-003](ADR-003-jwt-hs256.md)             | JWT HS256 y JTI blocklist                 | DRAFT  | Manual técnico, §20; API/seguridad actual por revalidar.                 |
| [ADR-004](ADR-004-anonymous-sightings.md)   | Anonimato del reportante de avistamientos | DRAFT  | Manual técnico, §20 y modelo `Sighting`.                                 |
| [ADR-005](ADR-005-guid-v7.md)               | GUID v7 para claves primarias             | DRAFT  | Manual técnico, §20 y entidades/migraciones.                             |
| [ADR-006](ADR-006-result-pattern.md)        | `Result<T>` para errores de negocio       | DRAFT  | Manual técnico, §20 y handlers/controllers.                              |
| [ADR-007](ADR-007-public-blob-photos.md)    | Acceso público a algunas fotos Blob       | DRAFT  | Manual técnico, §20 y adaptador actual; requiere revisión de privacidad. |

## Ratificación

Para cambiar de `DRAFT`: registrar contexto actual con rutas, decisión aprobada por responsable identificado, alternativas efectivamente evaluadas, consecuencias/risks, fecha, estado (propuesta/aceptada/sustituida) y referencias a pruebas/configuración. No inferir aprobación porque el manual use el rótulo ADR.
