# ADR-005: GUID v7 como identificador

**Estado:** DRAFT. **Fecha/owner/aprobación:** no verificados. **Fuente inicial:** [MANUAL_TECNICO.md §20](../Manuales/MANUAL_TECNICO.md).

## Contexto

El manual indica `Guid.CreateVersion7()` para claves primarias y describe orden temporal/localidad de índice como rationale.

## Decisión registrada, pendiente de ratificación

Usar GUID v7 en identificadores de dominio según el registro narrativo. Debe comprobarse cobertura por entidad, proveedor de .NET y migraciones antes de elevar el ADR.

## Alternativas

El manual compara GUID v4; no aporta benchmark del workload ni comparación con identity/ULID. La evaluación efectiva no está registrada.

## Consecuencias

Identificadores ordenables pueden cambiar distribución/índices y exponer orden temporal aproximado. Verificar constraints, generación centralizada y compatibilidad antes de cambios de esquema.

## Evidencia

[Manual técnico](../Manuales/MANUAL_TECNICO.md), entidades en [Domain](../../backend/src/PawTrack.Domain/) y [migraciones](../../backend/src/PawTrack.Infrastructure/Persistence/Migrations/).
