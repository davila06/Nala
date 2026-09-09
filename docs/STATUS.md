# PawTrack CR - Estado verificado

> Corte: 2026-09-09. Este documento es el estado operativo actual, no una
> promesa comercial.

## Resumen ejecutivo

PawTrack tiene una base funcional amplia y diferenciada para identidad de
mascotas, perdida y reunificacion, salud, red local, instituciones y
marketplaces. El nucleo tecnico compila y las suites verificadas recientes
estan verdes, pero el producto no debe etiquetarse como completamente
production-ready hasta cerrar calidad de entrega, claims comerciales,
observabilidad de negocio y validacion operativa.

## Evidencia tecnica reciente

- Backend solution build: correcto.
- Backend unit tests: 1340 correctos en la validacion del 2026-09-09.
- Backend integration tests: 102 correctos.
- Frontend typecheck: correcto.
- Frontend tests: 20 archivos, 53 correctos.
- Frontend lint: correcto, sin errores ni warnings.
- Frontend production build: correcto.
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

### P0

- Corregir claims de "production-ready", "SENASA-ready", tasas de recuperacion
  y certificaciones hasta contar con evidencia legal/operativa.
- Verificar y rotar cualquier secreto real en `secrets/sa_password.txt`; el
  archivo no debe contener credenciales utilizables ni estar versionado.
- Definir el funnel de activacion y recuperacion con eventos reales.
- Completar pruebas E2E del ciclo registro -> QR -> perdida -> avistamiento ->
  handover -> reunificacion.
- Cerrar la fuente unica de planes y responsabilidades comerciales.

### P1

- Lint frontend sin errores y gates obligatorios en CI.
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
