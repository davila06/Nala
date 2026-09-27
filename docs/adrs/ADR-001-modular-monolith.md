# ADR-001: Monolito modular frente a microservicios

**Estado:** DRAFT. **Fecha/owner/aprobación:** no verificados. **Fuente inicial:** [MANUAL_TECNICO.md §20](../Manuales/MANUAL_TECNICO.md).

## Contexto

El manual registra una solución organizada en módulos y MediatR, desplegada como una aplicación. Hoy la solución compila cuatro proyectos backend y los dominios permanecen en un monolito; véase [PawTrack.sln](../../PawTrack.sln) y [arquitectura](../ARCHITECTURE.md).

## Decisión registrada, pendiente de ratificación

El manual afirma monolito modular para favorecer velocidad con equipo pequeño y dejar extracción futura condicionada a carga. Este archivo no afirma que la dirección haya ratificado la decisión.

## Alternativas

El manual menciona microservicios como contraste; no aporta evaluación fechada de costos, límites de dominio o criterios cuantitativos. Las alternativas y umbrales de extracción quedan por documentar.

## Consecuencias

Persistencia y despliegue comparten componentes; extracción exige contratos, propiedad de datos y operación autónoma. No hay microservicios comprobados en la solución actual. Revisar [dependencias](../architecture/DEPENDENCIES.md).

## Evidencia y preguntas

Ver estructura de [Domain](../../backend/src/PawTrack.Domain/), [API](../../backend/src/PawTrack.API/) y [contexto EF](../../backend/src/PawTrack.Infrastructure/Persistence/PawTrackDbContext.cs). ¿Qué métricas y owner habilitan una decisión de extracción?
