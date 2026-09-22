# PawTrack CR - Estado verificado

> Corte: 2026-09-22. Este documento es el estado operativo actual, no una
> promesa comercial.

La documentacion operativa por rol fue contrastada con el router frontend,
`UserRole` y los controllers actuales. La entrada oficial es
[docs/README.md](README.md); los manuales de `Owner`, `Ally`, `Admin`, `Clinic`,
`Municipality`, `Store`, `ServiceProvider` y `Support` estan bajo
[Manuales/](Manuales/).

## Resumen ejecutivo

PawTrack tiene una base funcional amplia y diferenciada para identidad de
mascotas, perdida y reunificacion, salud, red local, instituciones y
marketplaces. El nucleo tecnico compila y las suites verificadas recientes
estan verdes, pero el producto no debe etiquetarse como completamente
production-ready hasta cerrar calidad de entrega, claims comerciales,
observabilidad de negocio y validacion operativa.

## Evidencia tecnica reciente

- Backend solution build: correcto.
- Backend unit tests: 1528 correctos.
- Backend integration tests: 112 correctos.
- Frontend typecheck: correcto.
- Frontend tests: 33 archivos, 104 correctos.
- Frontend lint: correcto, sin errores ni warnings.
- Frontend production build: correcto.
- Migraciones: `AddFirstResponseEventIdempotency` y
  `AddSearchLocationSharingSessions` aplicadas en `PawTrackDev`.
- Expiración de sharing SignalR: sesiones persistentes sin coordenadas, TTL Redis,
  job distribuido y auditoría autónoma de `SearchLocationSharingExpired`.
- Proveedores externos: gate `scripts/Test-ExternalProviderReadiness.ps1` exige
  evidencia redactada de contrato/aprobación y prueba para Azure, WhatsApp, GPS,
  pagos y correo antes de un go-live; no lee ni registra secretos.
- Funnel P0: eventos frontend iniciales instrumentados con `eventId`, timestamp e
  identidad anónima para registro de mascota,
  completitud básica, generación/escaneo de QR, reporte de pérdida, avistamiento,
  mascota encontrada, handover y reunificación; ingestión, deduplicación SQL,
  retención, consulta y dashboard Admin ya implementados. Exportación partner y
  E2E adicionales de broadcast/export partner pendientes.
- Export CSV del funnel agregado para Admin disponible sin PII, con rango
  máximo de 366 días y filtro opcional por cantón.
- E2E recovery: escenario Playwright principal pasa 1/1 contra backend, SQL/
  migraciones, Azurite y frontend preview local; cubre perdida, QR, chat
  enmascarado, broadcast y handover. Store Partner analytics export ya tiene
  gate, cuota mensual de 20 y auditoria; falta E2E dedicado.
- Adopciones: 11 pruebas de integración verifican directorio, detalle, ferias,
  solicitudes y flujos autorizados de shelter; el alcance no incluye pagos ni
  custodia financiera.
- Supply chain: workflow CI agregado para Gitleaks, npm audit de producción y
  vulnerabilidades transitivas de NuGet; falta observar su primera ejecución CI.
- Enterprise clinic/auth slice: TOTP MFA with protected secrets and one-time
  recovery codes; privileged-role MFA policy is enabled outside Development;
  authenticated rate limits partition by API key/user with IP fallback; clinic
  operations UI covers veterinarian permissions and scheduling; medical retention
  purges superseded versions and expired export metadata. Public certificate
  verification now validates the detached PDF signature when Key Vault or a
  configured development public key is available.
- Integracion real de pagos: no forma parte del alcance de tiendas actual.
- Tiendas: pedidos comunicados por PawTrack; la tienda controla disponibilidad,
  aceptacion, rechazo, entrega y estados.

Las cifras deben regenerarse en CI; no copiar numeros de una auditoria antigua
sin volver a ejecutar los comandos.

## Capacidades activas comprobadas

### Recuperacion

- Perfil publico, QR y escaneo.
- Reporte de perdida, Case Room y avistamientos.
- Matching visual y geografia.
- Mapa publico, zonas de busqueda y coordinacion SignalR.
- Broadcast multicanal.
- Chat enmascarado y handover seguro.
- Recompensas, fraude y modo offline.

### Salud

- Expediente medico y consentimiento de datos de salud.
- Recordatorios, alertas y exportacion.
- Clinicas verificadas, grants y certificados.
- API y widgets para planes Partner.

### Red y ecosistema

- Aliados, refugios, adopciones y ferias.
- Municipalidades y reportes institucionales.
- Tiendas y marketplace de servicios.
- GPS/collares, bundles, suscripciones e incentivos.

## Riesgos abiertos que bloquean escala

### P0 cerrados en este corte

- Claims comerciales principales revisados y brief de patrocinio consolidado en
  `docs/sponsor.md`; no se publican cifras de tracción sin fuente.
- `secrets/sa_password.txt` existe solo como secreto local y no está versionado.
- Funnel de recuperación correlacionado por `lostEventId`, con medianas SQL y
  `FirstResponseRecorded` idempotente.

### P0/P1 pendientes operativos

- Primera ejecución CI de Gitleaks, npm audit y NuGet audit.
- Completar evidencia real y vigente de contratos/cuentas de Azure, WhatsApp,
  GPS, pagos y correo mediante el gate de proveedores; esta evidencia requiere
  operadores autorizados y no puede sustituirse por configuración local.
- Prueba E2E de dos clientes SignalR para validar visualmente el roster de
  destinatarios; las pruebas unitarias del hub y el contrato backend ya pasan.
- Activación de Azure Monitor, alertas y ventana real de medición SLO.

### P1

- Gates obligatorios de lint/build/tests en CI.
- Contrato API versionado o verificado automaticamente.
- Pruebas BOLA/IDOR uniformes en dominios sensibles.
- Observabilidad de negocio: activacion QR, reportes, tiempo a primer avistamiento,
  tiempo a reunificacion, densidad territorial y conversion por canal.
- Runbooks y SLA para clinicas, municipalidades, aliados y proveedores.
- Runbook SLO tecnico agregado en `docs/OBSERVABILITY_SLO_RUNBOOK.md`; la
  activacion de Azure Monitor, alertas y ventana real de medicion sigue pendiente.
- Validacion de pagos y reembolsos solo donde el producto realmente custodie o
  confirme fondos; tiendas actuales quedan fuera.

### P2

- Simplificar navegacion en cuatro modos: Mi mascota, Perdida/Encontrada,
  Salud y Red local.
- Consolidar migraciones, jobs, retenciones y bounded contexts en documentacion.
- Instrumentar experimentos de onboarding y notificaciones.
- Preparar interoperabilidad LATAM despues de probar densidad en Costa Rica.

## Fuente tecnica de tiers y capacidades

La autoridad tecnica es el codigo en:

- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs`
- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs`
- `backend/src/PawTrack.Infrastructure/Subscriptions/SubscriptionService.cs`
- handlers y validators de cada modulo.

La documentacion comercial consolidada esta en [PRICING_AND_PLANS.md](PRICING_AND_PLANS.md).

## Regla de estado

Una capacidad solo puede marcarse como activa si tiene implementacion, control
de autorizacion, persistencia cuando aplique, prueba y evidencia de operacion.
Una propuesta comercial, una migracion existente o un enum no constituyen por
si solos una capacidad vendible.
