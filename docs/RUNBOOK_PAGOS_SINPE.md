# Runbook de Pagos SINPE y Activaciones Manuales

**Estado:** activo con alcance limitado  
**Audiencia:** Admin y soporte comercial  
**Corte:** 2026-10-02

## Alcance actual

PawTrack soporta reportes y activaciones manuales de suscripciones mediante
referencia SINPE. No debe describirse como procesador de pagos automatico.
Para tiendas, el codigo actual tiene un flujo separado y manual: el cliente
puede reportar un pago externo; la tienda registra que lo verifico por su
propio canal y puede guardar la referencia de una devolucion externa. No hay
conector bancario, captura, webhook, conciliacion ni liquidacion para pedidos de
tienda. Su migracion y despliegue `NO_VERIFICADO`; activar solo dentro del piloto
hibrido aprobado y tras el gate legal/comercial.

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

## Tiendas: registro manual externo (no es procesamiento)

1. Cliente y tienda acuerdan el metodo; el cliente transfiere por fuera de NALA.
2. El cliente puede registrar que reporto el pago en PawTrack.
3. La tienda revisa la cuenta bancaria por su propio canal y, si corresponde,
   registra manualmente su verificacion y referencia.
4. Para devolver fondos, la tienda ejecuta la devolucion fuera de PawTrack y
   despues puede registrar la referencia externa.

La accion de la tienda es una atestacion del actor; no existe confirmacion de
banco desde la aplicacion. No almacenar credenciales, comprobantes con PII ni
datos bancarios fuera de los sistemas autorizados. Este flujo no prueba pago
liquidado ni habilita claims de SINPE integrado.
