# Plan Maestro de Mejoras UI — PawTrack CR

## 1. Propósito del documento
Este documento convierte el análisis previo de UI/UX en un plan de ejecución detallado para rediseñar la experiencia visual de PawTrack CR con una dirección moderna, creativa, profesional, usable y claramente diferenciada.

La intención no es solo “hacer que se vea más bonito”, sino construir una capa de producto más fuerte en cinco dimensiones:

1. Confianza.
2. Claridad operativa.
3. Velocidad de uso en momentos críticos.
4. Consistencia visual entre módulos.
5. Diferenciación real de marca.

---

## 2. Resumen ejecutivo

### Diagnóstico general
PawTrack CR ya tiene bastante valor funcional. El problema principal no es de producto, sino de presentación, cohesión y madurez de la capa visual.

Actualmente el frontend transmite una sensación de producto híbrido:

1. Algunas vistas tienen intención visual clara.
2. Otras se perciben básicas o prototípicas.
3. No existe todavía un sistema de diseño formal.
4. La identidad de marca aún no se siente plenamente construida.
5. La navegación y jerarquía de información todavía pueden ganar mucha claridad.

### Hallazgos concretos observados en el frontend actual
Durante la revisión del proyecto se detectaron patrones relevantes:

1. Uso intensivo de clases utilitarias en múltiples pantallas sin evidencia clara de una estrategia visual centralizada.
2. Mezcla de estilos utilitarios con estilos inline extensos, lo que fragmenta la mantenibilidad.
3. Diferencia de calidad visual importante entre módulos.
4. Copy mezclado entre español e inglés.
5. Layout autenticado todavía muy básico para una app con varios roles.
6. Accesibilidad parcialmente bien atendida, pero no sistematizada.

### Conclusión ejecutiva
La mejor decisión no es retocar pantallas sueltas. La mejor decisión es diseñar y ejecutar una capa UI integral por fases, comenzando por sistema visual, layouts y flujos de mayor impacto.

---

## 3. Objetivo de negocio del rediseño
El rediseño UI debe aportar resultados tangibles, no solo estéticos.

### Objetivos principales
1. Aumentar la percepción de confianza y profesionalismo del producto.
2. Reducir fricción en flujos críticos de rescate y reporte.
3. Hacer más evidente qué hacer en cada pantalla.
4. Crear una identidad visual memorable y propia.
5. Preparar una base escalable para futuras features.

### Objetivos secundarios
1. Mejorar consistencia entre roles: usuario, aliado, clínica, admin.
2. Reducir deuda técnica visual.
3. Establecer lineamientos de accesibilidad y responsive design.
4. Facilitar mantenimiento futuro del frontend.

---

## 4. Dirección recomendada

## Opción recomendada: Opción B — Rediseño Signature PawTrack

### Por qué esta opción es la mejor
La Opción B equilibra tres necesidades clave:

1. Calidad visual premium.
2. Diferenciación real de marca.
3. Escalabilidad de largo plazo.

No se limita a un retoque cosmético y tampoco sacrifica claridad por creatividad. Permite construir una identidad sólida alrededor de la misión del producto: ayudar a reunir mascotas perdidas con sus familias.

### Concepto creativo recomendado
**Editorial cartográfico de rescate cívico**.

La interfaz debe sentirse como una mezcla de:

1. Plataforma tecnológica confiable.
2. Herramienta de coordinación comunitaria.
3. Sistema operativo emocional para situaciones de pérdida y recuperación.

### Sensación que debe transmitir
1. Seguridad.
2. Rapidez.
3. Empatía.
4. Inteligencia operativa.
5. Orden dentro del caos.

### Qué debe recordar el usuario de la experiencia
1. “Se ve serio y confiable.”
2. “Entiendo de inmediato qué debo hacer.”
3. “Se siente diferente a una app genérica.”
4. “Me ayuda cuando estoy en una situación emocionalmente intensa.”

---

## 5. Problemas raíz detectados

## 5.1 Sistema visual fragmentado
Hoy la UI no parece salir de un mismo sistema. Hay patrones repetidos, pero todavía no existe una gramática visual formal.

### Impacto
1. Inconsistencia en spacing, jerarquías y componentes.
2. Dificultad para escalar nuevas pantallas.
3. Aspecto general menos refinado.

## 5.2 Estilos distribuidos sin gobernanza clara
La mezcla de estilos inline, utilitarios y archivos puntuales genera complejidad.

### Impacto
1. Cambios globales difíciles.
2. Themable UI más costoso.
3. Menor legibilidad del frontend.

## 5.3 UX heterogénea entre módulos
No todas las áreas del producto tienen el mismo nivel de diseño.

### Impacto
1. El producto se siente irregular.
2. Baja percepción de madurez.
3. Mayor esfuerzo cognitivo al cambiar de módulo.

## 5.4 Navegación todavía poco estratégica
La app ya tiene múltiples casos de uso, pero la navegación aún no comunica bien la arquitectura del producto.

### Impacto
1. Menor orientación contextual.
2. Coste mayor de descubrimiento.
3. Jerarquía de acciones débil.

## 5.5 Copy y microcopy inconsistentes
El lenguaje visible todavía no está gobernado por una voz de producto definida.

### Impacto
1. Menor claridad.
2. Menor percepción premium.
3. Menor confianza en momentos críticos.

---

## 6. Principios rectores del nuevo UI

Todo el rediseño debe respetar estos principios.

### 6.1 Claridad antes que decoración
Cada pantalla debe responder rápidamente:

1. Dónde estoy.
2. Qué está pasando.
3. Qué acción es la principal.
4. Qué ocurre después.

### 6.2 Emoción controlada
PawTrack trabaja con una situación sensible: pérdida, ansiedad, búsqueda y recuperación. El diseño puede ser expresivo, pero no caótico.

### 6.3 Profesional pero humano
No debe verse burocrático ni frío. Debe proyectar seriedad con empatía.

### 6.4 Mobile-first real
Muchos flujos relevantes ocurren fuera de escritorio: rescate, reporte, chat, coordinación. La experiencia debe optimizarse primero para móvil y luego expandirse.

### 6.5 Sistema antes que pantalla
Antes de rediseñar veinte vistas, se debe construir el sistema que hará coherente a todas.

### 6.6 Accesibilidad como estándar
No debe dejarse al final. Debe integrarse desde los componentes base.

---

## 7. Alcance del proyecto UI

## 7.1 Alcance incluido
1. Sistema de diseño base.
2. Tokens visuales.
3. Layouts globales.
4. Componentes reutilizables.
5. Rediseño de pantallas clave.
6. Revisión de copy y tono.
7. Responsive design.
8. Motion design básico y útil.
9. Revisión de accesibilidad visual y de interacción.

## 7.2 Alcance no incluido en primera fase
1. Reescritura total del frontend.
2. Replanteamiento completo del dominio funcional.
3. Refactor profundo de estado global sin necesidad real.
4. Ilustración avanzada personalizada si no aporta a conversión o claridad.
5. Dark mode si compromete tiempo del rediseño principal.

---

## 8. Arquitectura visual propuesta

## 8.1 Sistema de diseño
Debe construirse una biblioteca interna mínima con reglas claras.

### Tokens a definir
1. Paleta principal.
2. Paleta de urgencia.
3. Paleta de éxito.
4. Grises neutrales.
5. Escala tipográfica.
6. Escala de spacing.
7. Radios.
8. Elevaciones.
9. Motion durations.
10. Breakpoints.

### Componentes base obligatorios
1. Button.
2. IconButton.
3. Input.
4. Textarea.
5. Select.
6. Toggle.
7. Badge.
8. Alert.
9. EmptyState.
10. Card.
11. TopBar.
12. SectionHeader.
13. Tabs.
14. Drawer o modal base.
15. Skeleton.
16. Toast o mensaje inline estándar.

### Componentes de producto específicos
1. PetCard.
2. LostStatusBanner.
3. ReportActionCard.
4. SearchProgressPanel.
5. SightingCard.
6. NotificationItemCard.
7. SafeContactCard.
8. EmergencyActionBar.
9. RoleStatusHeader.
10. KPIStatCard.

## 8.2 Estructura de layouts
Se recomienda formalizar layouts por contexto.

### Layouts propuestos
1. PublicLayout.
2. AuthLayout.
3. AppLayout para usuarios autenticados.
4. RoleWorkspaceLayout para Ally, Clinic y Admin.
5. FullscreenActionLayout para mapa, chat, coordinación y emergencias.

### Elementos que debe resolver cada layout
1. Jerarquía de navegación.
2. Espaciado base.
3. Máximo ancho de contenido.
4. Comportamiento responsive.
5. Superficies y fondos.
6. Ubicación de acciones globales.

---

## 9. Propuesta de identidad visual

## 9.1 Dirección estética
La interfaz no debe parecer una plantilla SaaS genérica. Debe tener identidad.

### Lenguaje visual sugerido
1. Base clara y luminosa.
2. Superficies suaves con profundidad sutil.
3. Colores de urgencia bien jerarquizados, no saturación constante.
4. Elementos cartográficos y patrones topográficos discretos.
5. Tipografía editorial con carácter.

## 9.2 Paleta conceptual recomendada

### Colores de marca sugeridos
1. Terracota cálido o naranja quemado como primario emocional y recordable.
2. Verde rescate o verde comunitario como apoyo positivo.
3. Azul profundo o tinta para confianza institucional.
4. Arena, marfil y piedra para fondos cálidos neutros.
5. Rojo alerta solo para estado crítico real.

### Regla de uso del color
1. El color debe comunicar prioridad, no decorar sin criterio.
2. No saturar toda la app con el mismo primario.
3. Reservar colores intensos para acciones y estados relevantes.

## 9.3 Tipografía recomendada

### Estrategia tipográfica
1. Una tipografía display con personalidad para titulares y momentos de marca.
2. Una tipografía de lectura limpia para cuerpo, formularios y data.
3. Números con buen peso para KPIs y stats.

### Criterios de selección
1. Legibilidad alta en móvil.
2. Carácter distintivo.
3. Buen soporte para español.
4. Capacidad de convivir con datos, mapas y estados de urgencia.

## 9.4 Motion design
La app debe moverse con intención, no con exceso.

### Animaciones recomendadas
1. Entradas suaves por bloques en pantallas clave.
2. Skeletons elegantes y consistentes.
3. Hover y focus visibles, sobrios y rápidos.
4. Microtransiciones en cards, filtros y toggles.
5. Motion especial para estados de rescate, envío o éxito.

### Restricciones
1. Respetar prefers-reduced-motion.
2. Evitar transición total indiscriminada.
3. Usar transform y opacity cuando sea posible.

---

## 10. Priorización de pantallas

## 10.1 Pantallas de prioridad crítica
Estas son las primeras que más retorno visual y funcional pueden dar.

1. Login.
2. Register.
3. Dashboard.
4. Create/Edit Pet.
5. Pet Detail.
6. Public Pet Profile.
7. Report Lost.
8. Lost Report Confirmation.
9. Report Sighting.
10. Chat.

## 10.2 Pantallas de prioridad alta
1. Notifications.
2. Visual Match.
3. Public Map.
4. Search Coordination.
5. Case Room.
6. Ally Panel.

## 10.3 Pantallas de prioridad media
1. Clinic Register.
2. Clinic Dashboard.
3. Admin Panel.
4. Recovery Stats.

---

## 11. Plan de ejecución detallado

La ejecución recomendada se divide en 7 fases. El orden está pensado para maximizar coherencia y minimizar retrabajo.

## Fase 0 — Descubrimiento y definición estratégica

### Objetivo
Cerrar decisiones base de marca, arquitectura visual y alcance real.

### Duración estimada
3 a 5 días.

### Entregables
1. Moodboard visual.
2. Dirección creativa final aprobada.
3. Lista priorizada de pantallas.
4. Criterios de éxito del rediseño.
5. Inventario de inconsistencias actuales.

### Actividades
1. Consolidar una única visión visual para la app.
2. Definir si el tono será más editorial, más operativo o más híbrido.
3. Mapear todos los flujos y detectar cuáles son críticos.
4. Revisar roles y qué información necesita cada uno.
5. Acordar si el rediseño será incremental o por lotes.

### Preguntas que esta fase debe cerrar
1. ¿Qué imagen debe proyectar PawTrack primero: rescate comunitario o plataforma institucional?
2. ¿Cuál es la emoción dominante deseada: calma, urgencia controlada, acompañamiento, precisión?
3. ¿Qué módulos deben rediseñarse antes para obtener más impacto?

### Criterios de salida
1. Dirección estética aprobada.
2. Priorización de trabajo cerrada.
3. No existen dudas relevantes sobre el enfoque del rediseño.

---

## Fase 1 — Sistema de diseño y fundamentos

### Objetivo
Construir la base visual y técnica para que el resto del rediseño sea consistente.

### Duración estimada
1 a 2 semanas.

### Entregables
1. Tokens de color, spacing, radius, shadows y typography.
2. Guía de estilos base.
3. Biblioteca mínima de componentes UI.
4. Reglas de uso de iconografía.
5. Reglas de copywriting UI.

### Actividades
1. Definir variables CSS o estrategia equivalente para tokens.
2. Resolver la estrategia oficial de estilos.
3. Crear componentes base reutilizables.
4. Establecer estados estándar: hover, active, focus, disabled, loading, error, success.
5. Formalizar tamaños y variantes de botones, campos y tarjetas.

### Decisiones técnicas importantes
1. Unificar el approach de estilos para reducir mezcla inconsistente.
2. Preferir una base escalable y predecible antes de continuar con más pantallas.
3. Dejar documentado qué patrones quedan prohibidos o desaconsejados.

### Criterios de aceptación
1. Ya no se crean nuevos botones o inputs ad hoc.
2. Los componentes base resuelven la mayoría de casos comunes.
3. Existe una fuente única de verdad para color, tipografía y spacing.

---

## Fase 2 — Layouts globales y navegación

### Objetivo
Dar estructura clara a la app y ordenar la experiencia entre contextos.

### Duración estimada
4 a 7 días.

### Entregables
1. App shell autenticado rediseñado.
2. Auth layout moderno y coherente.
3. Public layout mejorado.
4. Navegación por rol definida.
5. Reglas de encabezados y acciones por pantalla.

### Actividades
1. Rediseñar el header autenticado.
2. Definir navegación principal y secundaria.
3. Añadir patrones de breadcrumb o retorno cuando corresponda.
4. Estandarizar ancho de contenido y gutters.
5. Resolver comportamiento móvil de navegación.

### Resultado esperado
El usuario debe entender mejor:

1. Dónde está.
2. Qué puede hacer.
3. Cuál es la acción principal.
4. Cómo volver o avanzar.

### Criterios de aceptación
1. Todas las páginas principales heredan una estructura coherente.
2. El layout autenticado deja de verse provisional.
3. La jerarquía visual entre contenido, acciones y utilidades es consistente.

---

## Fase 3 — Flujos críticos de usuario final

### Objetivo
Rediseñar las experiencias más importantes del producto para el usuario propietario y para quien encuentra una mascota.

### Duración estimada
2 a 3 semanas.

### Flujos incluidos
1. Auth.
2. Dashboard.
3. Crear mascota.
4. Perfil de mascota.
5. Perfil público.
6. Reportar pérdida.
7. Confirmación de pérdida.
8. Reportar avistamiento.

### Entregables
1. Nuevas versiones visuales de las pantallas priorizadas.
2. Estados vacíos y errores bien resueltos.
3. Copy unificado.
4. Mejoras de responsive.

### Actividades por subflujo

#### Auth
1. Crear una experiencia de acceso con mejor narrativa visual.
2. Mejorar campos, mensajes de error y estados de envío.
3. Reforzar confianza desde el primer contacto.

#### Dashboard
1. Mostrar mascotas, acciones rápidas y alertas con más jerarquía.
2. Mejorar vacío inicial para onboarding.
3. Reforzar CTA principales.

#### Pet Detail
1. Reordenar acciones para evitar saturación.
2. Jerarquizar estado de mascota, QR, escaneos y acciones críticas.
3. Integrar mejor elementos de pérdida y recuperación.

#### Public Pet Profile
1. Hacer la experiencia más clara para alguien que encontró una mascota.
2. Priorizar “reportar avistamiento”, “contactar” y “compartir”.
3. Reducir ruido visual.

#### Report Lost y Confirmation
1. Hacer el flujo más guiado y menos administrativo.
2. Convertir la confirmación en centro real de activación de búsqueda.
3. Destacar próximos pasos con urgencia controlada.

#### Report Sighting
1. Minimizar fricción en carga de foto y ubicación.
2. Reforzar sensación de ayuda concreta.
3. Hacer muy claro qué información es opcional y cuál es crítica.

### Criterios de aceptación
1. Los flujos críticos se ven claramente más maduros.
2. Hay coherencia total entre pantallas.
3. La acción principal siempre es evidente.
4. La app se siente más amigable en momentos de estrés.

---

## Fase 4 — Experiencias operativas avanzadas

### Objetivo
Rediseñar módulos de coordinación, comunicación y seguimiento para que transmitan inteligencia operativa.

### Duración estimada
1.5 a 2 semanas.

### Módulos incluidos
1. Chat.
2. Notifications.
3. Case Room.
4. Search Coordination.
5. Visual Match.
6. Public Map.

### Entregables
1. Interfaces más orientadas a acción y contexto.
2. Estados operativos más claros.
3. Navegación contextual mejorada.
4. Mejor densidad de información sin perder legibilidad.

### Actividades por módulo

#### Chat
1. Reforzar la seguridad y anonimato visualmente.
2. Mejorar sidebar de conversaciones y estado vacío.
3. Optimizar versión móvil.

#### Notifications
1. Reorganizar jerarquía entre alertas críticas y menores.
2. Resolver mejor empty states, filtros y batch actions.
3. Mejorar sheet de auto-resolución.

#### Case Room
1. Convertirlo en un verdadero “centro de comando”.
2. Reducir ruido visual y estilos inline dispersos.
3. Mejorar tabs, indicadores y bloques de prioridad.

#### Search Coordination
1. Reforzar lectura del mapa y estados de zonas.
2. Mejorar barra de estado, acciones de activación y summary visual.
3. Hacer el flujo más usable en móvil.

#### Visual Match y Public Map
1. Integrar mejor IA y mapas dentro de una misma narrativa.
2. Hacer más clara la leyenda y las acciones disponibles.
3. Reducir sensación de “pantalla técnica” y volverla más comprensible.

### Criterios de aceptación
1. Los módulos avanzados ya no se ven como piezas aisladas.
2. Las acciones críticas se entienden en segundos.
3. El producto gana un tono operativo más serio y más premium.

---

## Fase 5 — Módulos por rol: Ally, Clinic, Admin

### Objetivo
Dar identidad y estructura a los espacios de trabajo especializados.

### Duración estimada
1 a 1.5 semanas.

### Entregables
1. Workspace visual por rol.
2. Tableros más claros y profesionales.
3. Componentes de data display mejor resueltos.

### Actividades
1. Crear patrones de panel operativo.
2. Estandarizar formularios de verificación, listas y cards administrativas.
3. Mejorar tablas, alertas y bloques informativos.

### Criterios de aceptación
1. Cada rol se siente parte del mismo producto.
2. Cada rol también tiene personalidad contextual propia.
3. La interfaz transmite capacidad operativa y control.

---

## Fase 6 — Pulido, accesibilidad y calidad final

### Objetivo
Cerrar el rediseño con consistencia, calidad y validación real.

### Duración estimada
4 a 7 días.

### Entregables
1. Auditoría de accesibilidad.
2. Revisión responsive final.
3. Ajustes de motion y performance visual.
4. QA visual completo.
5. Checklist final de release UI.

### Actividades
1. Revisar contraste, foco visible, labels y jerarquías.
2. Revisar layouts en móvil, tablet y desktop.
3. Revisar errores visuales, truncados y casos extremos.
4. Unificar mensajes de error, loading y empty states.
5. Detectar componentes fuera del sistema.

### Criterios de aceptación
1. No quedan pantallas críticas fuera del lenguaje visual definido.
2. La experiencia es coherente de punta a punta.
3. El producto está listo para mostrarse con confianza.

---

## 12. Backlog detallado por área

## 12.1 Auth

### Problemas actuales
1. Pantallas demasiado básicas visualmente.
2. Poca narrativa de marca.
3. Jerarquía visual débil.

### Mejoras propuestas
1. Hero o bloque superior con identidad de producto.
2. Formularios más refinados.
3. Mensajes de error y éxito más claros.
4. Mejor uso de spacing, tipografía y color.
5. Navegación SPA consistente.

### Resultado esperado
Ingreso más confiable, moderno y profesional.

## 12.2 Dashboard

### Problemas actuales
1. Mezcla de idiomas.
2. Se siente funcional pero genérico.
3. Los atajos rápidos pueden jerarquizarse mejor.

### Mejoras propuestas
1. Header más expresivo y contextual.
2. Reordenar CTA primarias y secundarias.
3. Mejor tratamiento de tarjetas de mascota.
4. Empty state de onboarding más potente.
5. Integrar logros o estado del usuario sin ruido.

## 12.3 Pet Detail

### Problemas actuales
1. Muchas acciones conviven al mismo nivel.
2. La pantalla puede sentirse densa.
3. Faltan agrupaciones visuales más estratégicas.

### Mejoras propuestas
1. Crear secciones más claras: estado, identidad, acciones, historial, avistamientos.
2. Separar mejor acciones críticas de acciones de mantenimiento.
3. Reforzar visualmente estados “perdido”, “activo”, “reunido”.

## 12.4 Public Pet Profile

### Problemas actuales
1. Debe ser extremadamente clara para una persona que llega por QR.
2. Tiene buen potencial, pero puede ser más guiada.

### Mejoras propuestas
1. CTA principal mucho más clara.
2. Bloque de contacto más confiable.
3. Reforzar instrucción contextual: qué hacer si encontraste la mascota.
4. Mejor manejo de chat, llamada y compartir.

## 12.5 Report Lost

### Problemas actuales
1. Debe reducir ansiedad y aumentar foco.
2. Puede sentirse como formulario y no como flujo guiado.

### Mejoras propuestas
1. Estructura paso a paso.
2. Mejor copy de ayuda.
3. Inputs con mejores ayudas visuales.
4. Separación clara entre datos obligatorios y opcionales.

## 12.6 Lost Report Confirmation

### Problemas actuales
1. Es una pantalla crítica que debe activar la búsqueda.
2. Necesita sentirse más poderosa.

### Mejoras propuestas
1. Convertirla en hub de acciones inmediatas.
2. Reforzar compartir, flyer, mapa y checklist.
3. Jerarquizar próximos pasos con urgencia.

## 12.7 Chat

### Problemas actuales
1. Funcional, pero todavía muy sobrio y genérico.
2. El valor del “chat seguro” puede expresarse mejor.

### Mejoras propuestas
1. Identidad visual propia del módulo.
2. Mejor sidebar, header y estados vacíos.
3. Mejor versión móvil con prioridades claras.

## 12.8 Case Room

### Problemas actuales
1. Gran potencial, pero visualmente heterogéneo.
2. Mucho estilo inline y bloques manuales.

### Mejoras propuestas
1. Convertirlo en una interfaz táctica y clara.
2. Estandarizar paneles, tabs y KPIs.
3. Integrar mapas, alertas y acciones con más cohesión.

---

## 13. Plan de copy y tono de marca

## 13.1 Problema actual
La mezcla de español e inglés debilita identidad y pulido del producto.

## 13.2 Regla general
Toda la experiencia orientada a usuario final debe quedar en español consistente, idealmente español claro para Costa Rica, evitando regionalismos excesivos cuando resten claridad.

## 13.3 Principios de microcopy
1. Directo.
2. Claro.
3. Empático.
4. Orientado a acción.
5. Sin ambigüedad.

## 13.4 Ejemplos de tono deseado
1. “Reportar avistamiento” en vez de frases ambiguas.
2. “Tu mascota fue marcada como perdida” en vez de mensajes técnicos.
3. “Siguiente paso recomendado” para guiar decisiones.
4. “Tu información se mantiene protegida” para reforzar confianza.

## 13.5 Trabajo a ejecutar
1. Crear glosario UI oficial.
2. Estandarizar nombres de acciones.
3. Revisar todos los textos visibles en pantallas prioritarias.
4. Alinear mensajes de error y estados vacíos.

---

## 14. Accesibilidad y usabilidad

## 14.1 Estándares mínimos
1. Focus visible consistente.
2. Labels reales en formularios.
3. Jerarquía de headings correcta.
4. Estados de error vinculados a campos.
5. Contraste suficiente.
6. Navegación por teclado funcional.
7. Respeto a reduced motion.

## 14.2 Mejoras prácticas a integrar
1. Evitar enlaces duros cuando debe usarse navegación SPA.
2. Estandarizar aria-label en acciones iconográficas.
3. Unificar patrones de loading y alertas.
4. Revisar scroll, tamaños táctiles y jerarquía en móvil.

## 14.3 Meta de experiencia
La app debe ser usable incluso cuando el usuario está estresado, cansado o con poco tiempo.

---

## 15. Performance visual y calidad frontend

## 15.1 Objetivo
Mejorar UI sin degradar rendimiento.

## 15.2 Lineamientos
1. Cargar solo lo necesario por pantalla.
2. Evitar complejidad visual gratuita.
3. Mantener imágenes optimizadas.
4. Usar skeletons y feedback rápido.
5. No abusar de sombras, blur o efectos costosos.

## 15.3 Riesgos a vigilar
1. Sobrediseñar mapas o capas pesadas.
2. Agregar demasiadas animaciones.
3. Generar demasiada variación visual sin sistema.

---

## 16. Riesgos del proyecto y mitigaciones

## Riesgo 1 — Rediseñar sin sistema
### Impacto
Re-trabajo y UI inconsistente.

### Mitigación
Construir tokens y componentes primero.

## Riesgo 2 — Intentar rediseñar toda la app de golpe
### Impacto
Bloqueo de desarrollo y fatiga del equipo.

### Mitigación
Ejecutar por fases y lotes priorizados.

## Riesgo 3 — Exceso creativo sin claridad operativa
### Impacto
Confusión en flujos críticos.

### Mitigación
Validar cada pantalla con principio de acción principal visible.

## Riesgo 4 — Inconsistencia de copy
### Impacto
Sensación no premium.

### Mitigación
Crear glosario UI y revisar textos antes de cerrar cada fase.

## Riesgo 5 — Deuda técnica visual persistente
### Impacto
Dificultad futura para mantener el frontend.

### Mitigación
Eliminar gradualmente patrones inline y variantes improvisadas.

---

## 17. Métricas de éxito

El rediseño debe medirse con señales de producto y señales de calidad.

## 17.1 Métricas de percepción
1. Mayor confianza percibida en pruebas internas.
2. Mejor evaluación visual del producto por stakeholders.
3. Mayor claridad declarada sobre “qué hacer” en pantallas clave.

## 17.2 Métricas de uso
1. Menor abandono en auth.
2. Menor abandono en creación de mascota.
3. Mayor uso de reportar avistamiento.
4. Mayor uso de compartir perfil/flyer.
5. Mejor tiempo hasta primera acción en flujos críticos.

## 17.3 Métricas técnicas
1. Menos variantes visuales no documentadas.
2. Más componentes reutilizados.
3. Menor dispersión de estilos inline.
4. Menos inconsistencias de idioma.

---

## 18. Orden recomendado de implementación real

Si el equipo quiere máxima eficiencia, este debería ser el orden práctico:

1. Definir dirección visual final.
2. Crear tokens y componentes base.
3. Rediseñar layouts globales.
4. Rediseñar auth.
5. Rediseñar dashboard.
6. Rediseñar create/edit pet.
7. Rediseñar pet detail.
8. Rediseñar public pet profile.
9. Rediseñar report lost y confirmation.
10. Rediseñar report sighting.
11. Rediseñar notifications y chat.
12. Rediseñar case room, maps y coordinación.
13. Rediseñar roles operativos.
14. Hacer pulido y QA final.

---

## 19. Estimación global

### Escenario conservador
4 a 6 semanas para un rediseño serio e incremental de alto impacto.

### Escenario más agresivo
3 a 4 semanas si se reduce alcance y se concentra en sistema + pantallas críticas.

### Escenario ideal
6 a 8 semanas si se quiere cerrar también módulos por rol, mapas y pulido completo.

---

## 20. Plan técnico frontend para web y móvil con los mismos features

### Decisión funcional
La versión web y la versión móvil deben tener exactamente las mismas capacidades funcionales.

Esto significa que:

1. No habrá features exclusivas de móvil.
2. No habrá features exclusivas de escritorio.
3. No habrá rutas de negocio diferentes por plataforma.
4. No habrá privilegios funcionales distintos entre web y móvil.

### Qué sí puede cambiar entre web y móvil
Aunque los features sean los mismos, la UX no tiene por qué ser idéntica.

Lo correcto es mantener paridad funcional y variar:

1. Layout.
2. Navegación.
3. Jerarquía visual.
4. Densidad de información.
5. Orden de bloques.
6. Interacciones secundarias.

### Regla de oro
**Misma funcionalidad, distinta presentación.**

---

## 20.1 Qué no cambia entre plataformas

Estos elementos deben ser comunes en web y móvil:

1. Las rutas funcionales del producto.
2. Las acciones disponibles para el usuario.
3. Las validaciones y reglas de negocio.
4. El copy base.
5. La identidad visual.
6. El sistema de diseño.
7. Los estados del dominio.
8. Los permisos por rol.

### Ejemplo práctico
Si existe “Reportar avistamiento”, debe existir en ambas plataformas.

Lo que puede variar es:

1. En móvil, se presenta como flujo de una columna con CTA fijo inferior.
2. En web, se presenta como layout de dos columnas con resumen lateral.

El feature es el mismo. La resolución UX es distinta.

---

## 20.2 Qué sí cambia entre plataformas

## Navegación
### Móvil
1. Navegación más corta.
2. Más acciones visibles por contexto.
3. Posible barra inferior o acciones fijas.
4. Menos profundidad visual por pantalla.

### Web
1. Mayor visibilidad global.
2. Header más amplio.
3. Posibles sidebars o paneles auxiliares.
4. Más espacio para multitarea visual.

## Densidad de información
### Móvil
1. Una prioridad fuerte por pantalla.
2. Menos bloques visibles al mismo tiempo.
3. Más secuencia vertical.

### Web
1. Más datos simultáneos.
2. Más paneles secundarios.
3. Mejor uso de comparaciones y contexto adicional.

## Interacción
### Móvil
1. Controles más grandes.
2. Zonas táctiles más generosas.
3. Más énfasis en scroll y CTA persistentes.

### Web
1. Más atajos visuales.
2. Mejor aprovechamiento de hover.
3. Mayor visibilidad de información complementaria.

---

## 20.3 Enfoque arquitectónico recomendado

Para mantener el mismo set de features, la mejor arquitectura es:

1. Un solo frontend.
2. Una sola base de rutas.
3. Un solo sistema de componentes.
4. Variantes responsive o adaptativas por componente y layout.

### Modelo recomendado
**Shared Feature Layer + Responsive Experience Layer**

#### Shared Feature Layer
Contiene:

1. APIs.
2. Hooks.
3. Estado.
4. Reglas de negocio de presentación.
5. Modelos de datos.
6. Formularios y validaciones.

#### Responsive Experience Layer
Contiene:

1. Layouts por breakpoint.
2. Variantes visuales de componentes.
3. Reordenamiento de bloques.
4. Patrones de navegación según contexto.

### Beneficio principal
Se evita mantener dos apps distintas y al mismo tiempo se consigue una UX mucho mejor adaptada a cada dispositivo.

---

## 20.4 Estrategia de implementación técnica

## Opción recomendada
Implementar la interfaz bajo un esquema de **componentes con variantes responsivas**, no con duplicación de páginas completas.

### Patrón deseado
1. Compartir contenedor lógico.
2. Compartir hooks y datos.
3. Cambiar composición visual según breakpoint.

### Patrón no deseado
1. `DashboardMobile.tsx` y `DashboardDesktop.tsx` completamente separados para cada pantalla.
2. Dos árboles enteros de componentes para hacer lo mismo.
3. Dos sets de copy, reglas o formularios para el mismo feature.

### Cuándo sí puede justificarse una bifurcación parcial
Solo en pantallas muy especiales como:

1. Mapas a pantalla completa.
2. Chat.
3. Coordinación de búsqueda.
4. Paneles operativos con altísima densidad.

Incluso allí, debe compartirse lógica y mantenerse el mismo feature set.

---

## 20.5 Propuesta de estructura técnica frontend

### Estructura conceptual sugerida
1. `features/` para lógica de dominio.
2. `shared/ui/` para componentes base del sistema de diseño.
3. `shared/layouts/` para shells y estructuras reutilizables.
4. `shared/responsive/` para hooks y helpers de breakpoints.
5. `shared/patterns/` para patrones de pantalla complejos.

### Ejemplo de organización por pantalla
Cada feature puede tener:

1. Un contenedor principal de negocio.
2. Un layout móvil.
3. Un layout desktop.
4. Bloques compartidos.

### Patrón recomendado
`FeaturePage.tsx`

Responsabilidades:

1. Obtener datos.
2. Resolver permisos.
3. Determinar estado.
4. Elegir composición responsive.

`FeaturePageMobile.tsx`

Responsabilidades:

1. Orden móvil.
2. CTA móvil.
3. Densidad móvil.

`FeaturePageDesktop.tsx`

Responsabilidades:

1. Orden desktop.
2. Paneles secundarios.
3. Mayor paralelismo visual.

`FeaturePageSections.tsx`

Responsabilidades:

1. Compartir bloques de UI reutilizables.
2. Evitar duplicación entre mobile y desktop.

### Nota importante
No se recomienda hacer esto para todas las pantallas desde el día 1. Solo donde realmente cambia la composición.

En muchas vistas bastará con:

1. CSS responsive.
2. Reordenamiento por grid/flex.
3. Ajuste de densidad y spacing.

---

## 20.6 Reglas de paridad funcional

Para evitar que web y móvil se desalineen con el tiempo, deben definirse reglas explícitas.

### Regla 1
Toda historia de usuario debe validarse en móvil y en web.

### Regla 2
Ningún feature se considera terminado si funciona solo en una plataforma.

### Regla 3
Los mismos permisos deben producir las mismas acciones disponibles.

### Regla 4
Los estados vacíos, errores y loading deben existir en ambas experiencias.

### Regla 5
El backlog debe describir diferencias de UX, no diferencias funcionales, salvo aprobación explícita.

---

## 20.7 Breakpoints y comportamiento recomendado

### Breakpoints sugeridos
1. Móvil pequeño: hasta 479 px.
2. Móvil estándar: 480 a 767 px.
3. Tablet: 768 a 1023 px.
4. Desktop: 1024 px en adelante.
5. Desktop ancho: 1440 px en adelante.

### Política de diseño por breakpoint

#### Móvil pequeño
1. Una sola columna.
2. CTA principal visible con facilidad.
3. Controles grandes.
4. Espaciado compacto pero respirable.

#### Móvil estándar
1. Flujo completo y cómodo.
2. Jerarquía clara entre acción primaria y secundaria.
3. Más énfasis en navegación contextual.

#### Tablet
1. Debe verse como experiencia intermedia, no como desktop roto.
2. Se pueden introducir paneles secundarios ligeros.
3. Mejor densidad para formularios y mapas.

#### Desktop
1. Más información visible simultáneamente.
2. Mejor uso de paneles laterales.
3. Tableros, resúmenes y acciones complementarias visibles.

---

## 20.8 Traducción a backlog técnico

Cada historia de frontend debe escribirse con esta estructura:

### Ejemplo de historia técnica
**Feature:** Reportar avistamiento

**Paridad funcional requerida:**
1. Subir foto.
2. Elegir ubicación.
3. Agregar nota.
4. Enviar reporte.

**Experiencia móvil:**
1. Flujo vertical.
2. CTA fijo inferior.
3. Inputs apilados.
4. Preview de foto prioritaria.

**Experiencia web:**
1. Dos columnas.
2. Formulario principal + panel contextual.
3. Mapa más visible.
4. Resumen antes de enviar.

### Beneficio de este formato
El equipo deja de pensar en “dos features” y empieza a pensar en “un feature con dos resoluciones UX”.

---

## 20.9 Prioridad de implementación responsive por pantalla

## Lote 1 — Flujos críticos con paridad total
1. Login.
2. Register.
3. Dashboard.
4. Create/Edit Pet.
5. Pet Detail.
6. Public Pet Profile.
7. Report Lost.
8. Report Sighting.

### Objetivo del lote 1
Garantizar que el core del producto sea sólido en ambas plataformas.

## Lote 2 — Flujos de seguimiento y comunicación
1. Notifications.
2. Chat.
3. Lost Report Confirmation.

### Objetivo del lote 2
Hacer consistente la continuidad operativa entre dispositivos.

## Lote 3 — Flujos complejos de operación
1. Public Map.
2. Visual Match.
3. Search Coordination.
4. Case Room.

### Objetivo del lote 3
Resolver los módulos más sensibles a diferencias de layout.

## Lote 4 — Workspaces por rol
1. Ally Panel.
2. Clinic Dashboard.
3. Admin Panel.

### Objetivo del lote 4
Mantener paridad funcional sin perder eficiencia operativa en desktop ni usabilidad en móvil.

---

## 20.10 Cómo decidir cuándo usar responsive y cuándo usar composición dual

## Usar responsive simple cuando:
1. La pantalla es lineal.
2. El contenido es secuencial.
3. No hay multitarea visual importante.
4. El layout solo requiere reflujo y ajuste de spacing.

## Usar composición dual cuando:
1. El orden de bloques cambia de forma significativa.
2. Desktop necesita panel lateral persistente.
3. Móvil necesita CTA fija o estructura más dirigida.
4. El mismo feature tiene densidades muy distintas por contexto.

### Regla práctica
Primero intentar responsive simple. Si la UX se resiente, pasar a composición dual parcial.

---

## 20.11 Testing y QA para asegurar paridad

La paridad de features debe verificarse explícitamente.

### QA funcional
1. Cada feature debe probarse en viewport móvil y desktop.
2. Los mismos permisos deben mostrar las mismas acciones.
3. Los mismos errores deben resolverse igual en ambas experiencias.

### QA visual
1. Revisar overflow.
2. Revisar scroll accidental horizontal.
3. Revisar truncado de texto.
4. Revisar sticky headers, CTA fijas y drawers.
5. Revisar legibilidad en mapas y modales.

### QA de interacción
1. Tacto en móvil.
2. Teclado y focus en desktop.
3. Hover no obligatorio para entender la UI.
4. Acciones críticas accesibles sin precisión extrema.

---

## 20.12 Riesgos de tener mismo feature set en dos experiencias

## Riesgo 1
Forzar desktop dentro de móvil.

### Mitigación
No copiar layouts de escritorio en una sola columna sin rediseño de prioridad.

## Riesgo 2
Forzar móvil dentro de desktop.

### Mitigación
No dejar desktop como simple versión estirada sin aprovechar el espacio.

## Riesgo 3
Duplicar demasiados componentes.

### Mitigación
Compartir lógica y bloques; separar solo la composición cuando sea necesario.

## Riesgo 4
Desalineación futura entre plataformas.

### Mitigación
Introducir checklist de paridad por historia y por release.

---

## 20.13 Recomendación final de implementación

Para PawTrack CR, la estrategia más correcta es esta:

1. Un mismo producto.
2. Un mismo feature set.
3. Un mismo design system.
4. Una misma lógica de negocio.
5. Dos resoluciones UX: móvil y web.

### Traducción práctica
1. Móvil debe ser más guiado, táctil y enfocado.
2. Web debe ser más denso, visible y operativo.
3. Ambos deben permitir exactamente las mismas acciones.

### Decisión técnica recomendada
Construir un frontend responsive-adaptativo con layouts especializados por breakpoint, evitando dos apps o dos árboles completos salvo en módulos donde la composición realmente lo exija.

---

## 21. Decisión recomendada

## Si el objetivo es resultado visual fuerte y sostenible
Elegir **Opción B — Rediseño Signature PawTrack**.

## Si el objetivo es velocidad con mínimo riesgo
Elegir **Opción A — Rescate Rápido Premium**.

## Si el objetivo principal es eficiencia en calle y rescate inmediato
Elegir **Opción C — Mobile-First Command Center**.

---

## 22. Recomendación final de ejecución
La recomendación más sólida es esta:

1. Tomar Opción B como norte visual.
2. Ejecutarla con una táctica de implementación incremental similar a Opción A.
3. Incorporar principios mobile-first de Opción C en los flujos críticos.

En otras palabras:

**marca y diferenciación de B + velocidad de entrega de A + pragmatismo operativo de C**.

Ese enfoque híbrido es probablemente el mejor equilibrio para PawTrack CR.

---

## 23. Próximo paso sugerido
Una vez aprobada la dirección, el siguiente documento ideal sería:

1. Backlog UI por sprint.
2. Inventario de componentes a crear o refactorizar.
3. Lista exacta de pantallas a intervenir primero.
4. Checklist técnico para ejecutar el rediseño sin romper el frontend actual.

Si se desea, el siguiente paso puede ser convertir este plan en un roadmap de implementación técnico por sprint con tareas concretas de frontend para empezar a construirlo.