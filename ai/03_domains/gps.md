# Dominio: GPS y collares

El backend tiene módulos de collar, posiciones, zonas seguras, alertas y adaptadores de proveedor. Las capacidades concretas y sus límites se documentan en [GPS](../../docs/domains/GPS.md), [collares](../../docs/domains/COLLARS.md), [matriz de proveedores](../../docs/domains/GPS_PROVIDER_MATRIX.md) y F06/F13 de la [trazabilidad](../../docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md).

Registrar un collar fue probado a nivel handler en el corte de F06; conexión hardware/proveedor no está validada. TrackSolid condicionado por credenciales permanece `NO_VERIFICADO`. Coordenadas de GPS son datos sensibles desde la perspectiva de privacidad y deben tener propósito, alcance, autorización y retención explícitos. No exponer coordenadas exactas a agentes sin necesidad y autorización.
