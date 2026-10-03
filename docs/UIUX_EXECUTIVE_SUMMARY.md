# Auditoría UX/UI Enterprise de NALA

**Auditoría base:** 2026-09-28
**Revalidación focal:** 2026-10-02
**Fuente de verdad:** `frontend/src`, router React, estilos, componentes compartidos, tests y configuración. La documentación previa se usó solo como contexto, nunca para declarar una pantalla implementada.  
**Tipo de revisión:** inventario estático completo del router y de los módulos; revisión de profundidad de flujos de alto riesgo. No es certificación WCAG ni inspección visual en navegador.

## Dictamen Ejecutivo

El frontend es una aplicación React PWA amplia y funcional, con rutas públicas y autenticadas para identidad, recuperación, salud, marketplace y portales institucionales. Hay un lenguaje visual y tokens propios, navegación móvil fija, soporte de foco global, `prefers-reduced-motion`, estados offline y componentes compartidos. La mayor deuda enterprise no es falta de funcionalidad visible, sino consistencia y validación: superficies repetidas implementan formularios/dialogs de manera distinta, la accesibilidad automatizada cubre una fracción pequeña del producto y la jerarquía móvil no expone de igual forma los destinos por rol.

**Estado global UX/UI:** `PARCIALMENTE_VALIDADO`. Inventario completo de rutas declaradas y source files; UX y responsive revisados por código y patrones, pero no todas las pantallas fueron ejecutadas en navegador con datos reales. No se afirma cumplimiento WCAG 2.2 AA ni mobile-first certificado.

## Inventario Automático

- Router: 80 declaraciones `path` en `frontend/src/app/routes.tsx`, incluidas rutas comodín y parámetros dinámicos; 77 archivos `*Page.tsx` en `frontend/src/features`.
- Fuente: 355 archivos TS/TSX bajo `frontend/src`; 36 usos de `<form>` detectados en páginas/features.
- Presentación: `globals.css`, Tailwind 4 y tokens `@theme`; 19 componentes compartidos en `frontend/src/shared/ui`.
- Pruebas: la auditoría base del 28-sep-2026 registró conteos incompatibles entre documentos; no se deben citar como total vigente. Al 02-oct-2026, `ui-accessibility.spec.ts` integra Axe y contiene escenarios de shell, onboarding, reporte, QR, mapa y navegación móvil. La cobertura no equivale a un análisis completo.
- Roles/superficies: Owner, Clinic, Store, ServiceProvider, Ally, Municipality, Admin y SuperAdmin.

**Rutas agrupadas:** autenticación y alta (`/login`, `/register`, `/forgot-password`, `/reset-password`, `/verify-email`, `/registro-negocio`); perfil/mascotas/salud (`/dashboard`, `/pets/new`, `/pets/:id`, `/pets/:id/edit`, `/salud`, `/perfil`, `/red`); recuperación (`/pets/:id/report-lost`, `/pets/:id/lost-confirmed`, `/lost/:id/case`, `/lost/:lostEventId/busqueda`, `/map`, `/map/match`, `/p/:id`, `/p/:id/report-sighting`, `/encontre`, `/encontre-mascota`, `/encontre-mascota/resultados`, `/notifications`, `/chat/*`); B2B (`/clinicas`, `/clinicas/:clinicId`, `/clinica/*`, `/tiendas`, `/tienda/*`, `/servicios`, `/servicios/:id`, `/servicio/*`, `/mis-reservas`, `/mis-pedidos`); adopción/social (`/adopciones/*`, `/shelter/*`, `/allies/panel`, `/bienestar/reportar`, `/campanas-castracion*`); institucional (`/municipalidad*`, `/reportes-institucionales`, `/nala`, `/admin`, `/super-admin`, `/estadisticas`); verificación pública (`/verificar/:code`, `/verificar/pasaporte/:code`). El inventario literal está en `src/app/routes.tsx`.

## Hallazgos revalidados (2026-10-02)

| Prioridad | Hallazgo revalidado                                                                                                                                            | Impacto                                      |
| --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------- |
| P0        | Se corrigieron badge Adoptado y colores de avatar detectados por Axe; falta auditar globalmente tokens y pares de texto pequeños.                              | WCAG 2.2 AA, legibilidad móvil y confianza   |
| P0        | El reporte ya anuncia paso, expone `aria-current` y valida/focaliza la fecha antes de avanzar. No se ha probado con lector de pantalla real.                   | Flujo de emergencia y tecnología asistiva    |
| P1        | Onboarding comparte el manejo de foco del modal; Escape/retorno se probaron unitariamente. Anuncio de pasos con lector real sigue sin verificar.               | Accesibilidad del primer uso                 |
| P1        | QRFlipCard usa botones nativos; Enter/Espacio pasan prueba unitaria y Axe en E2E. Falta dispositivo/usuario real.                                              | Descubribilidad, teclado y uso de QR         |
| P1        | NFC se describe como escritura manual con app externa; compatibilidad por dispositivo y operación real no están verificadas.                                   | Expectativa de producto y soporte            |
| P1        | El menú móvil muestra enlaces de rol. Playwright pasó Owner, Clinic, Store y ServiceProvider; Ally, Municipality, Admin y tareas observadas siguen pendientes. | Descubribilidad B2B móvil                    |
| P1        | El CTA de pérdida está en navegación inferior móvil y pasó el recorrido Owner de Playwright. La facilidad en una emergencia requiere prueba con usuarios.      | Tiempo a acción en el flujo crítico          |
| P1        | Copy de onboarding limita resultados de IA/QR; disponibilidad por plan y claims de GPS requieren revisión de todas las superficies.                            | Conversión de confianza, claims y soporte    |
| P2        | Axe está integrado en Playwright/CI con cobertura selectiva; no es auditoría WCAG integral ni certifica conformidad.                                           | Riesgo de regresión y evidencia insuficiente |
| P2        | `Modal` y `Drawer` comparten hook de foco, pero siguen siendo primitives distintas; formularios/errores también varían por módulo.                             | Consistencia, velocidad de cambio y deuda QA |
| P2        | La consistencia de errores y recuperación de formularios sigue pendiente de validar por página/flujo.                                                          | Conversión y abandono de formularios         |

## Evaluación por Dimensión

| Dimensión                             | Evaluación                                                                                                                                                                    | Confianza                               |
| ------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| UX / arquitectura de información      | Funcionalidad amplia, pero los portales por rol y destinos de alto valor compiten por navegación. Hay que validar descubribilidad con tareas observadas.                      | Media                                   |
| UI / design system                    | Paleta, tipografías Fraunces/Plus Jakarta Sans, escalas y componentes compartidos presentes. Se observa variación en radios, colores, toasts y formularios por módulo.        | Alta en tokens; media en cobertura real |
| WCAG 2.2                              | Axe se ejecutó en journeys seleccionados. Persisten riesgos de contraste y falta teclado/lector real, targets y validación de estados/rutas no cubiertos.                     | NO_VERIFICADO como conformidad global   |
| Responsive / Mobile First             | Playwright pasó 4 menús por rol a 390×844; no se ejecutó la matriz visual completa ni todas las rutas.                                                                        | NO_VERIFICADO fuera de esa muestra      |
| Onboarding                            | Se comprobaron Escape/retorno de foco por unit test y Axe en el diálogo; lector real, uso observado y experimentos/cohortes siguen pendientes.                                | Parcial                                 |
| Conversión / PLG                      | CTA contextual, gates y upsell visible; no hay datos de funnel/retención en la auditoría que permitan probar conversión. Evitar upsell durante una crisis de mascota perdida. | Baja para impacto cuantitativo          |
| Design System                         | Tokens y shared UI existen; falta contrato de componentes, auditoría de contraste y matriz de estados/targets.                                                                | Alta                                    |
| Telemedicina                          | No existe flujo de vídeo/voz; no debe evaluarse como experiencia disponible. UI clínica actual cubre operación/consulta registrada.                                           | Alta                                    |
| QR/NFC                                | QR visible en perfil, descarga y componente flip; NFC es onboarding a herramienta externa, no integración.                                                                    | Alta                                    |
| Veterinarias/refugios/municipalidades | Portales ejecutables por rol, agenda/expediente/adopción/capturas; revisión estática, sin pruebas moderadas con personal real ni end-to-end de tareas completas.              | Media                                   |

## Verificación Ejecutada (2026-10-02)

- `npm --prefix frontend run test:e2e -- --grep "mobile menu reaches"`: 4/4 pasaron en Chromium a 390×844 para Owner, Clinic, Store y ServiceProvider. La prueba verifica acceso por teclado, `href` esperado y Axe en la navegación móvil; no completa tareas del portal.
- Pruebas unitarias focalizadas de accesibilidad: 7/7 pasaron. `AnimalCard.test.tsx`: 3/3 pasaron.
- Axe detectó inicialmente contraste de 3.26:1 (`rescue-500`) y 4.12:1 (`purple-500`) en avatares con texto blanco. Se oscureció la paleta y los cuatro recorridos pasaron al repetir la prueba.
- El workflow `.github/workflows/e2e.yml` ejecuta Playwright. No se consultó el resultado de la última corrida CI; tampoco se ejecutaron la suite completa frontend ni todo E2E en esta revalidación.
- No hubo sesiones con usuarios, lector de pantalla real ni auditoría WCAG completa. Esos resultados y el cumplimiento global permanecen `NO_VERIFICADO`.

## Decisión

Tratar esta entrega como revalidación focal, no certificación. La navegación móvil de cuatro roles pasó, pero WCAG global, lector de pantalla y facilidad de encontrar tareas con personas siguen `NO_VERIFICADO`. El estado por hallazgo está en `UIUX_BACKLOG.md`; completar los perfiles y pruebas indicados antes de declarar cumplimiento enterprise.
