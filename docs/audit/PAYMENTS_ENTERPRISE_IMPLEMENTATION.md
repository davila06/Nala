# Implementación enterprise+ de pagos

**Actualización:** 2026-09-29. Este documento describe el estado del worktree después de la primera vertical de pagos. No acredita cuenta CyberSource/BAC, aprobación comercial, sandbox operativo, 3-D Secure ni producción.

## Implementado

- `PaymentIntent` con estados `Created`, `PendingCustomerAction`, `Authorized`, `Captured`, `Settled`, `Failed`, `Declined`, `Cancelled`, `Refunded`, `PartiallyRefunded`, `Disputed` y `Unknown`.
- Transiciones protegidas por invariantes; reembolso parcial/total sin sobre-reembolso.
- Índices únicos EF para `(UserId, IdempotencyKey)` y `MerchantReference` en migración `AddPaymentIntents`.
- `ChargeCard` exige `Idempotency-Key`, reutiliza el mismo intent en replay y no activa beneficios durante la respuesta síncrona.
- CyberSource fail-closed: sin credenciales no simula contexto, tokenización, autorización, captura, void ni refund.
- Frontend usa hosted fields y `createToken` de CyberSource Flex; no fabrica tokens ni mantiene PAN/CVV en estado React.
- Webhook CyberSource exige `X-CyberSource-Signature`, deduplica por `MerchantReference` y transiciona `Authorized -> Captured -> Settled`.
- `Settled` se publica mediante el outbox existente; un handler idempotente activa suscripción, bundle o bounty después del commit.
- Endpoints autenticados para `capture`, `void` y `refund`.

## Pruebas ejecutadas

- Unit tests backend completos: `1.659` correctos.
- Tests de dominio `PaymentIntent`, repositorio, handler idempotente y fulfillment post-settlement.
- Tests CyberSource fail-closed y operaciones financieras no configuradas.
- Integración enfocada de suscripción correcta.
- Frontend `typecheck` correcto y suite focalizada de `SecureCardPaymentForm`: `3` correctos.

## Pendiente antes de beta

1. Contrato CyberSource/BAC confirmado: merchant, moneda, liquidación, contracargos, credenciales sandbox y 3-D Secure.
2. Contract tests contra sandbox y validación de la firma real de webhooks CyberSource; el HMAC configurado en esta vertical debe alinearse con el mecanismo oficial del proveedor antes de producción.
3. Idempotencia persistida por operación de capture/void/refund, además de la idempotencia del payment intent.
4. Reconciliación diaria, settlement externo, refunds parciales reales, chargebacks, ledger financiero y corrección manual auditada.
5. Rotación Key Vault, scopes mínimos, alertas de fraude, límites por usuario/tenant, correlation IDs y revisión PCI/SAQ/ROC.
6. E2E con tarjetas sandbox, pruebas de timeout/retry, eventos duplicados/fuera de orden, webhook inválido y doble cobro.

Hasta completar estos puntos, el estado global es `COMPLETE_BUT_UNVERIFIED`, no `PRODUCTION_READY`.
