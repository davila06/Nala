# Auditoria documental de PawTrack CR

**Fecha:** 2026-09-09  
**Alcance:** todos los `.md` bajo `docs/` y `docs/Manuales/`  
**Fuentes de contraste:** `backend/src`, `frontend/src`, `UserRole`, router y controllers

## Resultado

La documentacion fue clasificada por responsabilidad y contrastada con la
implementacion. La fuente vigente es [README.md](README.md), junto con
[STATUS.md](STATUS.md), [FEATURES.md](FEATURES.md),
[PRICING_AND_PLANS.md](PRICING_AND_PLANS.md), la matriz de autorizacion y los
manuales por rol.

No se reescribieron documentos historicos que describen decisiones o auditorias
pasadas. Se marcaron en el indice para evitar que se usen como contrato actual.

## Roles confirmados

El codigo define: `Owner`, `Ally`, `Admin`, `Clinic`, `Municipality`, `Store`,
`ServiceProvider` y `Support`. La cobertura documental final es:

| Rol             | Documento                                                                |
| --------------- | ------------------------------------------------------------------------ |
| Owner           | [Manuales/MANUAL_USUARIO.md](Manuales/MANUAL_USUARIO.md)                 |
| Ally            | [Manuales/MANUAL_ALIADOS.md](Manuales/MANUAL_ALIADOS.md)                 |
| Admin           | [Manuales/MANUAL_ADMINISTRADOR.md](Manuales/MANUAL_ADMINISTRADOR.md)     |
| Clinic          | [Manuales/MANUAL_CLINICAS.md](Manuales/MANUAL_CLINICAS.md)               |
| Municipality    | [Manuales/MANUAL_MUNICIPALIDADES.md](Manuales/MANUAL_MUNICIPALIDADES.md) |
| Store           | [Manuales/MANUAL_TIENDAS.md](Manuales/MANUAL_TIENDAS.md)                 |
| ServiceProvider | [Manuales/MANUAL_PROVEEDORES.md](Manuales/MANUAL_PROVEEDORES.md)         |
| Support         | [Manuales/MANUAL_SOPORTE.md](Manuales/MANUAL_SOPORTE.md)                 |

## Correcciones aplicadas

- Se documentaron las pestañas actuales de Admin: adopciones, bienestar,
  proveedores, incidentes y funnel, ademas de vallas y CollarTags.
- Se separo el acceso gratuito de directorio de los tiers comerciales de
  clinicas y tiendas.
- Se documentaron los gates reales de municipalidades, proveedores y tiendas.
- Se incorporaron exportacion de datos, consentimiento de salud y eliminacion
  de cuenta al manual Owner.
- Se aclaró que `Support` no tiene portal propio y que su acceso es limitado a
  bienestar e incidentes de proveedores.
- Se conservaron los claims de SENASA como `SENASA-ready`, no como aprobacion o
  integracion oficial.
- Se crearon fuentes operativas para API, deployment, seguridad, backups,
  retencion, MFA, pagos, integraciones, jobs, QA, accesibilidad, bienestar y
  diccionario de datos.
- Se consolidaron familias duplicadas de deployment, precios, collares,
  adopciones y NALA mediante documentos canonicos.
- Se retiro una credencial SQL literal de la documentacion beta.

## Documentos fuente activa revisados

- `STATUS.md`, `FEATURES.md`, `PRICING_AND_PLANS.md`
- `PawTrack_Documento_Maestro_v3.1.md`
- `API_AUTHORIZATION_MATRIX.md`
- `RUNBOOK_OPERACIONES.md`, `RUNBOOK_REPORTES_INSTITUCIONALES.md`
- `GUIA_ONBOARDING_DEV.md`, `GUIA_DEPLOY_PASO_A_PASO.md`, `pruebas.md`
- `POLITICA_DE_PRIVACIDAD.md`, `TERMINOS_DE_USO.md`,
  `CUMPLIMIENTO_PROTECCION_DATOS.md`
- `NALA.md`, `QR.md`, `expediente.md`, `senasa.md`, `collarFinal.md`,
  `jimiiot.md`
- `B2B_ESTADO_ACTUAL.md`, `featuresB2B.md`, `SERVICE_PROVIDERS_OPERABILITY.md`,
  `SERVICE_PROVIDERS_THREAT_MODEL.md`, `SERVICE_PROVIDERS_ENTERPRISE_TODOLIST.md`
- `API_CLINIC_PARTNER_v1.md`, `CATALOGO_METRICAS_NALA.md`,
  `OBSERVABILITY_SLO_RUNBOOK.md`, `DEPLOY_INFO.md`, `ERRORES_PENDIENTES.md`
- `B2B_E2E_RUNBOOK.md`, `CHANGELOG.md`, `checklist-lanzamiento.md`,
  `GUIA_SCOPES_REPORTES.md`, `sinpe.md`, `usuarios-prueba.md`,
  `MANUAL_ALIADOS_REFUGIOS.md`, `MANUAL_MUNICIPAL_REPORTES.md`,
  `MANUAL_REPORTES_INSTITUCIONALES.md`
- Los cinco manuales existentes y los cuatro manuales nuevos bajo `Manuales/`

## Documentos conservados como historicos o de trabajo

`TODOs.md`, `pendientesTotales.md`, `sprint-plan-enterprise.md`,
`ROLLOUT_SPRINT5.md`, `benchmark.md`, `sponsor.md`, `influencer.md`,
`adopciones.md`, `adopciones-todolist.md`, `ganancia.md`, `planes.md`,
`precios.md`, `pricing.md`, `publicidad.md`, `vallasPublicitarias.md`,
`pendientesTiendas.md`, `legal.md`, `operacional.md`, `pre.md`, `STATUS.md`
(no: STATUS permanece activo), `CCNAla.md`, `COLLAR_IMPLEMENTATION_PLAN.md`,
`pasos-para-ir-live.md`, `senasa-sprint1-todolist.md`,
`senasa-sprint2-todolist.md`, `senasa-sprint3-todolist.md`,
`senasa-sprint4-todolist.md`, `senasa-sprint5-todolist.md`, `todolist-b2b-enterprise.md`,
`adopciones.md` y los documentos de propuesta/comerciales no enlazados como
fuentes activas.

`PRODUCT_P0_ENTERPRISE_TODOLIST.md` y `PRODUCT_STRATEGY_TOP1.md` siguen siendo
fuentes activas de backlog/estrategia y no deben tratarse como manuales de uso.
La etiqueta historica aplica a los documentos de trabajo fechados, incluso
cuando una tarea concreta siga siendo valida.

## Regla de mantenimiento

Cada cambio de producto que afecte a una persona usuaria debe actualizar el
manual del rol correspondiente y `STATUS.md`. Cada cambio de autorizacion debe
actualizar [API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md). Cada
cambio de plan o gate debe actualizar `FEATURES.md` y
`PRICING_AND_PLANS.md` sin duplicar precios en otros documentos.
