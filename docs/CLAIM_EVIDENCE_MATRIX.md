# Matriz de Claims y Evidencia

> Corte: 2026-09-23  
> Estado: control de publicación pendiente de aprobación Legal/Comercial

Un claim solo puede publicarse cuando tiene evidencia técnica, evidencia
operativa y aprobación del responsable. `SENASA-ready` no significa aprobación,
certificación ni integración oficial.

| Claim                                  | Superficie                        | Evidencia técnica                                | Evidencia operativa                                    | Aprobación requerida   | Responsable         | Vence evidencia          | Referencia aprobación | Estado    |
| -------------------------------------- | --------------------------------- | ------------------------------------------------ | ------------------------------------------------------ | ---------------------- | ------------------- | ------------------------ | --------------------- | --------- |
| Recuperación de mascotas               | Marketing, login, investor docs   | Product Events correlacionados por `lostEventId` | Ventana real con métricas p50/p90 y muestra suficiente | Legal + Comercial      | Product + Legal     | No vigente hasta aprobar | Pendiente             | Pendiente |
| Producción enterprise                  | Marketing, sponsor, investor docs | Build, tests, seguridad, IaC                     | CI protegida, alertas, rollback y on-call              | Release + Operations   | Release Manager     | No vigente hasta aprobar | Pendiente             | Bloqueado |
| `SENASA-ready`                         | Clínicas, reportes, certificados  | PDF verificable, auditoría y trazabilidad        | Revisión documental vigente                            | Legal                  | Legal + Clinic      | No vigente hasta aprobar | Pendiente             | Pendiente |
| Certificado veterinario verificable    | Clínicas                          | Firma/QR/verificación pública                    | Clínica y veterinario autorizados                      | Legal + Clínica        | Clinic Operations   | 2026-12-22               | Pendiente             | Parcial   |
| Proveedor verificado                   | Directorio y marketplace          | Gates de verificación y scopes                   | Contrato, documentos y expiración vigentes             | Operations             | Provider Operations | 2026-12-22               | Pendiente             | Parcial   |
| Seguridad de ubicación                 | GPS, SignalR, marketing           | Consentimiento, aproximación, TTL, auditoría     | E2E multiusuario y revisión de logs                    | Privacy + Security     | Security + Privacy  | 2026-12-22               | Pendiente             | Parcial   |
| Integraciones WhatsApp/GPS/pagos/email | Marketing y ventas                | Adaptador, timeout, retry, idempotencia          | Sandbox/prod smoke test y contrato vigente             | Operations + Comercial | Platform Operations | No vigente hasta aprobar | Pendiente             | Pendiente |
| Métricas de recuperación               | Admin, institucional              | SQL por incidente y medianas                     | Azure Monitor y ventana de observación                 | Product + Legal        | Product Analytics   | 2026-12-22               | Pendiente             | Parcial   |

## Regla de publicación

- `Implementado`: código y pruebas locales.
- `Verificado por PawTrack`: evidencia técnica reproducible.
- `Operativo`: evidencia de staging/producción con fecha.
- `Aprobado`: referencia de Legal/Comercial/Operations.
- `Avalado por autoridad externa`: solo con documento verificable de la autoridad.

El gate técnico asociado a proveedores es
`scripts/Test-ExternalProviderReadiness.ps1`. El gate de lanzamiento es
`scripts/Test-GoLiveGovernance.ps1`.
