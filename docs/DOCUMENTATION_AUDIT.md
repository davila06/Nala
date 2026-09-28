# Auditoría de documentación de NALA

**Corte:** 2026-09-28.  
**Criterio:** código/configuración/migraciones > pruebas > infraestructura > documentación. Un documento puede ser útil aunque sea histórico; en ese caso no es fuente vigente.

## Clasificación por grupos

| Grupo/documentos | Estado | Motivo y fuente recomendada |
| --- | --- | --- |
| `PRODUCT_SCOPE.md`, `INTEGRATIONS.md`, `TESTING.md`, `KNOWN_LIMITATIONS.md` | `ACTUALIZADO` con revisiones de corte | Separan código, pruebas y operación no verificada; mantener sincronizados con matriz |
| `FEATURES.md` | `PARCIALMENTE ACTUALIZADO` | Conserva contrato histórico/comercial y ahora enlaza estado real; requiere evitar claims no respaldados |
| `auditoria/FEATURE_TRACEABILITY_MATRIX.md` | `PARCIALMENTE ACTUALIZADO` | Tiene capacidades S01-S16 vigentes y F01-F30 históricos; la tabla secundaria F20-F30 conserva warnings de formato |
| `ARCHITECTURE.md`, `DATA_MODEL.md`, `CONFIGURATION.md`, `API.md`, `SECURITY.md` | `ACTUALIZADO/PARCIAL` | Son fuentes técnicas activas, pero la cobertura de endpoints, producción y proveedores no es exhaustiva |
| `PRICING_AND_PLANS.md` | `ACTUALIZADO como autoridad técnica/comercial condicionada` | Precios sujetos a aprobación; no confundir tiers/gates con venta pública |
| `B2B_ESTADO_ACTUAL.md`, `ADOPTIONS_CURRENT_STATE.md`, `COLLAR_CURRENT_STATE.md` | `PARCIALMENTE ACTUALIZADO` | Útiles por dominio; deben conservar límites de operación, contratos y hardware |
| `RUNBOOK_DEPLOYMENT.md` | `ACTUALIZADO` | Fuente canónica de deployment; IaC no demuestra despliegue |
| `GUIA_DEPLOY_PASO_A_PASO.md`, `pasos-para-ir-live.md` | `HISTÓRICO` | Mantener para trazabilidad; no usar como instrucción vigente sin reconciliar |
| `PRICING_AND_PLANS.md` | `ACTIVO` | Fuente consolidada; `planes.md`, `precios.md`, `pricing.md` quedan subordinados |
| `TODOs.md`, `TODOs_PENDIENTES.md`, `MASTER_TODO.md` y roadmaps | `HISTÓRICO/DE TRABAJO` | Un TODO no demuestra ausencia actual; cada item debe revalidarse contra código |
| `SECURITY.md` y `security/` | `ACTIVO CON RIESGO DE DUPLICACIÓN` | Consolidar navegación y fechas; no borrar registros de auditoría |
| `CUMPLIMIENTO_PROTECCION_DATOS.md` y `compliance/` | `ACTIVO CON RIESGO DE DUPLICACIÓN` | Separar controles implementados de aprobaciones PRODHAB/DPA pendientes |
| `docs/ai/` y estrategias | `PROPUESTA/CONOCIMIENTO` | No confundir skills, prompts o estrategia con agentes ejecutables |
| Nuevos `NALA_*.md` | `ACTUALIZADO AL CORTE` | Informes de esta auditoría; dependen de evidencia y deben fecharse |

## Contradicciones principales

1. Documentos comerciales/históricos describen planes o capacidades con lenguaje de producto, mientras el código muestra gates parciales y aprobación pendiente.
2. La presencia de controllers, DbSets, DI o Bicep se interpretó históricamente como operación; debe clasificarse como implementación o preparación, no como despliegue.
3. Telemedicina se confunde con consultas clínicas y `CallClient` interno; no existe ACS.
4. CRM interno se confunde con Dynamics 365.
5. Matching visual se presenta potencialmente como IA general; el repositorio no contiene RAG, copiloto, agente o diagnóstico.
6. Municipal/SENASA y certificados requieren convenio/aval separado del formato técnico.
7. TODOs fechados antes de la última ola de implementación contienen falsos pendientes; deben revalidarse antes de priorizar.

## Acciones documentales

- Usar [README.md](README.md), [PRODUCT_SCOPE.md](PRODUCT_SCOPE.md), [TESTING.md](TESTING.md) y la matriz como navegación técnica.
- Marcar documentos de precios/deployment antiguos como históricos sin borrarlos hasta una decisión de archivado.
- Mantener un único catálogo de planes y una única fuente de alcance.
- Añadir fecha de corte y estado a documentos nuevos.
- Validar enlaces internos y tablas en CI; corregir MD060/MD010 gradualmente sin mezclar cambios de contenido.
- No actualizar claims de producción, contratos, compliance o resultados de mercado solo porque existe una clase o un documento.

## Limitaciones de esta auditoría

No se inspeccionó un entorno productivo, no se probaron proveedores externos, no se ejecutaron migraciones ni se validó cada endpoint con una matriz BOLA exhaustiva. Los documentos de mercado y competidores son hipótesis estratégicas, no investigación primaria vigente.
