# Contexto del repositorio

**Corte:** 2026-09-27. **Propósito:** cargar contexto conciso para agentes sin reemplazar fuentes canónicas.

## Hechos del sistema

- La solución [PawTrack.sln](../PawTrack.sln) contiene API, Application, Domain, Infrastructure y proyectos de pruebas unitarias e integración. [HashGen](../backend/HashGen/HashGen.csproj) es auxiliar y está fuera de la solución.
- El backend es un monolito desplegable, organizado por módulos/capas; comparte un `PawTrackDbContext`. No hay evidencia en la solución de microservicios con despliegue o persistencia independiente. [Arquitectura](../docs/ARCHITECTURE.md) · [modelo de datos](../docs/DATA_MODEL.md).
- El frontend es una PWA React/TypeScript en [frontend](../frontend/package.json), con rutas y features bajo `frontend/src/`.
- [Bicep](../infra/main.bicep) y workflows de [.github/workflows](../.github/workflows/) son configuración versionada; no prueban recursos productivos desplegados ni ejecución CI exitosa.
- El dominio combina identidad de mascotas, recuperación, salud, collares/GPS, actores comunitarios e institucionales, catálogo/reservas y suscripciones. El estado comprobable está en la [matriz de capacidades](../docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md).

## Incertidumbres importantes

La matriz del corte no valida todos los métodos API ni los proveedores en producción. El matching Azure Vision está `NO_VERIFICADO` como servicio externo; agentes/RAG son `PROPUESTO`; telemedicina remota aparece `DECLARADO_NO_IMPLEMENTADO`; el envío regulatorio institucional no está implementado. Ver [limitaciones](../docs/KNOWN_LIMITATIONS.md), [integraciones](../docs/INTEGRATIONS.md) y [preparación IA](../docs/ai/AI_READINESS.md).

El árbol Git al iniciar esta documentación contenía cambios concurrentes de salud, frontend y documentación. Este corte no los valida ni los atribuye a este hub. No se ejecutaron compilaciones, pruebas, migraciones, despliegues o llamadas a proveedores para esta entrega documental.

## Regla para agentes

Usar [Evidence First](README.md), leer el documento canónico del dominio solicitado, comprobar si hay cambios locales y reportar alcance/no verificado. Nunca convertir la visión, estrategia, capacidades comerciales o historial del repositorio en estado operativo sin pruebas.
