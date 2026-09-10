# Consolidacion Documental

**Estado:** activo  
**Corte:** 2026-09-09

Este documento define que archivo manda cuando existen documentos con el mismo
alcance. Los archivos historicos se conservan, pero no deben recibir nuevas
secciones de producto sin actualizar primero la fuente canonica.

## Fuentes canonicas

| Familia             | Fuente canonica                                                              | Historicos o soporte                                                                      |
| ------------------- | ---------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Producto/estado     | `STATUS.md`                                                                  | `NALA.md`, `CCNAla.md`                                                                    |
| Features/tier       | `FEATURES.md` + `PRICING_AND_PLANS.md`                                       | `planes.md`, `precios.md`, `pricing.md`, `featuresB2B.md`                                 |
| Deploy              | `RUNBOOK_DEPLOYMENT.md`                                                      | `GUIA_DEPLOY_PASO_A_PASO.md`, `operacional.md`, `pasos-para-ir-live.md`, `DEPLOY_INFO.md` |
| Seguridad API       | `API_AUTHORIZATION_MATRIX.md` + `API_REFERENCE.md`                           | notas de auditoria antiguas                                                               |
| Collares            | `COLLAR_CURRENT_STATE.md`                                                    | `collarFinal.md`, `COLLAR_IMPLEMENTATION_PLAN.md`, `jimiiot.md`                           |
| Adopciones          | `ADOPTIONS_CURRENT_STATE.md`                                                 | `adopciones.md`, `MANUAL_ALIADOS_REFUGIOS.md`                                             |
| Salud/compliance    | `MATRIZ_RETENCION_DATOS.md` + `CUMPLIMIENTO_PROTECCION_DATOS.md`             | `expediente.md`, `senasa.md`                                                              |
| NALA/reportes       | `NALA_REPORTING_GUIDE.md`                                                    | `MANUAL_NALA.md`, `CATALOGO_METRICAS_NALA.md`, `MANUAL_REPORTES_INSTITUCIONALES.md`       |
| Operacion           | `RUNBOOK_OPERACIONES.md`                                                     | `operacional.md`, `B2B_E2E_RUNBOOK.md`                                                    |
| Seguridad operativa | `RUNBOOK_SEGURIDAD_INCIDENTES.md` + `RUNBOOK_MFA_Y_ACCESOS_PRIVILEGIADOS.md` | instrucciones dispersas de deploy                                                         |
| Continuidad         | `RUNBOOK_BACKUPS_RECUPERACION.md`                                            | datos beta y notas de infraestructura                                                     |
| Pagos               | `RUNBOOK_PAGOS_SINPE.md` + `PRICING_AND_PLANS.md`                            | propuestas en `pricing.md` y `ganancia.md`                                                |
| Integraciones       | `GUIA_INTEGRACIONES_WEBHOOKS.md`                                             | `API_CLINIC_PARTNER_v1.md`, `jimiiot.md`                                                  |
| QA                  | `GUIA_QA_E2E.md`                                                             | `pruebas.md`, `B2B_E2E_RUNBOOK.md`                                                        |
| UX                  | `GUIA_ACCESIBILIDAD_Y_UX.md`                                                 | criterios aislados en issues                                                              |
| Bienestar           | `RUNBOOK_MODERACION_Y_BIENESTAR.md`                                          | `MANUAL_ALIADOS_REFUGIOS.md`, `MANUAL_MUNICIPAL_REPORTES.md`                              |
| Datos               | `DICCIONARIO_DATOS.md` + `MATRIZ_RETENCION_DATOS.md`                         | modelo EF y auditorias                                                                    |
| Manuales            | `Manuales/` por rol                                                          | manuales de dominio anteriores                                                            |

## Regla de actualización

1. Actualizar la fuente canonica.
2. Actualizar el manual o runbook afectado.
3. Marcar el documento historico si ya no debe usarse.
4. Verificar enlaces y claims contra codigo/pruebas.
5. Registrar el cambio en `CHANGELOG.md` cuando afecte una capacidad visible.

No duplicar precios, estados, permisos ni URLs de API en documentos secundarios.
