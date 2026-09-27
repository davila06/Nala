# ADR-007: Acceso público a algunas fotos Blob

**Estado:** DRAFT, requiere revisión de seguridad. **Fecha/owner/aprobación:** no verificados. **Fuente inicial:** [MANUAL_TECNICO.md §20](../Manuales/MANUAL_TECNICO.md).

## Contexto

El manual afirma que fotos de perfil/sightings se sirven por URLs públicas y que no contienen PII. El adaptador y la configuración deben revisarse por contenedor, tipo de foto, datos incrustados y acceso actual.

## Decisión registrada, pendiente de ratificación

El texto legado describe acceso público para ciertas fotos con finalidad de perfil público. No aprobar una política general de contenedores públicos ni asumir que todas las fotos son inocuas.

## Alternativas

El manual no documenta una evaluación de URLs firmadas/temporales, proxy autenticado, recorte de metadatos ni contenedores privados. Son alternativas para evaluar; no se afirma que fueran consideradas en la decisión original.

## Consecuencias y riesgos

Acceso anónimo mejora lectura de perfiles pero puede permitir descarga/copia y amplificar exposición de imágenes/metadatos. Clasificar por propósito, controlar contenido, revisar borrado/cache/retención y evitar coordenadas/PII en metadatos.

## Evidencia

[Manual técnico](../Manuales/MANUAL_TECNICO.md), [BlobStorageService](../../backend/src/PawTrack.Infrastructure/Storage/BlobStorageService.cs), [integraciones](../INTEGRATIONS.md), [mapa de datos](../security/PRIVACY_DATA_MAP.md). La política exacta por contenedor queda `NO_VERIFICADO` en este ADR.
