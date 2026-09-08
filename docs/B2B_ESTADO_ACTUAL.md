# PawTrack CR — Estado B2B/B2G y Marketplace de Servicios

> Corte verificado: 2026-09-08
> Fuente técnica: `SubscriptionTier`, `SubscriptionPricing`, controllers, handlers y rutas frontend actuales.
>
> Este documento distingue capacidades implementadas de propuestas comerciales. Una capacidad no debe venderse como activa si aparece en `📋`.

## Resumen ejecutivo

PawTrack tiene seis superficies B2B/B2G activas o parcialmente activas:

| Superficie               | Estado actual                                                                      | Comercialización                                             |
| ------------------------ | ---------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| Clínicas veterinarias    | ✅ Directorio, mapa, escaneo QR/microchip, portal, visibilidad y funciones Partner | Tiers ClinicPlus/ClinicPartner existentes                    |
| Tiendas de mascotas      | ✅ Directorio, mapa, catálogo, pedidos, analytics y sedes                          | Tiers StorePlus/StorePartner existentes                      |
| Refugios/adopciones      | ✅ Publicación, solicitudes, ferias y dashboard base                               | ShelterPlus existente                                        |
| Municipalidades          | ✅ Capturas, estados, fotos, estadísticas y dashboard regional base                | Tiers municipales anuales existentes; billing/UI incompletos |
| Proveedores de servicios | ✅ Marketplace operativo sin tier comercial aprobado                               | No publicar comisión ni precio todavía                       |
| Aliados/organizaciones   | ✅ Registro, verificación y operación de alertas base                              | Modelo gratuito; roles/SLA avanzados pendientes              |

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

- Tier de suscripción específico para proveedores.
- Membresía mensual de ₡3,000–₡5,000, feature gates y cobro recurrente.
- Comisión aprobada, impuestos, conciliación financiera o factura electrónica.
- Integración de tarjeta/SINPE con un proveedor de pagos externo; el gateway actual es manual.
- Payout automático al proveedor.
- KYC empresarial, contratos digitales o firma criptográfica.
- Multi-sede y miembros/roles por organización de proveedor.
- SLA comercial, soporte por nivel y ranking pagado.
- Política definitiva por categoría para cancelaciones, no-show, domicilio y custodia.
- Moderación automática de reseñas, antifraude avanzado y verificación de licencias sectoriales.

### Modelo comercial propuesto, todavía no aprobado

La opción preferida para validar el mercado es una membresía mensual, porque
evita custodiar y repartir fondos por cada reserva durante el MVP:

| Nivel       | Precio sugerido | Incluye                                                   |
| ----------- | --------------: | --------------------------------------------------------- |
| Perfil base |          Gratis | Perfil y contacto básico                                  |
| Verificado  |      ₡3,000/mes | Documentos revisados, catálogo, disponibilidad y reservas |
| Destacado   |      ₡5,000/mes | Prioridad, badge y estadísticas                           |

Se recomienda probar 60–90 días gratis y medir conversión, reservas y churn.
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

| Producto                 | Backend/frontend actual                           | Gate o tier               | Gaps principales                                                    |
| ------------------------ | ------------------------------------------------- | ------------------------- | ------------------------------------------------------------------- |
| Clínica básica           | Registro, revisión, directorio, mapa y escaneo    | `ClinicBasic`/estado base | Perfil completo, horarios, consentimiento y revalidación documental |
| Clínica Plus             | Destacado, badge, estadísticas y alertas cercanas | `ClinicPlus`              | SLA y soporte formal                                                |
| Clínica Partner          | Certificados PDF, API key, widget y pasaporte     | `ClinicPartner`           | Scopes API, sandbox, firma criptográfica y multi-veterinario        |
| Tienda básica            | Perfil, catálogo, directorio y mapa               | `StoreBasic`/estado base  | Inventario y perfil comercial completo                              |
| Tienda Plus              | Pedidos in-app, SINPE/referencia y operación      | `StorePlus`               | Confirmación, reembolso, idempotencia comercial, impuestos y envío  |
| Tienda Partner           | Analytics y sedes                                 | `StorePartner`            | Exportaciones, inventario por sede y badge/ranking consistente      |
| Refugio básico           | Publicación y solicitudes de adopción             | `ShelterBasic`            | Roles, consentimiento y moderación avanzada                         |
| Refugio Plus             | Animales ilimitados, ferias y visibilidad         | `ShelterPlus`             | Billing y operación enterprise                                      |
| Municipalidad básica     | Capturas, estados, búsqueda y portal              | `MuniBasica` anual        | Compra, renovación, roles y aislamiento por municipio               |
| Municipalidad Full       | Fotos, estadísticas y reportes                    | `MuniFull` anual          | API pública, exportaciones oficiales y SLA                          |
| Red regional             | Multi-cantón y dashboard regional                 | `MuniRedRegional` anual   | Contrato, delegación y aislamiento cross-cantón                     |
| Aliado/refugio operativo | Alertas, cobertura y KPI base                     | Sin tier aprobado         | Miembros, permisos, auditoría y SLA                                 |
| Publicidad               | Vallas Map/Dashboard/Directory/Feed               | Sin catálogo aprobado     | Contratos, cobro, impresiones, CTR y moderación                     |
| API/widget               | API clínica y widget Partner                      | ClinicPartner             | Versionado público, scopes, sandbox y webhooks                      |
| Datos agregados          | No disponible como producto                       | Futuro                    | Anonimización, consentimiento, contratos y gobierno de datos        |

## Rutas principales

### Proveedores de servicios

- Público: `/servicios`, `/servicios/:id`, `/servicio/registro`.
- Operación autenticada: `/service-providers`, `/provider-bookings`, disponibilidad y verificación documental según el rol.
- API pública: `GET /api/public/service-providers` y detalle/servicios.
- API privada: `api/service-providers`, `api/provider-bookings`.
- Incidentes: `api/support/provider-incidents` para `Admin`/`Support`.

### Otras superficies

- Clínicas: `/clinicas`, `/clinica/registro`, portal `/clinica/portal`.
- Tiendas: `/tiendas`, `/tienda/registro`, pedidos y analytics.
- Adopciones/refugios: `/adopciones` y dashboards de refugio.
- Municipalidades: portal municipal y `api/municipalities`.
- Reportes de bienestar: `POST /api/public/welfare-cases`, disponible para reportes anónimos.

## Decisiones necesarias antes de vender B2B enterprise

1. Aprobar una sola matriz comercial y retirar precios contradictorios de documentos históricos.
2. Crear catálogo versionado de productos, moneda, impuestos, ciclo, factura y política de cambios.
3. Definir tenancy formal: organización, miembros, sedes, roles, invitaciones y aislamiento.
4. Completar billing real: idempotencia, webhooks, conciliación, reembolsos, payout y auditoría.
5. Publicar SLA, soporte, retención, exportación y procedimiento de incidentes por tier.
6. Ejecutar pruebas negativas cross-tenant, contratos API, concurrencia, DAST y pruebas de carga.
7. Crear datos sintéticos y cuentas demo para cada categoría y tier.

## Regla editorial

- `precios.md`: catálogo comercial aprobado o explícitamente marcado como propuesta.
- `pricing.md`: estrategia interna y roadmap; no usarlo como catálogo de capacidades activas.
- `featuresB2B.md`: matriz técnica de estado, no lista de trabajo histórico.
- `todolist-b2b-enterprise.md`: pendientes verificables, nunca marcar una hipótesis comercial como implementada.
