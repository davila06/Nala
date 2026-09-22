# Validación de Proveedores Externos

## Regla de go-live

WhatsApp, GPS, pagos, Azure y correo no se consideran operativos en producción solo porque el código tenga adaptadores o referencias a Key Vault. Cada proveedor requiere cuenta o contrato vigente, credenciales almacenadas en Key Vault, una prueba controlada y evidencia redactada.

No registrar secretos, tokens, números de cuenta, URLs firmadas ni datos personales en este documento, tickets o repositorio.

## Gate ejecutable

1. Copiar `scripts/external-provider-evidence.example.json` a `scripts/external-provider-evidence.local.json`.
2. Mantener el archivo local fuera de control de versiones.
3. Registrar solo referencias internas redactadas de contrato/aprobación y prueba.
4. Ejecutar:

```powershell
pwsh -NoProfile -File scripts/Test-ExternalProviderReadiness.ps1 -Environment Production
```

El gate exige evidencia para `Azure`, `WhatsApp`, `GPS`, `Payments` y `Email`; vence a los 90 días salvo que se use una excepción explícita.

## Evidencia mínima por proveedor

| Proveedor | Validación técnica                                                              | Evidencia contractual/operativa                                            |
| --------- | ------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| Azure     | Key Vault, identidad administrada, health checks, alertas y rollback en staging | Suscripción, resource group, owner operativo y presupuesto aprobados       |
| WhatsApp  | Webhook firmado, mensaje de prueba y manejo de errores de Meta                  | Cuenta Business, número aprobado y política de plantillas                  |
| GPS       | Ingesta o polling contra dispositivo sandbox, timestamp y precisión verificadas | Contrato/OEM, límites API, soporte y política de datos                     |
| Payments  | Cobro/refund sandbox y conciliación sin datos de tarjeta en PawTrack            | Gateway/SINPE aprobado, términos financieros y responsable de conciliación |
| Email     | Entrega a buzón de prueba, SPF/DKIM/DMARC y rebotes                             | Dominio remitente aprobado y límites de SendGrid                           |

## Estado actual

Las configuraciones productivas usan referencias de Key Vault para Azure, correo y canales de broadcast. El código contiene adaptadores para WhatsApp, TrackSolid GPS y CyberSource. La evidencia real debe completarla un operador autorizado con acceso a las cuentas y contratos; no se puede sustituir con datos de desarrollo.
