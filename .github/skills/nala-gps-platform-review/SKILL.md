---
name: nala-gps-platform-review
description: Audita la plataforma GPS, geolocalización, collares e IoT de NALA. Usar al revisar tracking, historial de posiciones, geofencing, polling, batería, Tractive, Queclink, Concox, hardware propio, Azure IoT Hub o firmware.
---

# GPS, collares e IoT

## Procedimiento

Antes de cambios extensos, revisa todo el repositorio por módulos sin omitir proyectos y crea o actualiza el plan documental con el orquestador. En auditorías modifica solo `docs/`, preserva antecedentes útiles en `docs/archive/` y no expongas secretos.

1. Con inventario y arquitectura comprobados, revisa rutas, vinculación de dispositivos, workers, adaptadores, modelo de datos, UI, infraestructura y pruebas. Prepara plan con el orquestador antes de cambios extensos; no actives dispositivos ni uses credenciales reales.
2. Verifica proveedores GPS, OAuth sin revelar credenciales, ownership, registro y vinculación, ingestión, polling, frecuencia, historial, retención, geofencing, alertas, batería, desconexión, datos offline, rate limits, privacidad y jobs. Para hardware propio examina firmware, provisioning, certificados y OTA; contrasta Azure IoT Hub o equivalente con registro y tráfico demostrables, sin presuponer despliegue.
3. Distingue integración implementada, integración simulada, proveedor evaluado, prototipo de hardware y hardware futuro. Por capacidad usa `IMPLEMENTADO_Y_VERIFICADO`, `IMPLEMENTADO_SIN_PRUEBAS`, `PARCIALMENTE_IMPLEMENTADO`, `DECLARADO_NO_IMPLEMENTADO`, `DESHABILITADO`, `OBSOLETO`, `PROPUESTO` o `NO_VERIFICADO`; cita rutas relativas y pruebas aprobadas cuando existan.
4. Actualiza `docs/domains/GPS.md`, `docs/domains/COLLARS.md`, `docs/domains/IOT_ARCHITECTURE.md`, `docs/domains/GPS_PROVIDER_MATRIX.md` y `docs/domains/GPS_LIMITATIONS.md`. Describe privacidad, dependencias, fallos y límites de cada flujo; revisa enlaces y diff.

## Coordinación

- Depende de `nala-feature-inventory` y `nala-architecture-review` bajo plan de `nala-product-auditor`; recomienda `nala-security-audit` para ubicación y ownership, y `nala-api-documenter` para rutas.
- Puede modificar solo los cinco documentos GPS/collares indicados y entradas propias en `docs/auditoria/DOCUMENTATION_CHANGELOG.md`; no modifica arquitectura general, infraestructura, documentación de salud ni código.
- Conserva contenido de otros autores; remite contradicciones al orquestador antes de sustituir y registra cambios en el changelog sin incluir secretos.
