# Matriz de Retencion de Datos

**Estado:** activo  
**Audiencia:** producto, soporte, legal y plataforma  
**Corte:** 2026-09-09

> Los periodos son configuracion operativa y deben revisarse con legal. Esta
> matriz no autoriza conservar datos mas tiempo del necesario.

| Datos                    | Retencion tecnica actual                  | Responsable                         | Regla                                 |
| ------------------------ | ----------------------------------------- | ----------------------------------- | ------------------------------------- |
| QR scans                 | job de retencion configurado              | Infrastructure                      | conservar segun tier y ventana activa |
| Avistamientos            | `SightingRetentionDays` (730 por defecto) | PersonalDataRetentionJob            | purgar registros antiguos             |
| Chats cerrados           | 730 dias por defecto                      | PersonalDataRetentionJob            | solo threads `Closed`                 |
| Notificaciones leidas    | 365 dias por defecto                      | PersonalDataRetentionJob            | no purgar no leidas                   |
| Ubicaciones de collar    | 30 dias                                   | CollarLocationPurgeJob              | historial crudo; export bajo acceso   |
| Vistas de perfil clinico | 90 dias                                   | ClinicProfileViewPurgeHostedService | agregacion sin PII innecesaria        |
| Grants y logs medicos    | segun expiracion y auditoria              | Medical jobs                        | mantener trazabilidad y revocacion    |
| Exports medicos          | metadata expirada se purga                | Medical retention                   | no conservar archivos innecesarios    |
| Auditoria administrativa | politica legal/operativa                  | Audit repositories                  | integridad y acceso restringido       |
| Tokens revocados         | limpieza programada                       | RevokedTokenCleanupJob              | impedir replay durante vigencia       |

## Derechos de la persona

- Exportacion: `GET /api/auth/me/export`.
- Eliminacion: `DELETE /api/auth/me` con confirmacion de contrasena.
- Consentimiento sanitario: `POST /api/auth/me/health-data-consent`.
- Revocacion de grants medicos desde el panel del propietario.

## Reglas

- Aplicar filtro antes de enmascarar datos en la respuesta.
- No usar datos retenidos para nuevos fines de producto sin base legal.
- Los backups pueden tener una ventana distinta y deben quedar documentados.
- Toda excepcion legal debe tener responsable, motivo y fecha de expiracion.

Ver [CUMPLIMIENTO_PROTECCION_DATOS.md](CUMPLIMIENTO_PROTECCION_DATOS.md) y
[MANUAL_USUARIO.md](Manuales/MANUAL_USUARIO.md).
