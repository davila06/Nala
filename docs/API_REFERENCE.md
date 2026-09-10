# PawTrack CR - Referencia API

**Estado:** activo  
**Corte:** 2026-09-09  
**Contrato:** OpenAPI `1.0`

## Fuentes

La especificacion ejecutable se publica en `/openapi/v1.json` cuando la API esta
encendida. Este documento organiza la superficie por modulo; no sustituye el
contrato generado.

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
- `GET/POST /medical/*`
- `POST /certificates/*`
- `GET /public/certificates/{code}`

### Organizaciones

- `POST /stores/register`, `GET/PUT /stores/*`, `GET/POST/PUT/DELETE /stores/products/*`
- `POST /service-providers/register`, `GET/PUT /service-providers/*`
- `GET/POST /municipalities/captures/*`
- `GET/POST /allies/*`
- `GET/POST /adoptions/*`

### Administración

- `/admin/allies`, `/admin/clinics`, `/admin/stores`
- `/admin/service-providers`, `/admin/subscription-plans`
- `/admin/collar-tags`, `/admin/promotions`, `/admin/billboards`
- `/admin/welfare-cases`, `/admin/audit`, `/admin/product-analytics`

## Seguridad de integraciones

La matriz de ownership, BOLA/IDOR y scopes esta en
[API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md). Las claves de
clinica requieren scope, vigencia y pertenencia a la clinica. No documentar ni
probar API keys reales en issues, logs o ejemplos.
