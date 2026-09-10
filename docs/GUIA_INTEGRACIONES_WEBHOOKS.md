# Guia de Integraciones y Webhooks

**Estado:** activo  
**Audiencia:** integradores de clinicas, collares y partners  
**Corte:** 2026-09-09

## Principios

Toda integracion debe tener identidad, scope, rotacion, idempotencia, timeout,
reintentos y auditoria. Un header libre no sustituye autenticacion.

## API Partner de clinicas

`ClinicPartner` puede usar API keys con scopes separados: `scan`,
`medical:read`, `medical:write`, `certificates`, `analytics` y los scopes de
exportacion definidos por el backend. Las keys se muestran una vez, tienen
vencimiento y deben revocarse ante sospecha.

## Webhooks salientes

Las suscripciones de webhook se encolan y entregan mediante outbox/fanout. El
consumidor debe:

1. validar firma y timestamp;
2. guardar el identificador de evento;
3. responder rapidamente con `2xx`;
4. procesar de forma idempotente;
5. tolerar reintentos y orden no garantizado;
6. registrar fallos sin incluir secretos.

Los estados de entrega incluyen `Pending`, `Delivered`, `Failed` y `Disabled`.

## WhatsApp

Los webhooks entrantes requieren verificacion de Meta y firma HMAC. El sistema
usa idempotencia por `wamid`; Meta puede redeliver el mismo evento. No guardar
el numero telefonico en claro: el dominio usa hash para correlacion operativa.

## GPS

Tractive usa OAuth2 con tokens protegidos. Los dispositivos genericos usan
credenciales de dispositivo y validacion de serial. La ingesta valida rango,
propietario/dispositivo y rate limit. Un proveedor OEM debe documentar modelo,
protocolo, push/polling, precision y borrado de ubicaciones.

## Checklist de integrador

- scope minimo definido;
- URL HTTPS y dominio controlado;
- secreto en Key Vault o gestor aprobado;
- idempotencia y reintentos probados;
- rate limits conocidos;
- prueba BOLA/tenant;
- plan de revocacion y contacto operativo.

Ver [API_CLINIC_PARTNER_v1.md](API_CLINIC_PARTNER_v1.md),
[collarFinal.md](collarFinal.md) y [API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md).
