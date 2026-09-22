# Entitlements: estado de implementación

Este documento registra la implementación técnica inicial de la matriz de planes definida en `FEATURES.md`.

## Implementado

- `PlanEntitlement` configurable por plan y clave.
- `EntitlementConsumption` persistente con clave idempotente única.
- `SubscriptionAddon` persistente con vigencia, activación y suma automática sobre los límites del plan.
- CRUD administrativo protegido para crear, listar y desactivar add-ons.
- Índices SQL para idempotencia y agregación por ciclo.
- `IEntitlementService` para snapshot, autorización y consumo.
- Cuotas Free para mascotas, escaneos clínicos y productos de tiendas.
- Gates centralizados para mascotas, matching visual, escaneos clínicos, productos de tiendas, publicaciones de refugios y capturas municipales.
- Gates clínicos para API keys activas, certificados verificables y pasaportes, con conteos SQL por ciclo.
- Exportaciones médicas clínicas autorizadas y consumidas mediante `ClinicMedicalExportsPerCycle`, con fallback temporal de 20 por ciclo.
- Dominios de widget clínico persistentes, administrables y limitados a cinco activos por clínica; el endpoint público valida `Origin` cuando está presente.
- Auditoría de autorización y revocación de dominios de widget mediante `AuditLogEntry`.
- Downgrade programado `ClinicPartner -> ClinicPlus` conservando `ClinicId` y aplicando la expiración de capacidades Partner.
- Gate `MaxGpsCollars` en el registro de collares, con reemplazo permitido para la misma mascota.
- Gates B2C para `MaxFamilyMembers` y `MaxActiveLostCases`, con conteos SQL antes de mutaciones.
- Cuota `MaxActiveVetReminders` para Familia, contando recordatorios activos de todas sus mascotas mediante SQL.
- Cuota `MaxOrdersPerCycle` para StorePlus/StorePartner, contando pedidos por `PlacedAt` sin liberar cuota por cancelación.
- Difusión por lote con `BroadcastRunId` compartido por todos los canales y consumo diario por caso, idempotente por ejecución.
- Cuotas municipales `BulkUpdateLimit` para operaciones masivas y `InterCantonTransfersEnabled` para transferencias regionales.
- Auditoría de downgrades y ciclo de vida de add-ons asociada al administrador autenticado.
- Límite `MaxLocations` para StorePartner y `MaxScheduleBlocks` para proveedores.
- Cálculo puro de prorrateo de add-ons mediante `SubscriptionAddonProration`, con crédito por días no usados y monto mínimo cero.
- `PaymentTransaction` conserva monto bruto, crédito prorrateado y monto neto como campos auditables.
- El cobro recurrente consolida el precio de la suscripción y los `PriceCrc` de add-ons activos en una sola `PaymentTransaction` y una sola carga al gateway.
- La sustitución de add-ons calcula crédito prorrateado, desactiva el anterior y crea el reemplazo dentro de la misma unidad de trabajo.
- Los downgrades de tiendas aplican modo inactivo a productos y sedes excedentes, conservando los registros históricos.
- El job de expiración aplica modo inactivo a servicios publicados y elimina cantones adicionales al bajar una municipalidad a MuniBasica, conservando historial.
- Auditoría de activación por referencia de pago y pestaña Admin para consultar/desactivar add-ons.
- UI Admin integrada para add-ons y medidores con estados de carga, error, no incluido, disponible y agotado.
- Renovación persistente de add-ons activos junto con el cobro recurrente de la suscripción.
- UI Admin para autorizar y revocar dominios de widgets clínicos.
- `Idempotency-Key` aceptado en operaciones de matching, escaneo y captura.
- Problem Details `PLAN_LIMIT_REACHED` con límite, consumo, saldo y reinicio.
- Telemetría sin PII para decisiones denegadas y consumos aprobados.
- Endpoint autenticado `GET /api/subscriptions/entitlements`.
- Hook y medidor frontend basados en el snapshot.
- Precios del flujo de pago obtenidos del catálogo administrable, sin fallback de precios en frontend.

## Migración y despliegue

Las migraciones son:

- `AddEntitlementCatalogAndConsumption`
- `MakeEntitlementSubscriptionOptional`
- `AddSubscriptionAddons`
- `AddBroadcastRunId`

Aplicar en orden mediante el proceso normal de migraciones de la aplicación. No ejecutar semillas comerciales manuales fuera de las migraciones.

## Trabajo pendiente

- Añadir UI de estado para difusión por lote; salud y familia conservan gates de consentimiento/plan y sus cuotas principales ya están aplicadas.
- Añadir UI administrativa para gestionar dominios; la API administrativa ya está disponible.
- Completar pedidos, sucursales e importación de tiendas.
- Completar cuotas de sucursales e importación de tiendas.
- Completar entitlements persistidos para las membresías específicas de proveedores (`Free`, `Verified`, `Featured`); los gates actuales usan fallback controlado mientras ese catálogo se modela.
- Completar modo lectura específico para clínicas, proveedores y municipalidades con sus catálogos comerciales persistidos.
- Integrar `SubscriptionAddonProration` en facturación, renovación y persistencia comercial; el cálculo base ya está implementado.
- Añadir pruebas de integración del cobro recurrente consolidado y aplicar crédito de cambio de plan/add-on cuando exista una operación de sustitución.
- Resolver configuración de aislamiento del worker Vitest para `PublicPetProfilePage`; el test funcional pasa, pero el CLI local no acepta la opción de pool usada para aislarlo.
- Añadir precio, catálogo comercial y reglas de prorrateo/renovación para `SubscriptionAddon`.
- Añadir auditoría persistente de cambios de plan y decisiones comerciales.
- Añadir snapshot de consumo completo para cada plan y pruebas de integración por tenant.

## Regla de rollout

Durante la transición, los servicios legacy conservan fallback únicamente cuando no existe una definición persistida. Una vez aplicadas y verificadas ambas migraciones en todos los entornos, deben eliminarse esos fallbacks y bloquearse cualquier nuevo límite hardcodeado.
