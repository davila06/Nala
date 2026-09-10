# PawTrack CR - Mapa oficial de documentacion

> Fuente de navegacion oficial. Actualizado: 2026-09-09.

Esta carpeta fue auditada contra `backend/src` y `frontend/src` el
2026-09-09. Los manuales operativos por rol viven en
[Manuales/](Manuales/); los documentos historicos se conservan para
trazabilidad, pero no son contratos vigentes.

## Como leer la documentacion

Cada documento tiene una responsabilidad. Si dos documentos contradicen el
codigo, el codigo y `docs/STATUS.md` prevalecen hasta que se corrija la
contradiccion.

Estados documentales:

- `active`: fuente vigente y revisada.
- `draft`: propuesta que requiere aprobacion.
- `historical`: contexto historico; no usar para decisiones actuales.
- `archived`: conservado por trazabilidad; no representa el producto vigente.

## Fuentes activas

| Documento                                                                        | Proposito                                    |
| -------------------------------------------------------------------------------- | -------------------------------------------- |
| [STATUS.md](STATUS.md)                                                           | Estado tecnico y operativo verificado        |
| [PRODUCT_STRATEGY_TOP1.md](PRODUCT_STRATEGY_TOP1.md)                             | Estrategia, north star, moat y roadmap       |
| [PRODUCT_P0_ENTERPRISE_TODOLIST.md](PRODUCT_P0_ENTERPRISE_TODOLIST.md)           | Backlog y gates P0 enterprise                |
| [FEATURES.md](FEATURES.md)                                                       | Matriz de capacidades por plan               |
| [PRICING_AND_PLANS.md](PRICING_AND_PLANS.md)                                     | Fuente comercial consolidada                 |
| [PawTrack_Documento_Maestro_v3.1.md](PawTrack_Documento_Maestro_v3.1.md)         | Arquitectura y especificacion consolidada    |
| [pruebas.md](pruebas.md)                                                         | Usuarios, runtime local y validacion manual  |
| [RUNBOOK_OPERACIONES.md](RUNBOOK_OPERACIONES.md)                                 | Operacion, incidentes y continuidad          |
| [POLITICA_DE_PRIVACIDAD.md](POLITICA_DE_PRIVACIDAD.md)                           | Politica de privacidad para revision legal   |
| [TERMINOS_DE_USO.md](TERMINOS_DE_USO.md)                                         | Terminos de uso para revision legal          |
| [ERRORES_PENDIENTES.md](ERRORES_PENDIENTES.md)                                   | Deuda tecnica y gates de calidad             |
| [API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md)                       | Ownership, BOLA/IDOR y versionado API        |
| [AUDITORIA_DOCUMENTAL_2026-09-09.md](AUDITORIA_DOCUMENTAL_2026-09-09.md)         | Resultado de la auditoria documental         |
| [CONSOLIDACION_DOCUMENTAL.md](CONSOLIDACION_DOCUMENTAL.md)                       | Jerarquia de fuentes y documentos historicos |
| [API_REFERENCE.md](API_REFERENCE.md)                                             | Referencia organizada de la API              |
| [RUNBOOK_DEPLOYMENT.md](RUNBOOK_DEPLOYMENT.md)                                   | Deployment canonico                          |
| [RUNBOOK_SEGURIDAD_INCIDENTES.md](RUNBOOK_SEGURIDAD_INCIDENTES.md)               | Respuesta a incidentes de seguridad          |
| [RUNBOOK_BACKUPS_RECUPERACION.md](RUNBOOK_BACKUPS_RECUPERACION.md)               | Backups y continuidad                        |
| [MATRIZ_RETENCION_DATOS.md](MATRIZ_RETENCION_DATOS.md)                           | Retencion y derechos de datos                |
| [RUNBOOK_MFA_Y_ACCESOS_PRIVILEGIADOS.md](RUNBOOK_MFA_Y_ACCESOS_PRIVILEGIADOS.md) | MFA y roles privilegiados                    |
| [RUNBOOK_PAGOS_SINPE.md](RUNBOOK_PAGOS_SINPE.md)                                 | Pagos y activaciones manuales                |
| [DICCIONARIO_DATOS.md](DICCIONARIO_DATOS.md)                                     | Entidades y sensibilidad de datos            |
| [GUIA_INTEGRACIONES_WEBHOOKS.md](GUIA_INTEGRACIONES_WEBHOOKS.md)                 | Integraciones y webhooks                     |
| [GUIA_QA_E2E.md](GUIA_QA_E2E.md)                                                 | Estrategia de pruebas                        |
| [GUIA_ACCESIBILIDAD_Y_UX.md](GUIA_ACCESIBILIDAD_Y_UX.md)                         | Criterios frontend y accesibilidad           |
| [RUNBOOK_JOBS_BACKGROUND.md](RUNBOOK_JOBS_BACKGROUND.md)                         | Jobs y tareas programadas                    |
| [RUNBOOK_MODERACION_Y_BIENESTAR.md](RUNBOOK_MODERACION_Y_BIENESTAR.md)           | Bienestar y moderacion                       |
| [COLLAR_CURRENT_STATE.md](COLLAR_CURRENT_STATE.md)                               | Estado actual de collares                    |
| [ADOPTIONS_CURRENT_STATE.md](ADOPTIONS_CURRENT_STATE.md)                         | Estado actual de adopciones                  |
| [NALA_REPORTING_GUIDE.md](NALA_REPORTING_GUIDE.md)                               | NALA y reportes institucionales              |
| [pendientesTiendas.md](pendientesTiendas.md)                                     | Backlog enterprise de tiendas                |
| [legal.md](legal.md)                                                             | Borrador legal y operativo de tiendas        |

## Documentacion por dominio

- Recuperacion y NALA: [NALA_REPORTING_GUIDE.md](NALA_REPORTING_GUIDE.md), [QR.md](QR.md),
  [RUNBOOK_REPORTES_INSTITUCIONALES.md](RUNBOOK_REPORTES_INSTITUCIONALES.md).
- Salud y clinicas: [expediente.md](expediente.md), [CUMPLIMIENTO_PROTECCION_DATOS.md](CUMPLIMIENTO_PROTECCION_DATOS.md), [senasa.md](senasa.md).
- Collares: [COLLAR_CURRENT_STATE.md](COLLAR_CURRENT_STATE.md), [jimiiot.md](jimiiot.md).
- B2B/B2G: [B2B_ESTADO_ACTUAL.md](B2B_ESTADO_ACTUAL.md), [todolist-b2b-enterprise.md](todolist-b2b-enterprise.md).
- Adopciones: [ADOPTIONS_CURRENT_STATE.md](ADOPTIONS_CURRENT_STATE.md), [adopciones.md](adopciones.md).
- NALA y reportes: [NALA_REPORTING_GUIDE.md](NALA_REPORTING_GUIDE.md), [MANUAL_NALA.md](MANUAL_NALA.md), [RUNBOOK_REPORTES_INSTITUCIONALES.md](RUNBOOK_REPORTES_INSTITUCIONALES.md).
- Servicios profesionales: [SERVICE_PROVIDERS_OPERABILITY.md](SERVICE_PROVIDERS_OPERABILITY.md), [SERVICE_PROVIDERS_THREAT_MODEL.md](SERVICE_PROVIDERS_THREAT_MODEL.md), [GUIA_INTEGRACIONES_WEBHOOKS.md](GUIA_INTEGRACIONES_WEBHOOKS.md).
- Pruebas y despliegue: [GUIA_ONBOARDING_DEV.md](GUIA_ONBOARDING_DEV.md), [RUNBOOK_DEPLOYMENT.md](RUNBOOK_DEPLOYMENT.md), [pruebas.md](pruebas.md).

## Operacion transversal

- [RUNBOOK_JOBS_BACKGROUND.md](RUNBOOK_JOBS_BACKGROUND.md)
- [GUIA_QA_E2E.md](GUIA_QA_E2E.md)
- [GUIA_ACCESIBILIDAD_Y_UX.md](GUIA_ACCESIBILIDAD_Y_UX.md)
- [RUNBOOK_MODERACION_Y_BIENESTAR.md](RUNBOOK_MODERACION_Y_BIENESTAR.md)
- [DICCIONARIO_DATOS.md](DICCIONARIO_DATOS.md)
- [GUIA_INTEGRACIONES_WEBHOOKS.md](GUIA_INTEGRACIONES_WEBHOOKS.md)

## Manuales por rol

| Rol del sistema   | Manual operativo                                                |
| ----------------- | --------------------------------------------------------------- |
| `Owner`           | [MANUAL_USUARIO.md](Manuales/MANUAL_USUARIO.md)                 |
| `Ally`            | [MANUAL_ALIADOS.md](Manuales/MANUAL_ALIADOS.md)                 |
| `Admin`           | [MANUAL_ADMINISTRADOR.md](Manuales/MANUAL_ADMINISTRADOR.md)     |
| `Clinic`          | [MANUAL_CLINICAS.md](Manuales/MANUAL_CLINICAS.md)               |
| `Municipality`    | [MANUAL_MUNICIPALIDADES.md](Manuales/MANUAL_MUNICIPALIDADES.md) |
| `Store`           | [MANUAL_TIENDAS.md](Manuales/MANUAL_TIENDAS.md)                 |
| `ServiceProvider` | [MANUAL_PROVEEDORES.md](Manuales/MANUAL_PROVEEDORES.md)         |
| `Support`         | [MANUAL_SOPORTE.md](Manuales/MANUAL_SOPORTE.md)                 |

`Support` no tiene una pantalla independiente: es un rol privilegiado
asignado por un administrador para operar casos de bienestar e incidentes de
proveedores en las superficies administrativas autorizadas.

## Documentos de trabajo o historicos

Estos documentos siguen disponibles por trazabilidad, pero no son fuentes de
verdad sin una fecha de revision posterior a 2026-09-09:

- [TODOs.md](TODOs.md): auditoria historica de deuda; usar `ERRORES_PENDIENTES.md`.
- [pendientesTotales.md](pendientesTotales.md): backlog transversal historico.
- [sprint-plan-enterprise.md](sprint-plan-enterprise.md): plan de sprint completado.
- [ROLLOUT_SPRINT5.md](ROLLOUT_SPRINT5.md): rollout historico.
- [benchmark.md](benchmark.md): investigacion de mercado fechada.
- [sponsor.md](sponsor.md): propuesta de inversion.
- [influencer.md](influencer.md): material comercial temporal.

## Regla de mantenimiento

Toda nueva capacidad debe actualizar, en la misma entrega:

1. `STATUS.md` si cambia el estado del producto.
2. `FEATURES.md` si cambia un gate o una capacidad comercial.
3. `PRODUCT_STRATEGY_TOP1.md` si cambia una apuesta estrategica.
4. Un runbook o manual si cambia una operacion humana.
5. Pruebas y evidencia reproducible.

No crear otro documento maestro ni otra tabla de precios sin actualizar este
indice y retirar la fuente duplicada.
