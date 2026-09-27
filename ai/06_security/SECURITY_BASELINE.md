# Baseline de seguridad para agentes

La seguridad actual del producto y su matriz de controles son las fuentes canónicas: [SECURITY](../../docs/SECURITY.md), [autorización API](../../docs/API_AUTHORIZATION_MATRIX.md), [mapa de datos](../../docs/security/PRIVACY_DATA_MAP.md), [hallazgos](../../docs/security/SECURITY_FINDINGS.md) y [gobernanza AI existente](../../docs/ai/AI_GOVERNANCE.md).

## Datos que requieren tratamiento reforzado

- Identidad, contacto, sesión y auditoría de personas.
- Ubicaciones GPS, sightings y zonas de búsqueda.
- Información veterinaria, certificados y documentos.
- Evidencia/fotos, reportes de bienestar y contenido de usuarios.
- Identificadores de organizaciones, proveedores, pagos y eventos institucionales.

## Controles mínimos antes de cualquier agente

1. Inventario de propósito/base autorizada, owner, consentimiento, retención, región y subprocesadores por fuente.
2. Identidad de servicio dedicada, scopes mínimos y autorización backend; aislamiento por usuario/organización.
3. Redacción/tokenización previa al modelo; exclusión de secretos, PII y coordenadas exactas de logs/memoria por defecto.
4. Herramientas allowlisted y validadas; sin mutaciones autónomas; confirmación humana y auditoría para acciones sensibles.
5. Threat model de prompt injection, exfiltración, abuso de tools y fuga cross-tenant; tests de regresión/abuso y mecanismo de apagado.
6. Revisión legal y de proveedor para retención, entrenamiento, transferencia internacional y borrado.

Este baseline es requisito documental propuesto, no evidencia de controles AI implementados ni declaración de cumplimiento legal.
