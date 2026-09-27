# ADR-003: JWT HS256 y JTI blocklist

**Estado:** DRAFT. **Fecha/owner/aprobación:** no verificados. **Fuente inicial:** [MANUAL_TECNICO.md §20](../Manuales/MANUAL_TECNICO.md).

## Contexto

El manual describe tokens HS256 y una blocklist JTI para logout/revocación, con la hipótesis de un solo servicio y secreto protegido.

## Decisión registrada, pendiente de ratificación

Mantener firma simétrica HS256 y revocación consultable vía JTI según el manual. Configuración/algoritmo actuales, rotación, almacenamiento de claves y estrategia de identidad deben verificarse antes de aprobar.

## Alternativas

El manual nombra RS256 como contraste y aporta una razón de infraestructura, pero no evidencia evaluación de amenazas, operación de llaves ni evolución multi-servicio. No atribuir una evaluación adicional.

## Consecuencias

Validadores necesitan acceso al secreto compartido; su compromiso puede habilitar firma. La blocklist necesita disponibilidad/retención y pruebas de revocación. Revisar [SECURITY](../SECURITY.md), [auth](../../backend/src/PawTrack.API/Controllers/AuthController.cs) y configuración antes de ratificar.

## Evidencia

[Manual técnico](../Manuales/MANUAL_TECNICO.md), [programa API](../../backend/src/PawTrack.API/Program.cs) y [matriz de autorización](../API_AUTHORIZATION_MATRIX.md).
