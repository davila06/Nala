# PawTrack CR - Mapa oficial de documentacion

> Fuente de navegacion oficial. Actualizado: 2026-09-09.

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

| Documento                                                                | Proposito                                   |
| ------------------------------------------------------------------------ | ------------------------------------------- |
| [STATUS.md](STATUS.md)                                                   | Estado tecnico y operativo verificado       |
| [PRODUCT_STRATEGY_TOP1.md](PRODUCT_STRATEGY_TOP1.md)                     | Estrategia, north star, moat y roadmap      |
| [PRODUCT_P0_ENTERPRISE_TODOLIST.md](PRODUCT_P0_ENTERPRISE_TODOLIST.md)   | Backlog y gates P0 enterprise               |
| [FEATURES.md](FEATURES.md)                                               | Matriz de capacidades por plan              |
| [PRICING_AND_PLANS.md](PRICING_AND_PLANS.md)                             | Fuente comercial consolidada                |
| [PawTrack_Documento_Maestro_v3.1.md](PawTrack_Documento_Maestro_v3.1.md) | Arquitectura y especificacion consolidada   |
| [pruebas.md](pruebas.md)                                                 | Usuarios, runtime local y validacion manual |
| [RUNBOOK_OPERACIONES.md](RUNBOOK_OPERACIONES.md)                         | Operacion, incidentes y continuidad         |
| [POLITICA_DE_PRIVACIDAD.md](POLITICA_DE_PRIVACIDAD.md)                   | Politica de privacidad para revision legal  |
| [TERMINOS_DE_USO.md](TERMINOS_DE_USO.md)                                 | Terminos de uso para revision legal         |
| [ERRORES_PENDIENTES.md](ERRORES_PENDIENTES.md)                           | Deuda tecnica y gates de calidad            |
| [API_AUTHORIZATION_MATRIX.md](API_AUTHORIZATION_MATRIX.md)               | Ownership, BOLA/IDOR y versionado API      |
| [pendientesTiendas.md](pendientesTiendas.md)                             | Backlog enterprise de tiendas               |
| [legal.md](legal.md)                                                     | Borrador legal y operativo de tiendas       |

## Documentacion por dominio

- Recuperacion y NALA: [NALA.md](NALA.md), [QR.md](QR.md),
  [MANUAL_NALA.md](MANUAL_NALA.md), [RUNBOOK_REPORTES_INSTITUCIONALES.md](RUNBOOK_REPORTES_INSTITUCIONALES.md).
- Salud y clinicas: [expediente.md](expediente.md), [CUMPLIMIENTO_PROTECCION_DATOS.md](CUMPLIMIENTO_PROTECCION_DATOS.md), [senasa.md](senasa.md).
- Collares: [collarFinal.md](collarFinal.md), [jimiiot.md](jimiiot.md).
- B2B/B2G: [B2B_ESTADO_ACTUAL.md](B2B_ESTADO_ACTUAL.md), [todolist-b2b-enterprise.md](todolist-b2b-enterprise.md).
- Servicios profesionales: [SERVICE_PROVIDERS_ENTERPRISE_TODOLIST.md](SERVICE_PROVIDERS_ENTERPRISE_TODOLIST.md), [SERVICE_PROVIDERS_OPERABILITY.md](SERVICE_PROVIDERS_OPERABILITY.md), [SERVICE_PROVIDERS_THREAT_MODEL.md](SERVICE_PROVIDERS_THREAT_MODEL.md).
- Pruebas y despliegue: [GUIA_ONBOARDING_DEV.md](GUIA_ONBOARDING_DEV.md), [GUIA_DEPLOY_PASO_A_PASO.md](GUIA_DEPLOY_PASO_A_PASO.md), [pruebas.md](pruebas.md).

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
