# PawTrack CR - Referencia API

**Estado:** activo  
**Corte general:** 2026-09-22; revalidacion focal de tiendas: 2026-10-02.
**Contrato:** OpenAPI `1.0`

## Fuentes

La especificacion ejecutable se publica en `/openapi/v1.json` solo en
Development/Local, segun [Program.cs](../backend/src/PawTrack.API/Program.cs).
Este documento organiza la superficie por modulo; no sustituye un contrato
generado en tiempo de ejecucion y contrastado con la revision actual.

## Reglas comunes

- Base URL local: `http://localhost:5199/api` cuando se usa `start-dev.ps1`.
- Enviar `Authorization: Bearer <access-token>` para endpoints autenticados.
- El token de acceso vive en memoria del frontend; la renovacion usa cookie
  httpOnly.
- Enviar `Api-Version: 1.0` en integraciones que requieran version explicita.
- Los errores siguen Problem Details o `Result` serializado; no exponen
  excepciones internas.
- Todas las mutaciones sensibles validan ownership, tenant, grant o scope,
  ademas del rol.

## Superficies principales

| Modulo                   | Prefijo                                    | Acceso principal                              |
| ------------------------ | ------------------------------------------ | --------------------------------------------- |
| Auth                     | `/auth`                                    | publico y usuario autenticado                 |
| Mascotas                 | `/pets`                                    | `Owner` propietario                           |
| Perdidas y avistamientos | `/lost-pets`, `/sightings`                 | propietario, participante o publico minimo    |
| Chat y seguridad         | `/chat`, `/safety`                         | participantes y actor validado                |
| Clinicas y salud         | `/clinics`, `/medical`, `/certificates`    | `Clinic`, `Owner`, grants y Partner           |
| Collares                 | `/collars`, `/collar-tags`                 | propietario, Admin o device key               |
| Tiendas                  | `/stores`, `/store-orders`                 | `Store` propietario o cliente propio          |
| Proveedores              | `/service-providers`, `/provider-bookings` | proveedor o cliente participante              |
| Municipalidades          | `/municipalities`                          | `Municipality` tenant o `Admin`               |
| Reportes                 | `/institutional-reports`, `/nala`          | scopes institucionales                        |
| Administracion           | `/admin`                                   | `Admin`; Support solo superficies especificas |

## Endpoints de referencia

### Auth y datos personales

- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/logout`
- `GET/PATCH /auth/me`
- `GET /auth/me/export`
- `DELETE /auth/me`
- `POST /auth/me/health-data-consent`
- `POST /auth/mfa/*`

Suscripciones de usuario:

- `DELETE /subscriptions/{id}` cancela la renovacion y conserva el plan hasta
  `ExpiresAt`.
- `POST /subscriptions/{id}/downgrade` programa `UserFamilia` a `UserPlus` en
  el vencimiento actual; requiere `targetTier: "UserPlus"`.
- `PUT /subscriptions/{id}/report-payment` informa el pago del plan pendiente;
  solo registra el aviso y no activa el plan. En tarjeta, la autorización no
  activa la suscripción hasta que se confirme el settlement.

### Recuperacion

- `GET /public/pets/{id}`
- `POST /pets`
- `POST /pets/{id}/report-lost`
- `GET/POST /lost-pets/*`
- `POST /sightings`
- `POST /found-pets`
- `POST /chat/*`
- `POST /handover/*`

### Clinicas y certificados

- `POST /clinics/register`
- `GET/PUT /clinics/me/*`
- `POST /clinics/scan`
- `POST /clinics/access-grants/*`
- `GET /clinics/patients/{petId}/medical` requiere un grant activo del dueño con permiso `read`; escanear el QR sólo identifica a la mascota.
- `POST /clinics/patients/medical` requiere grant activo con permiso `write`; el QR/chip no sustituye el consentimiento.
- `POST /pets/{petId}/clinic-access/code` inicia un grant del dueño; `POST /pets/{petId}/clinic-access/accept` acepta el código de la clínica; `DELETE /pets/{petId}/clinic-access/{clinicId}` revoca el acceso.
- `GET/POST /medical/*`
- `POST /certificates/*`
- `GET /public/certificates/{code}`

### Organizaciones

- `POST /stores/register`, `GET/PUT /stores/*`, `GET/POST/PUT/DELETE /stores/products/*`
- `POST /service-providers/register`, `GET/PUT /service-providers/*`
- `GET/POST /municipalities/captures/*`
- `GET/POST /allies/*`
- `GET/POST /adoptions/*`

### Familia

- `GET /family` devuelve los miembros activos, `pendingInvitations` y el máximo de invitaciones pendientes.
- `POST /family` requiere plan Familia activo.
- `POST /family/invite` requiere titular, cupo de `MaxFamilyMembers` y un máximo de 3 invitaciones pendientes; las invitaciones pendientes reservan asiento.
- `POST /family/invitations/{token}/accept` vuelve a comprobar identidad de correo, cuenta, plan y capacidad al aceptar.
- `DELETE /family/members/{memberId}` permite al titular retirar miembros; el dueño no puede borrarse a sí mismo.
- `GET /subscriptions/entitlements` informa el límite `MaxFamilyMembers`; el cliente no debe usar un límite fijo local.

### Tiendas: alcance actual

- `POST /store-orders`, `GET /store-orders/mine`, `GET /store-orders/incoming`
- `PUT /store-orders/{id}/confirm`, `PUT /store-orders/{id}/status`
- `POST /store-orders/{id}/report-payment` permite al cliente reportar un pago externo.
- `POST /store-orders/{id}/verify-payment` permite a la tienda registrar verificacion manual; no llama al banco ni procesa el pago.
- `POST /store-orders/{id}/record-external-refund` registra referencia de una devolucion ya ejecutada externamente; no envia fondos.
- `POST /store-orders` requiere el header `Idempotency-Key` (1-200 caracteres; ausente/inválido: 400). La misma clave y el mismo payload reproducen el pedido; la misma clave con payload distinto devuelve 409 `IDEMPOTENCY_KEY_CONFLICT`. La clave no va en el JSON del pedido.
- Al crear el pedido, el handler rechaza lineas con stock no declarado o insuficiente; no existe flujo de solicitud sin stock.
- Al aceptar, el codigo reserva `StockOnHand` disponible y una tarea vence/libera la reserva; esto no es un ledger ni stock productivo verificado.
- El pedido conserva snapshot de nombre/precio/cantidad al crearse; sigue pendiente reconfirmar cambios de precio antes de aceptar.
- La migracion `AddStoreOrderIdempotencyAndProviderRefundAccounting` declara el indice unico y los campos nuevos; esta generada, no aplicada ni verificada en un entorno compartido.
- `LocationId` es opcional en backend, pero checkout no permite seleccionar
  sede. No hay POS/caja, movimientos de inventario, compras ni pago/factura
  integrados para la tienda.

Roadmap: [ROADMAP_TIENDAS_USO_DIARIO.md](ROADMAP_TIENDAS_USO_DIARIO.md).

### Administración

- `/admin/allies`, `/admin/clinics`, `/admin/stores`
- `/admin/service-providers`, `/admin/subscription-plans`
- `PUT /admin/service-providers/payments/{paymentId}/external-refund` registra una devolución SINPE ya ejecutada fuera de PawTrack; es Admin-only, idempotente y auditable, pero no transfiere fondos. Pagos ligados a `PaymentIntent` usan el gateway.
- `/admin/collar-tags`, `/admin/promotions`, `/admin/billboards`
- `/admin/welfare-cases`, `/admin/audit`, `/admin/product-analytics`

## Seguridad de integraciones

La matriz de ownership, BOLA/IDOR y scopes esta en
[API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md). Las claves de
clinica requieren scope, vigencia y pertenencia a la clinica. No documentar ni
probar API keys reales en issues, logs o ejemplos.
