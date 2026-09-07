# Operabilidad - Proveedores de Servicios

## Estados operativos

- Proveedor: `Pending`, `Active`, `Rejected`, `Suspended`.
- Servicio: `Published`, `Paused`, `Archived`.
- Reserva: `Requested`, `Confirmed`, `InProgress`, `Completed`,
  `CancelledByCustomer`, `CancelledByProvider`, `NoShow`, `Expired`.
- Verificacion: `Pending`, `Verified`, `Rejected`, `Expired`.

## Procesos recurrentes

| Proceso                      | Frecuencia      | Lock                                  | Accion                                           |
| ---------------------------- | --------------- | ------------------------------------- | ------------------------------------------------ |
| Expiracion de reservas       | 15 minutos      | `ProviderBookingExpiration`           | Vence solicitudes sin confirmar despues de 24 h. |
| Recordatorio de reserva      | Diario 08:00 CR | `ProviderBookingReminder`             | Notifica reservas confirmadas dentro de 24 h.    |
| Expiracion de verificacion   | Diario          | `ProviderVerificationExpiration`      | Marca evidencia vencida.                         |
| Recordatorio de revalidacion | Diario 09:00 CR | `ProviderVerificationRenewalReminder` | Notifica vencimientos dentro de 30 dias.         |

## Operacion administrativa

- Revisar evidencia privada desde Admin > Verificacion proveedores.
- Aprobar requiere fecha de vencimiento; rechazar requiere motivo.
- Suspender un proveedor requiere motivo; reactivar elimina el motivo.
- La evidencia se descarga solo mediante endpoints autenticados y queda auditada.
- No compartir URLs de Blob, identificadores de clientes ni notas de mascotas.

## Incidentes de reserva

1. Confirmar el estado y el historial de auditoria de la reserva.
2. Para conflictos de horario, verificar reglas semanales y cierres excepcionales.
3. No modificar la base de datos manualmente: usar transiciones de API.
4. Si una reserva queda en `Requested`, el job la vence a las 24 h.
5. Para suspender un proveedor durante una investigacion, usar la consola Admin y registrar el motivo.

## Alertas recomendadas

- Errores de `ProviderBookingExpirationHostedService` o
  `ProviderVerificationExpirationHostedService`.
- Fallos de carga/descarga del contenedor privado `provider-verification`.
- Tasa elevada de conflictos de capacidad al crear o reprogramar reservas.
- Verificaciones pendientes por mas de 48 h.
- Proveedores suspendidos y reservas futuras asociadas.

## Validacion automatizada

- Unitarias del modulo:
  `dotnet test backend/tests/PawTrack.UnitTests/PawTrack.UnitTests.csproj --filter FullyQualifiedName~ServiceProviders`.
- Integracion HTTP in-process:
  `dotnet test backend/tests/PawTrack.IntegrationTests/PawTrack.IntegrationTests.csproj --filter FullyQualifiedName~ServiceProviders.ServiceProviderEndpointsTests`.
- Frontend:
  `cd frontend; npx tsc -b --pretty false`.
- Concurrencia real SQL y Playwright requieren SQL Server, Azurite y el backend
  ejecutandose. Iniciar los servicios definidos en `docker-compose.yml`, aplicar
  migraciones y sembrar cuentas E2E antes de ejecutar `npm run test:e2e`.

## Limites conocidos

- No hay pagos, reembolsos ni disputas implementados.
- No hay integración externa de agenda.
- Las decisiones de comisiones, deposito SINPE y politicas de cancelacion deben
  aprobarse antes de implementar cobros.
