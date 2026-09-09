# PawTrack CR — Estado B2B/B2G y Marketplace de Servicios

> Corte verificado: 2026-09-09
> Fuente técnica: `SubscriptionTier`, `SubscriptionPricing`, controllers, handlers y rutas frontend actuales.
>
> Este documento distingue capacidades implementadas de propuestas comerciales. Una capacidad no debe venderse como activa si aparece en `📋`.

## Resumen ejecutivo

PawTrack tiene seis superficies B2B/B2G activas o parcialmente activas:

| Superficie               | Estado actual                                                                      | Comercialización                                                  |
| ------------------------ | ---------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| Clínicas veterinarias    | ✅ Directorio, mapa, escaneo QR/microchip, portal, visibilidad y funciones Partner | Tiers ClinicPlus/ClinicPartner existentes                         |
| Tiendas de mascotas      | ✅ Directorio, mapa, catálogo, pedidos, analytics y sedes                          | Tiers StorePlus/StorePartner existentes                           |
| Refugios/adopciones      | ✅ Publicación, solicitudes, ferias y dashboard base                               | ShelterPlus existente                                             |
| Municipalidades          | ⏸️ Módulo diferido; capacidades existentes fuera del alcance activo                | Tiers técnicos conservados; no implementar ni vender autoservicio |
| Proveedores de servicios | ✅ Marketplace operativo con tiers técnicos y prueba de 30 días                    | No publicar comisión ni precio recurrente todavía                 |
| Aliados/organizaciones   | ✅ Registro, verificación y operación de alertas base                              | Modelo gratuito; roles/SLA avanzados pendientes                   |

## Marketplace de proveedores de servicios

### Categorías soportadas

Las categorías canónicas del dominio son:

- `Trainer` — Adiestradores
- `Groomer` — Groomers / peluquería canina
- `Hotel` — Hoteles para mascotas
- `Daycare` — Guarderías
- `Walker` — Paseadores
- `Photographer` — Fotógrafos de mascotas
- `Other` — Otros servicios

No crear categorías paralelas en documentación, frontend o billing sin cambiar primero el enum de dominio y sus contratos.

### Implementado

- Perfiles: registro público, estado pendiente, edición autenticada y
  publicación pública condicionada al estado del proveedor.
- Campos de perfil: nombre comercial, descripción, categoría, dirección,
  coordenadas públicas, correo, teléfono, website y logo cuando corresponde.
- Registro público de proveedor con aprobación administrativa.
- Perfil con nombre, descripción, categoría, dirección, coordenadas, contacto, website y logo.
- Directorio público filtrable por categoría, modalidad, rango de precio y radio geográfico.
- Catálogo de servicios publicados, pausados o archivados.
- Modalidades: establecimiento, domicilio, virtual, grupal y estadía nocturna.
- Duración, precio CRC y capacidad por servicio.
- Reglas semanales de disponibilidad y bloqueos excepcionales.
- Reservas con control de capacidad y prueba de concurrencia en SQL Server.
- Estados de reserva: solicitada, pago pendiente, confirmada, en progreso, completada, cancelada, no-show, expirada, disputada y reembolsada.
- Snapshot comercial de la reserva: precio, impuestos, fee de plataforma, total y política aplicable.
- Pago manual/SINPE con idempotencia, reporte, confirmación, expiración, disputa y reembolso.
- Verificación documental privada, revisión administrativa, vencimiento y recordatorios de renovación.
- Auditoría de acciones y notificaciones operativas.
- Incidentes de bienestar, seguridad, política, pago, privacidad u otros, con investigación, resolución, apelación y cierre.
- Rol `Support` para operaciones de incidentes, separado de `Admin`.
- Directorio y marcadores en el mapa público.

### No implementado o no aprobado comercialmente

- Precio comercial recurrente específico para proveedores.
- Cobro recurrente, checkout comercial, impuestos y facturación para proveedores.
- Comisión aprobada, impuestos, conciliación financiera o factura electrónica.
- Integración de tarjeta/SINPE con un proveedor de pagos externo; el gateway actual es manual.
- Payout automático al proveedor.
- KYC empresarial, contratos digitales o firma criptográfica.
- Multi-sede y miembros/roles por organización de proveedor.
- SLA comercial, soporte por nivel y ranking pagado.
- Política definitiva por categoría para cancelaciones, no-show, domicilio y custodia.
- Moderación automática de reseñas, antifraude avanzado y verificación de licencias sectoriales.

### Modelo comercial propuesto, todavía no aprobado

El gate técnico ya existe y no debe confundirse con una suscripción comercial:
`Free` permite directorio y contacto; `Verified` y `Featured` habilitan
catálogo, disponibilidad y reservas. La activación inicial concede una prueba
única de 30 días en `Verified`; el job de expiración devuelve el proveedor a
`Free`, salvo una asignación manual del administrador.

La opción preferida para validar el mercado es una membresía mensual, porque
evita custodiar y repartir fondos por cada reserva durante el MVP:

| Nivel       | Precio sugerido | Incluye                                                   |
| ----------- | --------------: | --------------------------------------------------------- |
| Perfil base |          Gratis | Perfil y contacto básico                                  |
| Verificado  |      ₡3,000/mes | Documentos revisados, catálogo, disponibilidad y reservas |
| Destacado   |      ₡5,000/mes | Prioridad, badge y estadísticas                           |

La implementación actual usa una prueba única de 30 días y debe medirse
conversión, reservas y churn antes de aprobar la tarifa comercial.
También puede validarse un único plan de ₡3,990/mes antes de separar niveles.
Esta es una propuesta de producto, no una capacidad activa.

Las comisiones de `pricing.md` son hipótesis y no deben presentarse como precios activos:

| Servicio          | Propuesta histórica | Estado                                  |
| ----------------- | ------------------: | --------------------------------------- |
| Pet sitting       |                 12% | 📋 Pendiente aprobación legal/comercial |
| Paseos            |                 12% | 📋 Pendiente aprobación legal/comercial |
| Custodia temporal |                 10% | 📋 Pendiente aprobación legal/comercial |
| Grooming          |                  8% | 📋 Pendiente aprobación legal/comercial |

Antes de activar cobros deben definirse custodía de fondos, impuestos, reembolsos, disputas, KYC, payout y responsabilidad del proveedor.

## Matriz B2B/B2G actual

| Producto                 | Backend/frontend actual                                                                                        | Gate o tier               | Gaps principales                                                        |
| ------------------------ | -------------------------------------------------------------------------------------------------------------- | ------------------------- | ----------------------------------------------------------------------- |
| Clínica básica           | Registro, revisión, perfil público editable, directorio paginado, mapa y escaneo                               | `ClinicBasic`/estado base | Revalidación documental y UI de gestión de consentimientos              |
| Clínica Plus             | Destacado, badge, estadísticas y alertas cercanas                                                              | `ClinicPlus`              | SLA y soporte formal                                                    |
| Clínica Partner          | Certificados PDF firmables, API key con scopes, widget, pasaporte, agenda, export clínico y contrato `/api/v1` | `ClinicPartner`           | UI de agenda, PKI gestionada por Key Vault y atribución operativa       |
| Tienda básica            | Perfil, catálogo, directorio y mapa                                                                            | `StoreBasic`/estado base  | Inventario y perfil comercial completo                                  |
| Tienda Plus              | Pedidos in-app, SINPE/referencia y operación                                                                   | `StorePlus`               | Confirmación, reembolso, idempotencia comercial, impuestos y envío      |
| Tienda Partner           | Analytics, sedes y exportacion CSV con cuota/auditoria                                                         | `StorePartner`            | Inventario por sede y badge/ranking consistente                         |
| Refugio básico           | Publicación y solicitudes de adopción                                                                          | `ShelterBasic`            | Roles, consentimiento y moderación avanzada                             |
| Refugio Plus             | Animales ilimitados, ferias y visibilidad                                                                      | `ShelterPlus`             | Billing y operación enterprise                                          |
| Municipalidad básica     | Capacidades técnicas existentes; módulo diferido                                                               | `MuniBasica` anual        | No implementar compra, renovación ni ampliación durante el diferimiento |
| Municipalidad Full       | Capacidades técnicas existentes; módulo diferido                                                               | `MuniFull` anual          | No implementar API pública ni exportaciones oficiales ahora             |
| Red regional             | Capacidades técnicas existentes; módulo diferido                                                               | `MuniRedRegional` anual   | No implementar contrato, delegación ni aislamiento adicional ahora      |
| Aliado/refugio operativo | Alertas, cobertura y KPI base                                                                                  | Sin tier aprobado         | Miembros, permisos, auditoría y SLA                                     |
| Publicidad               | Vallas Map/Dashboard/Directory/Feed                                                                            | Sin catálogo aprobado     | Contratos, cobro, impresiones, CTR y moderación                         |
| API/widget               | API clínica, widget Partner, sandbox, scopes y webhooks salientes                                              | ClinicPartner             | Observabilidad Azure y contratos de Stores pendientes                   |
| Datos agregados          | No disponible como producto                                                                                    | Futuro                    | Anonimización, consentimiento, contratos y gobierno de datos            |

## Rutas principales

### Proveedores de servicios

- Público: `/servicios`, `/servicios/:id`, `/servicio/registro`.
- Operación autenticada: `/servicio/portal`, `/servicio/portal/servicios`, `/servicio/portal/reservas` y `/servicio/portal/verificacion`.
- API pública: `GET /api/public/service-providers` y detalle/servicios.
- API privada: `api/service-providers`, `api/provider-bookings`.
- Incidentes: `api/support/provider-incidents` para `Admin`/`Support`.

### Otras superficies

- Clínicas: `/clinicas`, `/clinica/registro`, portal `/clinica/portal`.
- Tiendas: `/tiendas`, `/tienda/registro`, pedidos y analytics.
- Adopciones/refugios: `/adopciones` y dashboards de refugio.
- Municipalidades: portal municipal y `api/municipalities` existentes, fuera del alcance activo por decisión de producto.
- Reportes de bienestar: `POST /api/public/welfare-cases`, disponible para reportes anónimos.

## Decisiones necesarias antes de vender B2B enterprise

1. Aprobar una sola matriz comercial y retirar precios contradictorios de documentos históricos.
2. Crear catálogo versionado de productos, moneda, impuestos, ciclo, factura y política de cambios.
3. Definir tenancy formal: organización, miembros, sedes, roles, invitaciones y aislamiento.
4. Completar billing real: idempotencia, webhooks, conciliación, reembolsos, payout y auditoría.
5. Publicar SLA, soporte, retención, exportación y procedimiento de incidentes por tier.
6. Ejecutar pruebas negativas cross-tenant, contratos API, concurrencia, DAST y pruebas de carga.
7. Crear datos sintéticos y cuentas demo para cada categoría y tier.

## Seguridad y operación verificadas 2026-09-09

- WebAuthn/passkeys: registro, autenticación, desafíos con TTL distribuido,
  almacenamiento SQL, contador anti-replay y revocación individual.
- Store Partner: export CSV con autorización, cuota de 20/mes y auditoría.
- SQL Server: carrera de registro WebAuthn con índice único, validada 1/1 en
  `PawTrackDev`.
- Playwright B2B: aislamiento negativo de consumidor frente a analytics Store
  y API keys Clinic validado 1/1 contra backend real; export Store Partner queda
  como escenario opt-in porque requiere seed y suscripción enterprise activa.
- Smoke de integración B2B: aislamiento Store/Clinic validado 1/1 con
  `WebApplicationFactory`; el smoke Playwright live también pasó 1/1.
- Azure: Bicep compilado con Key Vault purge protection, clave RSA dedicada,
  Managed Identity, RBAC criptográfico, Application Insights y diagnósticos de
  Container App. El despliegue real y la ventana SLO todavía requieren
  `what-if`, aprobación de resource group y observación en Azure Monitor.

## Regla editorial

- `precios.md`: catálogo comercial aprobado o explícitamente marcado como propuesta.
- `pricing.md`: estrategia interna y roadmap; no usarlo como catálogo de capacidades activas.
- `featuresB2B.md`: matriz técnica de estado, no lista de trabajo histórico.
- `todolist-b2b-enterprise.md`: pendientes verificables, nunca marcar una hipótesis comercial como implementada.
