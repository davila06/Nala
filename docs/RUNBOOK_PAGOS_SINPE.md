# Runbook de Pagos SINPE y Activaciones Manuales

**Estado:** activo con alcance limitado  
**Audiencia:** Admin y soporte comercial  
**Corte:** 2026-09-09

## Alcance actual

PawTrack soporta reportes y activaciones manuales de suscripciones mediante
referencia SINPE. No debe describirse como procesador de pagos automatico.
Tiendas y proveedores no tienen checkout o liquidacion automatica aprobada.

## Flujo B2C/B2B habilitado

1. El cliente selecciona un plan y obtiene una referencia.
2. Realiza la transferencia fuera de PawTrack.
3. Reporta el pago y conserva comprobante.
4. Admin verifica monto, referencia, tier, titular y periodo.
5. Admin activa la suscripcion desde el panel.
6. Se confirma `Status`, `ExpiresAt` y que el feature gate responda correctamente.

## Controles

- No activar por captura sin referencia verificable.
- No reutilizar `PaymentReference`.
- Confirmar que la suscripcion no este vencida.
- Registrar actor, fecha, referencia y resultado.
- Ante duplicado, reembolso o monto incorrecto, dejar pendiente y escalar.
- No guardar comprobantes con PII en ubicaciones no autorizadas.

## Alcance por segmento

| Segmento           | Estado                                              |
| ------------------ | --------------------------------------------------- |
| Dueños             | activacion manual de `UserPlus`/`UserFamilia`       |
| Clinicas           | activacion manual de `ClinicPlus`/`ClinicPartner`   |
| Tiendas            | el cliente coordina pago directamente con la tienda |
| Proveedores        | sin precio recurrente aprobado                      |
| Municipalidades    | tiers anuales modelados; contratacion pendiente     |
| Recompensas/escrow | propuesta, no flujo activo                          |

La fuente de tiers es [PRICING_AND_PLANS.md](PRICING_AND_PLANS.md). Nunca usar
`planes.md`, `precios.md` o `pricing.md` para activar un plan.
