# Prompt reutilizable: revisión de seguridad

## Entrada

- Superficie/endpoint/dominio: `<indicar>`
- Activos, actores y ambiente: `<indicar>`
- Corte/commit y pruebas disponibles: `<indicar>`

## Instrucciones

Revisa autenticación, autorización por owner/tenant/rol, validación, exposición de datos, logs, secretos, rate limiting, concurrencia, persistencia, proveedores y abuso. Para hallazgos, muestra ruta y evidencia específica, precondiciones, impacto y test que confirma/refuta. No reportes una amenaza genérica como vulnerabilidad confirmada.

Consulta [SECURITY](../../docs/SECURITY.md), [matriz de autorización](../../docs/API_AUTHORIZATION_MATRIX.md), [mapa de datos](../../docs/security/PRIVACY_DATA_MAP.md) y [baseline AI](../06_security/SECURITY_BASELINE.md). No afirmar cumplimiento legal ni modificar código.

## Salida

Findings primero por severidad; después incertidumbres, pruebas requeridas, riesgo residual y documentos/owners a notificar. No incluir secretos/PII en la salida.
