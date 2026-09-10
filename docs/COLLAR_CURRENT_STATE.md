# Estado Actual de Collares GPS

**Estado:** activo  
**Corte:** 2026-09-09

## Implementado

- Activacion por CollarTag/serial y vinculacion a mascota.
- Device key protegida y validacion de ownership.
- Integracion Tractive y proveedor generico; Kippy no es integracion activa.
- Ingesta/polling de ubicacion.
- Modo perdido sincronizado con `LostPetEvent` y mapa publico.
- Alertas offline y bateria con cooldown.
- Zonas seguras y eventos de salida/regreso.
- Historial de ubicacion y export CSV con ventana de retencion.
- Handover seguro separado del handover de reunificacion.
- Auditoria de ciclo de vida y dashboard de inventario Admin.
- Locks distribuidos para jobs de escala.

## Flujos

Owner: `/collars/activate`, `/collars/handover` y pestaña GPS de `/pets/:id`.
Admin: inventario y metricas de CollarTags. Dispositivo: endpoint de ingest con
credencial, limites y validacion de serial.

## Retenciones y alertas

El historial crudo se conserva 30 dias salvo cambios de politica. El polling
usa 30 segundos para collar en modo perdido y 5 minutos en modo normal. Offline
y bateria tienen cooldown para evitar notificaciones repetidas.

## Proveedores

Tractive es el proveedor soportado con OAuth2. Un OEM futuro debe cumplir el
contrato de ingesta, seguridad de device key, precision, webhook/polling,
retencion y pruebas BOLA. Ver [GUIA_INTEGRACIONES_WEBHOOKS.md](GUIA_INTEGRACIONES_WEBHOOKS.md).

## No confundir

`CollarHandoverCode` transfiere un collar GPS. `HandoverCode` del modulo Safety
confirma la entrega de una mascota. Son conceptos y bounded contexts distintos.

El detalle historico y el roadmap se conservan en
[collarFinal.md](collarFinal.md) y
[COLLAR_IMPLEMENTATION_PLAN.md](COLLAR_IMPLEMENTATION_PLAN.md).
