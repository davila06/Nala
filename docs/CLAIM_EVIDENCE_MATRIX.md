# Matriz de Claims y Evidencia

> Corte: 2026-09-23  
> Estado: control de publicación pendiente de aprobación Legal/Comercial

Un claim solo puede publicarse cuando tiene evidencia técnica, evidencia
operativa y aprobación del responsable. `SENASA-ready` no significa aprobación,
certificación ni integración oficial.

| Claim                                  | Superficie                        | Evidencia técnica                                | Evidencia operativa                                    | Aprobación requerida   | Estado    |
| -------------------------------------- | --------------------------------- | ------------------------------------------------ | ------------------------------------------------------ | ---------------------- | --------- |
| Recuperación de mascotas               | Marketing, login, investor docs   | Product Events correlacionados por `lostEventId` | Ventana real con métricas p50/p90 y muestra suficiente | Legal + Comercial      | Pendiente |
| Producción enterprise                  | Marketing, sponsor, investor docs | Build, tests, seguridad, IaC                     | CI protegida, alertas, rollback y on-call              | Release + Operations   | Bloqueado |
| `SENASA-ready`                         | Clínicas, reportes, certificados  | PDF verificable, auditoría y trazabilidad        | Revisión documental vigente                            | Legal                  | Pendiente |
| Certificado veterinario verificable    | Clínicas                          | Firma/QR/verificación pública                    | Clínica y veterinario autorizados                      | Legal + Clínica        | Parcial   |
| Proveedor verificado                   | Directorio y marketplace          | Gates de verificación y scopes                   | Contrato, documentos y expiración vigentes             | Operations             | Parcial   |
| Seguridad de ubicación                 | GPS, SignalR, marketing           | Consentimiento, aproximación, TTL, auditoría     | E2E multiusuario y revisión de logs                    | Privacy + Security     | Parcial   |
| Integraciones WhatsApp/GPS/pagos/email | Marketing y ventas                | Adaptador, timeout, retry, idempotencia          | Sandbox/prod smoke test y contrato vigente             | Operations + Comercial | Pendiente |
| Métricas de recuperación               | Admin, institucional              | SQL por incidente y medianas                     | Azure Monitor y ventana de observación                 | Product + Legal        | Parcial   |

## Regla de publicación

- `Implementado`: código y pruebas locales.
- `Verificado por PawTrack`: evidencia técnica reproducible.
- `Operativo`: evidencia de staging/producción con fecha.
- `Aprobado`: referencia de Legal/Comercial/Operations.
- `Avalado por autoridad externa`: solo con documento verificable de la autoridad.

El gate técnico asociado a proveedores es
`scripts/Test-ExternalProviderReadiness.ps1`. El gate de lanzamiento es
`scripts/Test-GoLiveGovernance.ps1`.
