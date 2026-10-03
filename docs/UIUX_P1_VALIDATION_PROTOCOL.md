# Validación P1 de navegación y tareas frecuentes

**Auditoría base:** 2026-09-28. **Revalidación focal:** 2026-10-02. La navegación móvil/Axe pasó para cuatro roles; sesiones con personas, lector de pantalla real y WCAG global siguen `NO_VERIFICADO`. No tomar decisiones de rediseño de portales con automatización como único insumo.

## Preparación

- Convocar al menos una persona por rol (tutor, veterinaria, refugio/aliado, tienda, proveedor y municipalidad) que use tareas reales de su rol. No usar datos reales de mascotas, pacientes, cobros ni capturas: preparar cuentas y registros ficticios aislados.
- Probar a 390 CSS px, zoom 200% y teclado sin mouse; repetir el recorrido con lector de pantalla en al menos un dispositivo móvil. Observar sin indicar dónde está el menú ni dictar la ruta.
- Registrar por tarea: tiempo hasta encontrar el acceso, pasos, bloqueos, errores, abandonos, éxito y comentarios expresados; no registrar información personal, contenido clínico ni coordenadas. Anotar dispositivo, lector, fecha y versión.
- Criterio de aceptación por módulo: tarea alcanzable sin URL directa; foco visible y lógico, Escape cancela sin mutar, revisión antes de acción irreversible, valores conservados ante cancelación/error, Axe WCAG 2.2 A/AA sin violaciones en estados inicial, error y confirmación, y comprobación manual de nombres/anuncios con lector de pantalla. Un test Axe aislado no certifica WCAG.

## Guion de tareas

| Rol                            | Tarea a observar sin dar instrucciones de navegación                                                   | Ruta esperada en código                         | Evidencia automática actual                                                                                            |
| ------------------------------ | ------------------------------------------------------------------------------------------------------ | ----------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Tutor (`Owner`)                | Seleccionar mascota y empezar reporte de pérdida; volver sin perder el paso                            | `/dashboard?action=report-lost`                 | Playwright móvil 390×844 pasó (menú, href y Axe en navegación); no prueba completar reporte ni volver sin perder datos |
| Veterinaria (`Clinic`)         | Entrar al portal, localizar expediente/autorización y revisar un cierre de caja ficticio               | `/clinica/portal`, `/clinica/caja`              | Playwright móvil 390×844 pasó menú/href/Axe de navegación; no prueba expediente ni caja en el recorrido E2E            |
| Refugio/aliado (`Ally`)        | Encontrar el panel y publicar una ficha ficticia; interrumpir y recuperar borrador sin notas clínicas  | `/allies/panel`, `/shelter/publicar`            | [borrador](../frontend/tests/features/adoptions/ShelterPublishPage.test.tsx)                                           |
| Tienda (`Store`)               | Encontrar portal y órdenes sin usar URL directa                                                        | `/tienda/portal`, `/tienda/portal/ordenes`      | Playwright móvil 390×844 pasó menú/href/Axe de navegación; no prueba órdenes ni flujo comercial                        |
| Proveedor (`ServiceProvider`)  | Encontrar portal y reservas entrantes; distinguir confirmar/rechazar y cancelar inasistencia sin mutar | `/servicio/portal`, `/servicio/portal/reservas` | Playwright móvil 390×844 pasó menú/href/Axe de navegación; acciones de reserva y cancelación requieren otro E2E        |
| Municipalidad (`Municipality`) | Entrar al portal y registrar/revisar una captura ficticia, cancelar y reanudar                         | `/municipalidad/portal`                         | [capturas](../frontend/tests/features/admin/MunicipalDashboardPage.test.tsx)                                           |

Las rutas describen implementación, no resultados con personas. La fixture de [env.ts](../frontend/e2e/fixtures/env.ts) incluye Owner, Clinic, Store y ServiceProvider, pero no Ally ni Municipality. El 2026-10-02 la suite E2E autenticada se ejecutó y pasó solo para esos cuatro menús; verificar un `href` y analizar Axe en el nav no prueba que una persona complete la tarea descrita. No se realizaron sesiones de usuarios ni pruebas con lector de pantalla real. Mantener ambos resultados `NO_VERIFICADO`.

## Evidencia actual (2026-10-02)

- `npm --prefix frontend run test:e2e -- --grep "mobile menu reaches"`: 4/4 pasaron en Chromium a 390×844 para Owner, Clinic, Store y ServiceProvider. El escenario comprueba teclado, `href` y Axe solo en la navegación móvil.
- Axe encontró contraste insuficiente en avatares de dos de los cuatro usuarios (3.26:1 y 4.12:1). Se oscurecieron las variantes de color con texto blanco y la ejecución posterior pasó 4/4.
- Las pruebas unitarias focalizadas de accesibilidad pasaron 7/7. No se ejecutaron la suite frontend completa ni todo Playwright en esta revalidación; no se consultó CI.
- Un menú sin violaciones Axe no certifica WCAG 2.2 AA: no se verificaron otras rutas/estados, lector de pantalla real, zoom/reflow ni experiencia con usuarios.

## Puertas pendientes

- Completar datos E2E de Ally, Municipality y Admin; verificar destino efectivo y tareas internas, no solo el `href` del menú.
- Ejecutar Axe y teclado en errores, confirmaciones y acciones principales de clínica, expediente, adopciones, reservas y capturas.
- Realizar sesiones moderadas con personas de los seis perfiles y lector de pantalla en al menos un móvil; registrar resultados anónimos según este protocolo.
- Mantener descubribilidad de tareas por rol y conformidad WCAG global como `NO_VERIFICADO` hasta completar esas puertas.
