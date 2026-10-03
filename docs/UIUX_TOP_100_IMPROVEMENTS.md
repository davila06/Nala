# Top 100 mejoras UX/UI para NALA

**Corte:** 2026-09-28. Orden preliminar por impacto en tarea, negocio, urgencia y esfuerzo; la matriz de factores está en [UIUX_OPPORTUNITY_MATRIX.md](UIUX_OPPORTUNITY_MATRIX.md). `[C]` = problema confirmado por código/token/test; `[V]` = validar en navegador/usuarios antes de declarar defecto; `[P]` = propuesta de producto.

**Revalidación focal:** 2026-10-02. Las etiquetas `[C]`/`[V]`/`[P]` describen evidencia/clase del ítem, no su estado de resolución. El estado actualizado de hallazgos revisados está en [UIUX_BACKLOG.md](UIUX_BACKLOG.md). Axe ya está integrado en Playwright; 4/4 menús móviles (Owner, Clinic, Store, ServiceProvider) pasaron a 390×844. WCAG global, roles no cubiertos, lector de pantalla y sesiones con usuarios permanecen `NO_VERIFICADO`.

<!-- markdownlint-disable MD029 -->

## Seguridad, WCAG y tareas críticas (1-20)

1. `[C]` Sustituir pares de texto pequeños con contraste menor a 4.5:1; revisar sand-400/sand-500/brand-500 por superficie.
2. `[C]` Convertir el stepper de reporte perdido a lista de pasos con `aria-current="step"` y progreso anunciado.
3. `[C]` Validar campos requeridos al pulsar “Siguiente” antes de desmontar cada paso.
4. `[C]` En error del wizard, enfocar el primer campo inválido y asociar mensaje con `aria-describedby`.
5. `[C]` Añadir trapping/restauración de foco y cierre Escape al `OnboardingWizard`.
6. `[C]` Cambiar QRFlipCard a botón semántico; soportar Space y Enter con estado accesible.
7. `[C]` Hacer que PhotoUpload tenga label/ID real en el input file y estado anunciado.
8. `[V]` Ejecutar axe por una matriz de rutas/roles e integrar hallazgos A/AA en CI.
9. `[V]` Probar todos los diálogos con solo teclado, lector de pantalla y zoom 400%.
10. `[V]` Comprobar que targets pequeños cumplen WCAG 2.2 24×24 y definir 44×44 como objetivo táctil.
11. `[V]` Comprobar contraste de focus ring, sticky topbar y bottom nav bajo foco desplazado.
12. `[V]` Probar mapa con teclado y lector; ofrecer listado completo de los mismos eventos/acciones.
13. `[C]` Mostrar CTA de pérdida también en móvil, sin competir visualmente con una emergencia ya activa.
14. `[C]` Mantener una ruta de recuperación urgente alcanzable desde toda pantalla de propietario en mobile.
15. `[V]` Verificar que pérdida offline conserva foto/datos, informa cola local y recupera fallos/reintentos sin duplicar casos.
16. `[V]` Probar que los avisos de permisos de geolocalización ofrecen entrada manual y nunca bloquean el reporte.
17. `[C]` Distinguir “no disponible”, “sin permiso”, “sin conexión” y error del servidor en flujos de crisis.
18. `[V]` Asegurar que los botones de cierre, cámara, mapa, subir foto y QR tienen nombre accesible y foco visible.
19. `[V]` Probar navegación con teclado en desktop y mobile browser, incluyendo menús de rol y menús de usuario.
20. `[V]` Fijar una checklist de salida WCAG 2.2 AA con teclado, reflow, contraste, nombres, errores, foco y targets.

## Recuperación y QR/NFC (21-35)

21. `[C]` Reescribir el copy de onboarding de IA para no prometer reconocimiento universal ni resultado de búsqueda.
22. `[C]` Explicar en el perfil QR qué información es pública y qué acciones/contacto requieren cuenta.
23. `[V]` Probar escaneo QR con móvil real, cámara integrada, navegador in-app y lector de pantalla.
24. `[V]` Añadir QR visible sin gesto 3D como alternativa accesible, descargable e imprimible.
25. `[C]` Eliminar la dependencia de swipe para descubrir QR; explicar que se puede accionar y ofrecer botón explícito.
26. `[C]` Cambiar tutorial NFC para diferenciar lectura NFC del teléfono y escritura manual con app de terceros.
27. `[C]` Reemplazar placeholder NFC `/p/[id-de-tu-mascota]` por URL real copiable o instrucción inequívoca.
28. `[V]` Informar compatibilidad de etiquetas/chips, bloqueo de escritura, capacidad y prueba de lectura; validar por modelo.
29. `[C]` Declarar que NFC no se activa ni verifica desde PawTrack mientras ese flujo no exista.
30. `[V]` Mejorar página pública de mascota con jerarquía móvil: identificación, contacto seguro y reportar avistamiento.
31. `[V]` Probar que “Encontré mascota” funciona sin cuenta con el mínimo de datos y opción de anonimato comprensible.
32. `[V]` Evitar que el buscador visual parezca necesario para reportar avistamiento; resaltarlo como ayuda opcional.
33. `[V]` Explicar precisión/limitaciones del mapa y cuándo fue actualizado cada evento.
34. `[V]` Probar filtros de mapa/lista, estados vacíos y alternativas si geolocalización está denegada.
35. `[V]` Diseñar red de coordinación con privacidad por defecto y descripción visible de los datos compartidos.

## Onboarding, conversión y PLG (36-50)

36. `[C]` Adaptar onboarding por rol: tutor, veterinaria, refugio/aliado, tienda/proveedor y municipalidad.
37. `[V]` Medir activación real: perfil creado, foto/QR descargado, primer miembro, primera reserva o primer recordatorio.
38. `[V]` Usar checklists por rol, progreso retirable y reanudable; no bloquear dashboard con modal repetitivo.
39. `[C]` Corregir onboarding para enlazar el paso “Registrar mi primera mascota” a una promesa QR/IA realista.
40. `[V]` Probar “aha moment” dentro de la primera sesión con tarea ejecutada, no solo tutorial leído.
41. `[V]` Mostrar qué está incluido, plan requerido y motivo del bloqueo en el punto de acción.
42. `[V]` Separar límites del plan de seguridad/privacidad; no pagar para obtener protección básica de recuperación.
43. `[C]` Suprimir upsell/modal promocional durante reporte de pérdida, reporte de hallazgo o cierre de una emergencia.
44. `[V]` Atribuir upgrade a una necesidad concreta y explicar importe, periodo, renovación/cancelación antes de pago.
45. `[V]` Añadir opt-in explícito, atribución consentida y supresión de PII en eventos PLG.
46. `[V]` Probar canal de recuperación de usuarios que abandonan registro, con consentimientos y sin mensajes invasivos.
47. `[V]` A/B test de CTA solo tras instrumentación base, guardrails de confianza y segmento/rol definido.
48. `[V]` Medir conversiones y errores por paso de registros de clínica, tienda y proveedor.
49. `[V]` Comunicar pasos/tiempos de revisión y estado de solicitud a negocios, refugios y clínicas.
50. `[V]` Distinguir planes/términos técnicos de oferta comercial aprobada en todas las pantallas de precio.

## Salud, veterinarias y telemedicina (51-65)

51. `[V]` Separar hecho registrado, declaración del tutor, verificación clínica y recomendación preventiva visualmente y en lectores.
52. `[V]` Explicar qué puede editar el tutor y qué solo la clínica; mostrar autor/fecha fuente en cada registro.
53. `[V]` Asegurar búsqueda, filtros y paginación de historia conservan posición/criterios al volver.
54. `[V]` Hacer que carga de adjunto anuncie tamaño, formatos, progreso, fallo y recuperación.
55. `[V]` En recordatorios, indicar zona horaria, fuente del protocolo y acción concreta, sin sugerir diagnóstico.
56. `[C]` Mantener descarga autenticada de adjunto; no mostrar URL Blob ni enlazar directamente al storage.
57. `[V]` Revisar contraste y legibilidad de gráficos/tendencias; datos se deben entender sin depender del color.
58. `[V]` Diseñar estado sin historial con siguiente paso útil y textos no clínicamente engañosos.
59. `[V]` Probar flujo owner ↔ clínica para solicitar/autorizar/revocar grant, incluyendo acceso caducado y errores.
60. `[V]` En vistas de clínica, destacar sede activa, identidad de mascota y vigencia del permiso antes de mutaciones.
61. `[C]` No anunciar telemedicina audiovisual; no hay pantalla ni servicio de llamada implementado.
62. `[P]` Antes de diseñar telemedicina futura, especificar identidad profesional, consentimiento, cámara/micrófono, reconexión y fallback.
63. `[P]` Definir qué puede documentarse antes/después de una consulta remota y cómo se revoca consentimiento.
64. `[V]` Probar pantallas clínicas en tablet horizontal/vertical y con tareas repetidas de recepción/veterinario.
65. `[V]` Optimizar expediente clínico para consulta frecuente: acciones primarias persistentes, búsqueda y contexto de paciente siempre visibles.

## Refugios y municipalidades (66-75)

66. `[V]` Separar procesos de publicación de adopción del tablero de solicitudes con estados/next action claros.
67. `[V]` Guardar borradores y advertir antes de perder descripción/fotos de animal.
68. `[V]` Hacer filtros de adopción compatibles con URL, teclado, lector y pantallas de 320px.
69. `[V]` Ofrecer fallos de carga/reintento por foto y progreso de publicación múltiple.
70. `[V]` En captura municipal, ordenar campos por uso de campo/móvil y habilitar entrada manual fuera de cobertura.
71. `[V]` Optimizar lectura de tableros municipales para densidad e impresión/exportación sin perder contexto del filtro.
72. `[V]` Añadir bulk action preview, conteo y deshacer/confirmación antes de cambiar estados de múltiples animales.
73. `[V]` Diferenciar registro de captura, identidad verificada y reunificación confirmada con estados accesibles.
74. `[V]` Probar portales institucionales con usuarios reales y roles de operador, supervisor y administrador.
75. `[V]` No anunciar interoperabilidad gubernamental ni telemedicina municipal sin integración ejecutable.

## Marketplace y portales de negocio (76-86)

76. `[V]` Aclarar si un pedido/reserva es solicitud o compra confirmada; no insinuar inventario/pago garantizado.
77. `[V]` Mostrar costo total, moneda, impuestos, cancelación y método de pago antes de confirmar operación.
78. `[V]` Presentar disponibilidad con zona horaria local y confirmar cambios/cancelaciones de horario.
79. `[V]` Mejorar directorios con filtros transparentes, cobertura, verificación y fecha de datos.
80. `[V]` Asegurar que cards de proveedor/clínica/tienda no dependen solo de imagen, badge o color para decidir.
81. `[V]` Hacer onboarding de comercios progresivo, indicar requisitos por paso y guardar avance.
82. `[V]` En portales, separar acciones que cualquier miembro puede hacer de las reservadas a administración.
83. `[V]` En caja clínica, confirmar paciente/recibo/monto antes de registrar pago o devolución.
84. `[V]` Presentar tareas y alertas clínicas con prioridad explícita y límites de acceso por sede.
85. `[V]` Probar tablas en móvil con alternativa de tarjetas/columnas prioritarias, no scroll horizontal accidental.
86. `[V]` Añadir estados vacíos y ejemplo de siguiente paso a ventas, inventario, analítica y reservas.

## Responsive, sistema de diseño y operación (87-100)

87. `[V]` Ejecutar una matriz visual 320/360/390/768/1024/1440px en rutas públicas y de cada rol.
88. `[V]` Verificar bottom navigation contra safe areas/teclado virtual y que no tape botones finales.
89. `[V]` Verificar scroll horizontal en reportes, tablas, modales y páginas de detalles a 320 CSS px.
90. `[C]` Crear mapa de tokens de texto por contraste AA con ejemplos de combinación foreground/background.
91. `[V]` Normalizar radios, espaciado, elevation, tamaños de control y estados hover/focus/disabled.
92. `[C]` Unificar primitives Modal/Drawer y documentar cuándo usar cada una.
93. `[V]` Añadir storybook o catálogo navegable de patrones, variantes y estados de shared UI.
94. `[V]` Establecer un patrón único para `loading/error/empty/offline/permission-denied` con pruebas.
95. `[V]` Añadir traducción/formatting centralizados para CR, fechas, moneda y horas.
96. `[V]` Cargar tipografías de forma resiliente (preload/local fallback) y medir CLS/LCP en móvil lento.
97. `[V]` Verificar reduced-motion real en Framer Motion y animaciones CSS; no basta el override global.
98. `[V]` Añadir objetivos de rendimiento UX: LCP/INP/CLS para rutas más usadas, con móvil de gama media.
99. `[V]` Revisar productos de terceros/analytics/cookies con consentimiento y feedback comprensible.
100. `[V]` Ejecutar prueba moderada trimestral por perfil y actualizar el backlog con evidencia, tasa de éxito y severidad.

<!-- markdownlint-enable MD029 -->
