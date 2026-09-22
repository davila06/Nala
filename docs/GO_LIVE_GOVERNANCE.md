# Gobierno de salida a produccion

Este gate impide despliegues de produccion hasta que las aprobaciones externas
esten respaldadas por evidencia revisable. Una capacidad tecnica no sustituye
un tramite, contrato o criterio legal.

## Variables protegidas del environment `production`

| Variable                         | Aprobador             | Evidencia minima                   |
| -------------------------------- | --------------------- | ---------------------------------- |
| `PRODHAB_REGISTRATION_CONFIRMED` | Legal/Privacy         | Numero o constancia de registro    |
| `AZURE_DPA_CONFIRMED`            | Legal/Privacy         | Referencia del DPA vigente         |
| `B2B_CONTRACTS_APPROVED`         | Legal/Commercial      | Version contractual aprobada       |
| `SLA_APPROVED`                   | Operations/Commercial | Version y fecha del SLA            |
| `PRICING_APPROVED`               | Finance/Product       | Catalogo, moneda, IVA y vigencia   |
| `LEGAL_APPROVAL_REFERENCE`       | Release manager       | URL o identificador del expediente |

Las cinco confirmaciones deben contener exactamente `true`. La referencia no
debe contener documentos confidenciales ni secretos; debe apuntar al sistema
corporativo donde reside la evidencia.

## Proveedores externos

Antes de un release que dependa de Azure, WhatsApp, GPS, pagos o correo, el
release manager debe ejecutar el gate de
[EXTERNAL_PROVIDER_VALIDATION.md](EXTERNAL_PROVIDER_VALIDATION.md). La evidencia
local es redactada, no contiene secretos y vence a los 90 días. Una referencia
de Key Vault o un adaptador de código no sustituyen contrato, cuenta aprobada y
prueba controlada.

## Control de cambios

- Las variables viven en el GitHub Environment protegido `production`.
- Legal, Privacy, Finance y Operations deben aprobar los cambios de su dominio.
- El release manager registra la referencia de evidencia antes del despliegue.
- Revocar una aprobación bloquea automáticamente los siguientes despliegues.
- Los claims `production-ready`, `SENASA`, recuperación y certificación requieren
  revisión legal separada antes de publicarse.

El control automatizado se ejecuta mediante
`scripts/Test-GoLiveGovernance.ps1`. Este documento no afirma que los trámites
estén completados; registra cómo se demuestra y exige su cumplimiento.
