# Guia de QA y E2E

**Estado:** activo  
**Audiencia:** QA, frontend y backend  
**Corte:** 2026-09-22

## Capas de prueba

- Unitarias: dominio, handlers y servicios.
- Integracion: API, persistencia, autorizacion y migraciones.
- Frontend: Vitest y Testing Library.
- E2E: Playwright contra backend real, SQL/Azurite y frontend preview.

## Preparacion local

1. `dotnet restore` y `dotnet build` desde `backend`.
2. Levantar SQL LocalDB/SQL Server y Azurite.
3. Ejecutar seeds documentados en [pruebas.md](pruebas.md).
4. Confirmar que el backend usa el entorno y base correctos.
5. `npm install` en `frontend`.
6. Ejecutar `npm test`, `npm run typecheck`, `npm run lint` y `npm run build`.
7. Ejecutar `npm run test:e2e` con el backend ya disponible.

## Flujos criticos

- registro, verificacion y login;
- crear mascota y QR publico;
- reportar perdida y avistamiento;
- chat enmascarado y handover;
- collar: activacion, GPS, zona segura y modo perdido;
- coordinación: dos participantes autorizados, consentimiento explícito,
  sharing aproximado por defecto, roster de destinatarios y stop-sharing;
- clinica: scan, grant medico y certificado;
- tienda: catalogo, pedido, reserva temporal al aceptar y reporte/verificacion
  manual de pago externo estan presentes en codigo. El flujo no debe describirse
  como POS: no procesa ni liquida fondos, no sincroniza ventas presenciales y no
  ofrece kardex/caja/factura fiscal. Migracion aplicada y despliegue
  `NO_VERIFICADO`;
- proveedor: servicio, agenda y reserva;
- municipalidad: captura, estados y reportes;
- Admin/Support: aprobaciones, bienestar e incidentes.

## Regresiones obligatorias

Cada cambio de autorizacion requiere prueba BOLA/IDOR. Cada cambio de tier
requiere prueba con suscripcion activa, vencida y ausente. Cada cambio de
migracion requiere base vacia o upgrade incremental. Cada endpoint de archivo
requiere limite de tamano y content type.

### Gate E2E del piloto hibrido de tienda

La prueba [StoreOrderIdempotencySqlTests](../backend/tests/PawTrack.IntegrationTests/Stores/StoreOrderIdempotencySqlTests.cs)
se ejecutó localmente en Windows/LocalDB y pasó 1/1: crea una base temporal,
aplica la migración desde un pedido legacy y comprueba que una sola de dos
inserciones concurrentes con la misma clave gana. Esto no prueba migración en
`PawTrackDev`, staging o producción. La integración HTTP
[StoreOrdersIdempotencyEndpointsTests](../backend/tests/PawTrack.IntegrationTests/Stores/StoreOrdersIdempotencyEndpointsTests.cs)
pasó 1/1 contra la factory in-memory: replay 201, conflicto 409, aceptación,
reporte/verificación manual de pago, preparación, rechazo del cruce
Pickup→OutForDelivery, retiro completado y eventos Outbox. No prueba SQL. Antes
de presentar el stock como confiable para el piloto, agregar E2E que cubra
alta/ajuste de inventario, solicitud repetida con la misma `Idempotency-Key`,
aceptación con precio/stock vigentes, reserva/liberación concurrente,
expiración, cancelación/rechazo y dos solicitudes por las últimas unidades.
Para el flujo híbrido probar además que el cobro sigue en POS externo,
el reporte del cliente no se transforma en pago confirmado y la devolucion se
registra solo despues de gestionarla fuera. Completar E2E de estados por tipo de
entrega, notificaciones y aislamiento por tienda. Existe una spec Playwright
opt-in para replay/conflicto contra backend con seed StorePartner, pero no se
ejecutó; los tests existentes no acreditan la jornada completa ni integración
POS. Ver
[ROADMAP_TIENDAS_USO_DIARIO.md](ROADMAP_TIENDAS_USO_DIARIO.md).

## Datos de prueba

Usar usuarios y referencias de [pruebas.md](pruebas.md). No usar credenciales
reales ni bases productivas. Las suscripciones deben tener `ExpiresAt` futuro y
los seriales unicos por prueba.

La validación con credenciales reales de WhatsApp, GPS, pagos, Azure y correo
se realiza solo en staging/producción controlada siguiendo
[EXTERNAL_PROVIDER_VALIDATION.md](EXTERNAL_PROVIDER_VALIDATION.md).
