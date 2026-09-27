# ADR-006: `Result<T>` para errores de negocio

**Estado:** DRAFT. **Fecha/owner/aprobación:** no verificados. **Fuente inicial:** [MANUAL_TECNICO.md §20](../Manuales/MANUAL_TECNICO.md).

## Contexto

El manual establece que handlers devuelvan `Result<T>` y no propaguen excepciones de negocio entre módulos.

## Decisión registrada, pendiente de ratificación

Usar `Result<T>` como expresión de éxito/fallo de negocio según el texto existente. La aplicación real y excepciones restantes deben auditarse antes de afirmar uniformidad.

## Alternativas

El manual contrasta excepciones como flujo de control, pero no registra comparación con union types, errores tipados u otros patrones.

## Consecuencias

El contrato hace fallos esperados visibles en handlers; los controllers deben traducirlos a respuestas coherentes. La presencia del tipo no prueba uso consistente ni un contrato HTTP uniforme.

## Evidencia

[Manual técnico](../Manuales/MANUAL_TECNICO.md), [Application](../../backend/src/PawTrack.Application/) y observación de variación de errores en [catálogo de errores API](../api/ERROR_CATALOG.md).
