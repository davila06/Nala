# Auditoría UX/UI Enterprise de NALA

**Corte:** 2026-09-28  
**Fuente de verdad:** `frontend/src`, router React, estilos, componentes compartidos, tests y configuración. La documentación previa se usó solo como contexto, nunca para declarar una pantalla implementada.  
**Tipo de revisión:** inventario estático completo del router y de los módulos; revisión de profundidad de flujos de alto riesgo. No es certificación WCAG ni inspección visual en navegador.

## Dictamen Ejecutivo

El frontend es una aplicación React PWA amplia y funcional, con rutas públicas y autenticadas para identidad, recuperación, salud, marketplace y portales institucionales. Hay un lenguaje visual y tokens propios, navegación móvil fija, soporte de foco global, `prefers-reduced-motion`, estados offline y componentes compartidos. La mayor deuda enterprise no es falta de funcionalidad visible, sino consistencia y validación: superficies repetidas implementan formularios/dialogs de manera distinta, la accesibilidad automatizada cubre una fracción pequeña del producto y la jerarquía móvil no expone de igual forma los destinos por rol.

**Estado global UX/UI:** `PARCIALMENTE_VALIDADO`. Inventario completo de rutas declaradas y source files; UX y responsive revisados por código y patrones, pero no todas las pantallas fueron ejecutadas en navegador con datos reales. No se afirma cumplimiento WCAG 2.2 AA ni mobile-first certificado.

## Inventario Automático

- Router: 80 declaraciones `path` en `frontend/src/app/routes.tsx`, incluidas rutas comodín y parámetros dinámicos; 77 archivos `*Page.tsx` en `frontend/src/features`.
- Fuente: 355 archivos TS/TSX bajo `frontend/src`; 36 usos de `<form>` detectados en páginas/features.
- Presentación: `globals.css`, Tailwind 4 y tokens `@theme`; 19 componentes compartidos en `frontend/src/shared/ui`.
- Pruebas: 54 archivos Vitest y 156 tests pasaron en esta ejecución. 12 specs Playwright existen; `ui-accessibility.spec.ts` contiene solo 2 pruebas de skip link y CTA. No se encontró integración `axe` ejecutada.
- Roles/superficies: Owner, Clinic, Store, ServiceProvider, Ally, Municipality, Admin y SuperAdmin.

**Rutas agrupadas:** autenticación y alta (`/login`, `/register`, `/forgot-password`, `/reset-password`, `/verify-email`, `/registro-negocio`); perfil/mascotas/salud (`/dashboard`, `/pets/new`, `/pets/:id`, `/pets/:id/edit`, `/salud`, `/perfil`, `/red`); recuperación (`/pets/:id/report-lost`, `/pets/:id/lost-confirmed`, `/lost/:id/case`, `/lost/:lostEventId/busqueda`, `/map`, `/map/match`, `/p/:id`, `/p/:id/report-sighting`, `/encontre`, `/encontre-mascota`, `/encontre-mascota/resultados`, `/notifications`, `/chat/*`); B2B (`/clinicas`, `/clinicas/:clinicId`, `/clinica/*`, `/tiendas`, `/tienda/*`, `/servicios`, `/servicios/:id`, `/servicio/*`, `/mis-reservas`, `/mis-pedidos`); adopción/social (`/adopciones/*`, `/shelter/*`, `/allies/panel`, `/bienestar/reportar`, `/campanas-castracion*`); institucional (`/municipalidad*`, `/reportes-institucionales`, `/nala`, `/admin`, `/super-admin`, `/estadisticas`); verificación pública (`/verificar/:code`, `/verificar/pasaporte/:code`). El inventario literal está en `src/app/routes.tsx`.

## Hallazgos Prioritarios

| Prioridad | Hallazgo verificable                                                                                                                                                                                                              | Impacto                                              |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| P0        | Tokens de texto presentan contrastes insuficientes en pares de uso representativos: `sand-400` sobre blanco ≈2.00:1, `sand-500` ≈2.98:1, `brand-500` ≈3.71:1. Hay usos pequeños (10-12px) en navegación, metadatos y botones.     | WCAG 2.2 AA, legibilidad móvil y confianza           |
| P0        | El reporte de pérdida es un flujo de 3 pasos, pero el stepper no expone `aria-current`/progreso; “Siguiente” cambia de paso sin validación local y el contenido anterior se desmonta.                                             | Flujo de emergencia y usuarios de lector de pantalla |
| P1        | `OnboardingWizard` se declara modal, pero no implementa trapping/restauración de foco ni cierre Escape; el componente compartido Modal sí implementa esas funciones.                                                              | Bloqueo de teclado/lector de pantalla al primer uso  |
| P1        | `QRFlipCard` usa `role="button"` con click y `Enter`, sin Space ni semántica de botón; el gesto swipe es alternativo, pero la tarjeta captura clicks que podrían proceder de controles descendientes.                             | Descubribilidad, teclado y uso de QR                 |
| P1        | NFC se presenta como “configurar chip” pero el flujo es tutorial externo para escribir una URL con NFC Tools, condicionado a pedido `NfcQrCombo` entregado; no valida escritura ni lectura dentro de NALA.                        | Expectativa de producto y soporte                    |
| P1        | La navegación móvil inferior solo ofrece Mascota, Encontrar, Salud y Red; accesos de rol a portales están en la navegación desktop “Más” y la experiencia móvil no tiene una navegación equivalente evidente.                     | Tareas B2B en móvil y descubribilidad                |
| P1        | El CTA global para reportar pérdida en `AuthenticatedLayout` está `hidden md:flex`; la ruta de dashboard tiene selección contextual, pero el CTA de emergencia del header desaparece en viewport móvil.                           | Tiempo a acción en el flujo crítico                  |
| P1        | El onboarding dice que IA reconocerá la mascota y la guía de collar afirma capacidades de placa/QR; la UI debe explicar límites, disponibilidad por plan y dependencia de servicios sin prometer éxito de reconocimiento/entrega. | Conversión de confianza, claims y soporte            |
| P2        | Accesibilidad E2E cubre 2 aserciones sobre el shell, no formularios/dialogs/mapas por rol ni WCAG automatizado; no hay `axe` integrado en la suite.                                                                               | Riesgo de regresión y evidencia insuficiente         |
| P2        | Existe un UI kit, pero `Modal` y `Drawer` duplican implementación; hay formularios que usan `Input` compartido y otros `<input>`/errores/toasts ad hoc.                                                                           | Consistencia, velocidad de cambio y deuda QA         |
| P2        | Los estados de error de alta y transacciones recurren a mensajes genéricos/toasts; falta un patrón uniforme de error por campo, recuperación, foco y preservación de valores.                                                     | Conversión y abandono de formularios                 |

## Evaluación por Dimensión

| Dimensión                             | Evaluación                                                                                                                                                                                  | Confianza                               |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| UX / arquitectura de información      | Funcionalidad amplia, pero los portales por rol y destinos de alto valor compiten por navegación. Hay que validar descubribilidad con tareas observadas.                                    | Media                                   |
| UI / design system                    | Paleta, tipografías Fraunces/Plus Jakarta Sans, escalas y componentes compartidos presentes. Se observa variación en radios, colores, toasts y formularios por módulo.                      | Alta en tokens; media en cobertura real |
| WCAG 2.2                              | Hay skip link, landmarks, foco global, reduced-motion, labels, nombres ARIA y progreso en algunos flujos. Persisten riesgos de contraste, stepper, diálogos, keyboard parity y target size. | Media-baja; sin axe/lector de pantalla  |
| Responsive / Mobile First             | Diseño adaptable con bottom nav, `dvh`, safe area y grids Tailwind; no se ejecutó inspección en viewports. Menú por rol y flujo de emergencia requieren prueba móvil.                       | Media-baja                              |
| Onboarding                            | Wizard para Owner sin mascota y onboarding de páginas de alta; claims de IA/QR y modal incompleto en teclado. No hay evidencia de experimentos/cohortes.                                    | Media                                   |
| Conversión / PLG                      | CTA contextual, gates y upsell visible; no hay datos de funnel/retención en la auditoría que permitan probar conversión. Evitar upsell durante una crisis de mascota perdida.               | Baja para impacto cuantitativo          |
| Design System                         | Tokens y shared UI existen; falta contrato de componentes, auditoría de contraste y matriz de estados/targets.                                                                              | Alta                                    |
| Telemedicina                          | No existe flujo de vídeo/voz; no debe evaluarse como experiencia disponible. UI clínica actual cubre operación/consulta registrada.                                                         | Alta                                    |
| QR/NFC                                | QR visible en perfil, descarga y componente flip; NFC es onboarding a herramienta externa, no integración.                                                                                  | Alta                                    |
| Veterinarias/refugios/municipalidades | Portales ejecutables por rol, agenda/expediente/adopción/capturas; revisión estática, sin pruebas moderadas con personal real ni end-to-end de tareas completas.                            | Media                                   |

## Verificación Ejecutada

- `npm run typecheck`: pasó.
- `npm test -- --run --reporter=dot --pool=threads --maxWorkers=1`: 54 archivos, 156 tests aprobados.
- `npm run lint`: falló con 3 errores en `tests/features/clinics/ClinicOperationsPanel.test.tsx` (2 `no-unsafe-assignment`, 1 `no-unnecessary-type-assertion`). No son evidencia de problemas de UI de producción, pero dejan lint rojo.
- Playwright visual/WCAG no ejecutado: requiere runtime API/DB/Azurite configurados y usuarios de prueba. E2E actual configura solo Chromium desktop por defecto.

## Decisión

Tratar esta entrega como auditoría estática de frontend. Resolver primero contraste, teclado/semántica en wizard/QR y acceso móvil de portales; añadir axe/Playwright con datos semilla y pruebas manuales WCAG 2.2 AA antes de declarar cumplimiento enterprise. El backlog, Top 100, quick wins y matriz de oportunidades están en los documentos hermanos `UIUX_BACKLOG.md`, `UIUX_TOP_100_IMPROVEMENTS.md`, `UIUX_QUICK_WINS.md` y `UIUX_OPPORTUNITY_MATRIX.md`.
