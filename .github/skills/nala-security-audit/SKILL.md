---
name: nala-security-audit
description: Audita la seguridad de NALA en código, configuración, API, datos, Azure e integraciones. Usar al revisar autenticación, autorización, JWT, OAuth, roles, permisos, BOLA, IDOR, rate limiting, secretos, privacidad o controles de acceso.
---

# Auditoría de seguridad

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Antes de cambios documentales extensos, inventaría proyectos y superficies de entrada con `nala-product-auditor` y prepara un plan. Revisa autenticación, autorización, claims, roles, policies, propiedad de recursos, BOLA/IDOR y validación de entrada hasta el almacenamiento.
2. Inspecciona exposición de datos, GPS, ubicación, responsables, telemedicina, cargas de archivos, CORS, rate limiting, logs sensibles, secretos, dependencias, integraciones y configuración cloud. Usa OWASP Top 10 donde corresponda; comprueba enforcement en rutas y servicios, no solo atributos o políticas declaradas.
3. Cuando sea seguro, ejecuta pruebas existentes de autorización y `dotnet list package --vulnerable`; busca indicadores de secretos sin mostrar valores. No hagas pentest destructivo, despliegue, acceso a sistemas externos ni cambios de código. No afirmes cumplimiento legal ni certificaciones sin evidencia formal.
4. Anota para cada hallazgo severidad, superficie, condiciones, impacto, evidencia con rutas relativas, mitigación sugerida y estado de verificación. Si clasificas funciones usa `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`; una prueba no ejecutada no verifica el control.
5. Actualiza `docs/SECURITY.md`, `docs/security/THREAT_MODEL.md`, `docs/security/SECURITY_FINDINGS.md` y `docs/security/PRIVACY_DATA_MAP.md`. Elimina datos sensibles de ejemplos y resultados; comprueba diff, enlaces y evidencia.

## Coordinación

- Depende del inventario de `nala-product-auditor` y la arquitectura de `nala-architecture-review`; revisa transversalmente todos los dominios. Recomienda `nala-api-documenter` para rutas, y skills de GPS, salud y telemedicina para flujos específicos; los hallazgos de seguridad no redefinen su documentación funcional.
- Puede modificar solo los cuatro documentos de seguridad indicados y sus entradas en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica documentación comercial o de dominio, pruebas, configuración, infraestructura ni código.
- Registra conflictos en el reporte de brechas a través del orquestador antes de reemplazar afirmaciones; conserva secciones ajenas y anota cambios propios en el changelog sin divulgar secretos.
