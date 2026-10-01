# UX Audit — NALA / PawTrack

**Corte:** 2026-10-01. **Fuentes:** `landing/frontend`, `PRODUCT_SCOPE.md`, `NALA_ACTUAL_PRODUCT_STATE.md`, `CLAIM_EVIDENCE_MATRIX.md`.

## Hallazgos críticos

- El landing es informativo y deriva a la PWA; no debe capturar reportes.
- La arquitectura tiene muchas rutas y puede sobrecargar a visitantes nuevos.
- La intención ya se segmenta en tutor, hallazgo y organización; debe ser el patrón dominante.
- El catálogo de planes debe depender del endpoint público aprobado, no de copy estático.
- Contacto en reportes tiene un hallazgo de ownership pendiente; no describirlo como relay anónimo.
- La página de registro del landing es legacy frente al login de PawTrack.

## Fricciones

| Fricción                                      | Impacto | Solución                                            |
| --------------------------------------------- | ------- | --------------------------------------------------- |
| Demasiadas capacidades en navegación          | Alto    | Tres entradas de intención + progressive disclosure |
| Reporte perdido requiere interpretar el flujo | Alto    | CTA directo a login con `return`                    |
| Capacidades parciales parecen equivalentes    | Alto    | Estados visible/partial/future                      |
| Planes sin contexto operativo                 | Medio   | Catálogo API + empty state                          |
| Contacto sin canal oficial                    | Medio   | Página de estado, no formulario ficticio            |

## Recomendación

La home debe comportarse como un selector de siguiente acción, no como un inventario completo del producto.
