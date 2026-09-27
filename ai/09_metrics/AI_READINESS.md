# AI Readiness Scorecard

**Corte:** 2026-09-27. **Resultado documental:** **63/100**. Esta es una evaluación cualitativa del repositorio observado, no benchmark, certificación, métrica automática ni aprobación para producción.

## Rúbrica

0 = no hay base observable; 25 = ad hoc; 50 = algunos artefactos/configuración, brechas mayores; 75 = fuentes versionadas y controles/pruebas con brechas acotadas; 100 = evidencia operativa, owners, pruebas y mantenimiento continuos verificables. El valor es juicio explícito del corte y debe reevaluarse por los owners.

| Dimensión      | Puntaje | Justificación/evidencia                                                                                                                   | Brecha principal                                                                                                             |
| -------------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| Documentación  | 72      | Índice, alcance, arquitectura, datos, API, seguridad, dominios, IA y auditoría con fecha/enlaces. Ver [docs index](../../docs/README.md). | Duplicidades/históricos y cambios concurrentes; el nuevo hub aún requiere mantenimiento continuo.                            |
| Arquitectura   | 70      | Capas, composition roots, módulos y diagramas observables en [arquitectura](../../docs/ARCHITECTURE.md).                                  | Monolito y un DbContext compartido; decisiones sin aprobación ADR.                                                           |
| Seguridad      | 65      | Matriz de autorización, data map, MFA/auditoría y threat docs. Ver [SECURITY](../../docs/SECURITY.md).                                    | No hay cobertura integral de endpoints/producción; aprobaciones legales/proveedor no verificadas.                            |
| Testing        | 60      | Dos proyectos de pruebas y [corte registrado](../../docs/TESTING.md) con suite amplia.                                                    | Documentación reporta una falla frontend y no valida ediciones concurrentes, E2E o proveedores.                              |
| Observabilidad | 55      | Health checks/runbooks y métricas técnicas de baja cardinalidad registradas en [CHANGELOG](../../docs/CHANGELOG.md).                      | No se verificó telemetría productiva, SLO/outcomes ni trazas específicas de AI.                                              |
| Modularidad    | 65      | Carpetas/módulos de dominio y Application con CQRS/mediación.                                                                             | Persistencia compartida y dependencias entre dominios; límites no aislados como despliegues.                                 |
| Datos          | 58      | Diccionario, grants, consentimiento y políticas de retención documentadas en [DATA_MODEL](../../docs/DATA_MODEL.md).                      | Calidad/procedencia/owner por dataset, retención efectiva y permisos entre sedes requieren pruebas.                          |
| Integraciones  | 45      | Adaptadores y validation gates están documentados en [INTEGRATIONS](../../docs/INTEGRATIONS.md).                                          | Azure Vision, GPS, pagos y canales externos no verificados; gateway regulatorio NoOp.                                        |
| Automatización | 65      | Workflows versionados, Bicep, migrations y jobs; ver [.github/workflows](../../.github/workflows/) e [infra](../../infra/).               | Workflow ejecutado, deployment, release con evidencia y rollback no verificados en este corte.                               |
| Skills         | 75      | 30 skills locales en `.github/skills/` y `.agents/skills/`, con procedimientos y propiedad documental.                                    | No prueba invocación/ejecución; manifest `skills.json` es propuesto y su stack declara .NET 8 frente a documentación .NET 9. |

**Cálculo:** media aritmética de diez dimensiones: 630 / 10 = 63. Cada puntuación es redondeada a enteros; no se ponderó criticidad.

## Lectura ejecutiva

La base documental, modularidad y biblioteca de skills permiten comenzar gobernanza de agentes en entorno controlado. La preparación AI de runtime sigue baja: el matching por imágenes no tiene proveedor/benchmark verificado y no hay pipeline RAG/agente acreditado. No usar 63/100 como autorización para acceder a datos clínicos/GPS ni como claim comercial. Para casos AI, resolver primero consentimientos, procedencia, identidad/permisos, evaluación y monitoreo.

Fuentes transversales: [AI readiness previa](../../docs/ai/AI_READINESS.md), [plan de auditoría](../../docs/auditoria/DOCUMENTATION_UPDATE_PLAN.md), [brechas](../../docs/auditoria/DOCUMENTATION_GAP_REPORT.md), [governance](../../docs/ai/AI_GOVERNANCE.md).
