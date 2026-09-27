# Dominio: identidad y acceso

La API contiene registro, autenticación, sesión, MFA, gestión de roles y control de acceso. Fuentes: [AuthController](../../backend/src/PawTrack.API/Controllers/AuthController.cs), [WebAuthnController](../../backend/src/PawTrack.API/Controllers/WebAuthnController.cs), [matriz de autorización](../../docs/API_AUTHORIZATION_MATRIX.md) y F01/F25 de la [trazabilidad](../../docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md).

La existencia de endpoints no verifica el journey end-to-end ni una cuenta de producción. Los agentes nunca heredan identidad humana implícitamente: cada lectura/escritura debe pasar autorización del backend y contexto de tenant/ownership correspondiente. No guardar tokens, secretos ni datos de prueba en memoria de agente.
