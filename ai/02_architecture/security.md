# Arquitectura de seguridad

## Activos y límites de confianza

El sistema trata identidades y contactos, fotos/documentos de mascotas, reportes y coordenadas, datos veterinarios, tokens/sesiones, actividad de organizaciones y datos financieros de referencia. Consultar [mapa de datos](../../docs/security/PRIVACY_DATA_MAP.md), [matriz de autorización](../../docs/API_AUTHORIZATION_MATRIX.md), [security baseline](../../docs/SECURITY.md) y [gobernanza AI existente](../../docs/ai/AI_GOVERNANCE.md).

La API es frontera de autenticación/autorización; la interfaz y gates de plan no sustituyen comprobaciones backend. La infraestructura cloud y proveedores externos son límites adicionales. Las pruebas específicas y el estado de hallazgos constan en documentación de seguridad; esta síntesis no repite ni cierra findings.

## Guardrails para agentes

- Acceso a datos según usuario, rol, tenant, ownership, propósito y consentimiento; no crear credenciales amplias para el agente.
- No exponer PII, coordenadas exactas, tokens, documentos clínicos ni datos cross-tenant en prompts, logs o memoria.
- Las herramientas deben ser allowlist, validación de argumentos, autorización en servidor, límites/rate limits y auditoría con identificador de ejecución.
- Primera fase: lectura acotada; mutaciones requieren confirmación humana, idempotencia y ruta de reversión donde aplique.
- No usar IA para diagnóstico/tratamiento veterinario, decisión regulatoria o denegación automática de acceso/servicio.
- Revisar prompt injection desde contenido de usuarios/documentos, exfiltración, abuso de herramientas, retención del proveedor y borrado.

La existencia de guardrails escritos no prueba controles implementados. No afirmar cumplimiento legal; ver [auditoría documental de protección de datos](../../docs/CUMPLIMIENTO_PROTECCION_DATOS.md), que no constituye dictamen.
