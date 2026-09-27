---
name: dotnet-product-documentation-audit
description: Audita proyectos .NET y actualiza la carpeta /docs para reflejar fielmente las características, arquitectura, seguridad, integraciones, configuración, pruebas, limitaciones y alcance real implementado. Úsalo al revisar documentación desactualizada, validar funcionalidades o sincronizar documentos con el código.
---

# Auditoría de producto y documentación .NET

## Resultado y límites

Determina lo que el producto hace realmente y sincroniza `/docs` con evidencia verificable del repositorio. Trabaja de forma autónoma con la evidencia disponible; declara incertidumbres y no conviertas inferencias en hechos. Solo modifica archivos bajo `docs/`, incluido el plan previo. No modifiques código productivo, pruebas, infraestructura ni archivos de configuración fuera de `docs/`. No publiques secretos, tokens, credenciales, cadenas de conexión ni datos sensibles, tampoco en extractos de comandos o diffs.

## 1. Inventario completo antes de escribir

Antes de modificar cualquier archivo, lee las instrucciones del repositorio, comprueba el estado del árbol de trabajo y enumera **todos** los proyectos y soluciones; registra para cada proyecto su módulo, tipo y cobertura de inspección. Si el repositorio es grande, avanza por módulos con un registro de cobertura, pero no omitas proyectos. Examina:

- Soluciones, proyectos .NET, capas, referencias y dependencias.
- APIs, controllers, endpoints, rutas, contratos y sus implementaciones; commands, queries, handlers y servicios que deciden el comportamiento.
- Dominio, entidades, `DbContext`, configuración del modelo y migraciones.
- Autenticación, autorización, roles, policies y protección efectiva de las rutas.
- Jobs, colas, eventos, integraciones y sus adaptadores o consumidores reales.
- Frontend, rutas, componentes y llamadas a APIs, si existen.
- Pruebas, infraestructura, CI/CD, configuración y variables necesarias (solo nombres no sensibles).
- Todo el contenido actual de `docs/`, incluidos índices, documentos históricos y enlaces.

Rastrea cada capacidad de extremo a extremo: entrada o disparador, lógica efectiva, persistencia/integración, exposición al usuario y pruebas. La presencia de una interfaz, mock, TODO, opción de menú, contrato o documento por sí sola **no** demuestra que la capacidad funcione. Registra las zonas que no puedas inspeccionar.

## 2. Evidencia y clasificación

Ante discrepancias, usa esta jerarquía de fuentes de verdad: **código ejecutable > configuración y migraciones > pruebas > infraestructura > documentación existente**. Una prueba no sustituye al código que pretende probar; la infraestructura declarada no demuestra un despliegue operativo. Cita rutas relativas concretas del repositorio y explica qué prueban; verifica rutas y evita enlaces rotos. Distingue implementación comprobada por pruebas ejecutadas de implementación inferida por lectura. Si una prueba no se ejecutó o falló, no marques la característica como verificada.

Asigna exactamente un estado a cada característica:

| Estado                      | Criterio                                                                                    |
| --------------------------- | ------------------------------------------------------------------------------------------- |
| `IMPLEMENTADO_Y_VERIFICADO` | Flujo ejecutable completo y evidencia de pruebas pertinentes que pasaron en esta auditoría. |
| `IMPLEMENTADO_SIN_PRUEBAS`  | Flujo ejecutable completo, pero sin prueba pertinente ejecutada y aprobada.                 |
| `PARCIALMENTE_IMPLEMENTADO` | Faltan pasos necesarios, integración efectiva o parte del flujo.                            |
| `DECLARADO_NO_IMPLEMENTADO` | Solo hay intención, contrato, mock, TODO o documentación; no hay flujo ejecutable.          |
| `DESHABILITADO`             | Existe implementación, pero está inaccesible por configuración, flag o registro inactivo.   |
| `OBSOLETO`                  | Capacidad retirada o sustituida; no corresponde al alcance actual.                          |
| `NO_VERIFICADO`             | Evidencia insuficiente o acceso/ejecución impedidos para determinar el estado.              |

Si hay señales contrapuestas, explica la contradicción y el motivo de la clasificación sin suponer que producción se comporta igual que el entorno local.

## 3. Comprobaciones seguras

Cuando sea seguro y haya SDK, acceso y tiempo razonables, ejecuta en cada solución/proyecto pertinente (o una solución que los cubra) `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet list package` y `dotnet list package --vulnerable`. Adapta la sintaxis a la versión del SDK. Inspecciona los comandos antes de ejecutarlos: no ejecutes pruebas con efectos externos, despliegues, migraciones ni comandos que requieran secretos, servicios reales o escrituras peligrosas. Si una comprobación pudiera modificar archivos versionados fuera de `docs/`, omítela e indica el motivo; no reviertas cambios preexistentes. Registra comando, alcance, resultado, pruebas omitidas y limitaciones de entorno. Una auditoría estática no equivale a pruebas pasadas. Trata vulnerabilidades de dependencias como hallazgos, sin cambiar paquetes.

## 4. Plan previo y actualización de documentos

Tras el inventario y **antes de actualizar cualquier otro documento**, crea `docs/auditoria/DOCUMENTATION_UPDATE_PLAN.md` con: inventario y cobertura por proyecto/módulo, fuentes consultadas, discrepancias, documentos que crearás/actualizarás/archivarás, orden de trabajo, riesgos, comprobaciones previstas y elementos no verificables. Revisa el plan conforme aparezca evidencia sin sustituirlo por un listado de aspiraciones. A partir de ahí modifica únicamente `docs/`.

Crea o actualiza, como mínimo:

- `docs/README.md`: índice y convenciones de lectura.
- `docs/PRODUCT_SCOPE.md`: alcance actual, exclusiones y límites del producto.
- `docs/FEATURES.md`: catálogo de características y estados.
- `docs/ARCHITECTURE.md`: capas, módulos, dependencias y flujos reales.
- `docs/API.md`: endpoints, autenticación, contratos y disponibilidad comprobable.
- `docs/SECURITY.md`: controles efectivos y brechas comprobadas.
- `docs/CONFIGURATION.md`: requisitos y nombres de variables sin valores sensibles.
- `docs/DATA_MODEL.md`: entidades, relaciones, contextos y migraciones.
- `docs/INTEGRATIONS.md`: proveedores, flujos y estado operativo verificable.
- `docs/TESTING.md`: cobertura, comandos, resultados y pruebas pendientes.
- `docs/KNOWN_LIMITATIONS.md`: restricciones y comportamientos no resueltos.
- `docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md`: característica, estado, rutas de evidencia, pruebas y documentación de destino.
- `docs/auditoria/DOCUMENTATION_GAP_REPORT.md`: afirmaciones desactualizadas, contradicciones, incertidumbres y prioridad.
- `docs/auditoria/DOCUMENTATION_CHANGELOG.md`: cambios documentales realizados y motivo.

Para **cada** característica documenta objetivo y valor de negocio, actores, flujo principal, reglas y validaciones, backend y frontend, persistencia, seguridad, integraciones, configuración, pruebas, limitaciones, estado real y evidencia con rutas relativas. Usa "no aplica" o "no verificado" con motivo cuando corresponda; no rellenes vacíos con conjeturas. Puedes centralizar las fichas detalladas y enlazarlas desde el índice y la matriz para evitar duplicación.

Separa explícitamente **alcance actual**, **funcionalidades parciales**, **exclusiones**, **roadmap**, **backlog**, **deuda técnica** y **decisiones propuestas**. No presentes planes como funcionalidades disponibles. Conserva información histórica útil: mueve documentos reemplazados a `docs/archive/` o márcalos visiblemente como obsoletos y enlaza su reemplazo. Respeta cambios existentes del usuario, evita duplicar archivos que ya cumplan la función y deja rastro de cada sustitución en el changelog. Usa Markdown válido, enlaces relativos y diagramas Mermaid solo cuando aclaren arquitectura o flujos reales.

## 5. Control final y entrega

Revisa el diff completo de `docs/` y comprueba que no hubo modificaciones versionadas fuera de esa carpeta durante el trabajo. Valida enlaces internos y anclas, coherencia entre documentos, estado asignado a toda característica, respaldo de afirmaciones en evidencia, separación de estado actual y roadmap, y ausencia de secretos o datos sensibles. Informa resultados de compilación y pruebas, o las razones precisas por las que no se pudieron ejecutar. Corrige los problemas documentales detectados y vuelve a comprobar lo afectado.

En la respuesta final indica archivos creados, modificados y archivados; conteo de características por estado; contradicciones detectadas; resultados de compilación, pruebas y dependencias; áreas no verificables; riesgos y recomendaciones priorizadas. No declares verificación completa cuando queden módulos, proyectos o validaciones pendientes.
