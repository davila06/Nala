# Alcance del producto

**Fuente de estado:** [PRODUCT_SCOPE](../../docs/PRODUCT_SCOPE.md) y [matriz de trazabilidad](../../docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md), corte 2026-09-27.

## Alcance comprobable

La solución contiene capacidades técnicas en identidad de mascotas, QR, recuperación, avistamientos, salud, clínicas, adopciones, tiendas, proveedores, collares, administración, analítica y suscripciones. Sus alcances puntuales tienen estados separados: `IMPLEMENTADO_Y_VERIFICADO` solo aplica a la operación y prueba indicadas; varias capacidades son `PARCIALMENTE_IMPLEMENTADO`, `IMPLEMENTADO_SIN_PRUEBAS` o `NO_VERIFICADO`.

## Límites explícitos

- Una ruta, pantalla, entidad, flag o `DbSet` no prueba un flujo completo.
- Matching con proveedor externo y disponibilidad de collares no están operativamente verificados.
- Video/voz de telemedicina remota está `DECLARADO_NO_IMPLEMENTADO`.
- Checkout universal, liquidación de fondos, pagos automáticos de reservas e integración oficial con SENASA no deben presentarse como funciones disponibles.
- Las tablas de `FEATURES.md` son un contrato funcional objetivo/propuesto donde así lo declara el documento; no reemplazan enforcement probado.
- RAG, copilotos y agentes por rol son roadmap/propuesta, no producto desplegado.

## Autoridad y cambios

Consultar [alcance auditado](../../docs/PRODUCT_SCOPE.md), [FEATURES](../../docs/FEATURES.md), [limitaciones](../../docs/KNOWN_LIMITATIONS.md) y [documentación de pruebas](../../docs/TESTING.md). Ante contradicción, registrar en el [reporte de brechas](../../docs/auditoria/DOCUMENTATION_GAP_REPORT.md); no resolver borrando historia o elevando estados sin evidencia.
