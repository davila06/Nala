# PawTrack CR - Matriz de autorización BOLA/IDOR

> Estado: activo para revisión de seguridad. Corte: 2026-09-09.
>
> Regla: autenticación y rol no sustituyen ownership. Cada endpoint debe validar
> que el actor puede acceder al recurso concreto y que la respuesta minimiza PII.

## Leyenda

- `Public`: sin sesión; solo DTO mínimo y datos no sensibles.
- `Owner`: propietario del recurso o usuario explícitamente autorizado.
- `Participant`: participante de la conversación/caso.
- `Role + owner`: rol requerido más ownership del tenant/recurso.
- `Admin`: administración global, con auditoría.
- `Partner`: API key válida, activa, expirada/revocada comprobada y scope correcto.

## Matriz crítica

| Superficie                        | Lectura pública           | Lectura autenticada                  | Mutación           | PII/riesgo               | Prueba requerida                                                   |
| --------------------------------- | ------------------------- | ------------------------------------ | ------------------ | ------------------------ | ------------------------------------------------------------------ |
| Perfil QR `/api/public/pets/{id}` | Public, DTO mínimo        | Igual                                | Ninguna            | No dueño/teléfono exacto | ID enumerable, minimización                                        |
| Contacto de pérdida               | Relay anónimo             | Owner recibe aviso interno           | Finder envía msg   | Sin teléfono expuesto    | Rate limit + active event + PII minimization                       |
| Mascotas `/api/pets/{id}`         | No                        | Owner                                | Owner              | Perfil, microchip, fotos | IDOR read/write                                                    |
| Reporte pérdida                   | No                        | Owner                                | Owner              | Contacto y coordenadas   | Pet ownership                                                      |
| Avistamientos                     | DTO público mínimo        | Owner/ally autorizado según caso     | Actor validado     | GPS/foto                 | Caso cruzado                                                       |
| Case Room                         | No                        | Owner                                | Owner              | PII, alertas, chat       | LostEvent ownership                                                |
| Chat                              | No                        | Participant                          | Participant        | Mensajes/contacto        | Thread membership; owner resolved server-side from LostPetEvent    |
| Handover                          | No                        | Owner genera / participante verifica | Estado válido      | Código sensible          | Brute force/replay                                                 |
| Expediente médico                 | No                        | Owner/grant clinic/admin             | Grant + permiso    | Datos de salud           | Tenant/grant matrix; QR/chip no puede override PetId; export scope |
| Certificados                      | Público solo verificación | Clinic owner/admin/owner pet         | Clinic/admin       | Documentos               | Clinic/pet ownership                                               |
| Collares/GPS                      | No                        | Pet owner/admin/device key           | Owner/device       | Ubicación exacta         | Pet/collar binding                                                 |
| Tiendas/productos                 | Catálogo público          | Store owner                          | Store owner        | Datos comerciales        | Store ownership                                                    |
| Pedidos tienda                    | No                        | Customer own / Store own             | Actor según estado | Dirección/notas          | Cross-customer/store                                               |
| Proveedores/reservas              | Directorio mínimo         | Customer/provider participant        | Participant        | Dirección/agenda         | Booking ownership                                                  |
| Municipalidades                   | DTO público mínimo        | Municipality tenant/admin            | Tenant/admin       | Capturas/PII             | Institution isolation                                              |
| NALA/reportes                     | No                        | Role + scope                         | Role + scope       | Datos agregados          | Scope/canton isolation                                             |
| Product funnel                    | No                        | Admin                                | No                 | Datos agregados          | Admin-only + range                                                 |
| Export partner                    | No                        | Partner scope                        | No                 | Agregado                 | Scope + suppression                                                |

## Reglas obligatorias

- [ ] Toda consulta por `Guid` debe tener predicate de ownership/tenant en SQL o
      una comprobación equivalente antes de devolver datos.
- [ ] No confiar únicamente en `Authorize(Roles = ...)` para recursos de usuario.
- [ ] No aceptar `ownerUserId`, `clinicId`, `storeId`, `petId` o `tenantId` del
      cliente como autoridad; resolver el actor desde claims y repositorio.
- [ ] Todas las mutaciones sensibles deben registrar actor, recurso, resultado y
      correlation ID.
- [ ] Los endpoints públicos deben tener rate limit, payload limit y DTO mínimo.
- [ ] Los exports deben aplicar scopes, rango máximo, supresión de grupos pequeños
      y auditoría.
- [ ] Los códigos de handover, tokens, API keys y documentos nunca se incluyen
      en logs o métricas de producto.

## Casos BOLA mínimos para CI

- [ ] Owner A no puede leer ni mutar mascota de Owner B.
- [x] Cliente A no puede leer ni mutar pedido de Cliente B.
- [x] Tienda A no puede leer ni cambiar pedido de Tienda B.
- [x] Clínica A no puede leer expediente sin grant activo del propietario ni
      si `ClinicUserId` no pertenece al `ClinicId` solicitado.
- [x] QR/chip clínico no puede combinarse con `PetId` ajeno para forzar lectura
      ni escritura sobre otra mascota.
- [x] Las rutas clínicas de pacientes aplican scopes de API key; el export exige
      `medical:export` antes de llegar al controller.
- [x] Una API key con `medical:read` no puede usar `medical:export`.
- [x] Proveedor A no puede leer reserva de Proveedor B ni mutar reservas,
      pagos o incidentes de otro proveedor/cliente.
- [x] Municipio A no puede mutar la captura registrada por Municipio B.
- [x] Export institucional rechaza `organizationId` ajeno; solo acepta la propia
      organización municipal o un actor Admin, y rechaza actores sin perfil
      municipal o sin cantón cuando solicitan alcance institucional.
- [x] Funnel de producto y catálogos/reportes Admin-Nala son inaccesibles para
      Owner/Store/Clinic/Provider/Municipality; scopes privilegiados requieren
      Admin en controller y servicio.
- [ ] Export partner fuera de scope devuelve 403 y no filtra filas.

## Control ya implementado

`OpenChatThreadCommand` recibe solo `LostPetEventId` e `InitiatorUserId` y
resuelve `OwnerId` desde la base de datos. No acepta `ownerUserId` del cliente;
esto evita forjar el dueño de un hilo. Debe mantenerse cubierto por una prueba
de regresión BOLA.

## Versionado OpenAPI

- Versión canónica actual: `1.0`.
- El cliente puede enviar `Api-Version: 1.0`.
- Los breaking changes deben introducir `/api/v2` o una nueva versión de header.
- Toda versión debe publicar contrato, changelog, scopes y ejemplos.
- CI debe verificar que los endpoints públicos no cambian silenciosamente.
- El workflow E2E publica `openapi-v1-{sha}` y verifica la superficie crítica.
- `ProductAnalyticsController` declara explícitamente `ApiVersion("1.0")`;
  los endpoints incompatibles deben vivir bajo una nueva versión.

El export partner permanece bloqueado hasta definir una identidad M2M canónica
(API key/tenant), scopes y rotación. Un header libre no es autorización válida.
