# Manual de Usuario — Rol: Aliado Verificado

**Versión:** 2.0 | **Actualización:** Agosto 2026

---

## ¿Qué es un aliado?

Los **aliados** son organizaciones de bienestar animal verificadas por el equipo PawTrack CR: rescatistas independientes, refugios, protectoras, grupos de rescate cantonales o nacionales.

El aliado tiene acceso a un **panel operativo** con alertas enriquecidas, herramientas de coordinación y estadísticas de recuperación.

---

## Activar tu perfil de aliado

El rol Aliado no se auto-asigna. El equipo de PawTrack CR habilita tu cuenta. Una vez habilitada:

1. Inicia sesión.
2. Ve a `/allies/panel`.
3. Completa el perfil de tu organización:
   - **Nombre de la organización**
   - **Tipo** (Rescatista individual / Refugio / Protectora / Grupo de rescate)
   - **Área de cobertura**: dibuja el polígono en el mapa o define el radio en metros.
   - **Radio de cobertura** en metros.
4. Guarda.
5. El equipo de PawTrack CR revisa y aprueba tu perfil.

Una vez aprobado, empezarás a recibir alertas de mascotas perdidas dentro de tu área de cobertura.

---

## Panel del aliado

Accede desde `/allies/panel`. El panel incluye:

### 1. Bandeja de alertas activas

Lista en tiempo real de mascotas perdidas dentro de tu zona de cobertura. Cada alerta incluye:

- Foto de la mascota
- Especie, raza, color
- Último lugar visto (mapa)
- Tiempo desde el reporte
- Enlace directo a la sala de caso y al perfil público

### 2. Casos asignados

Mascotas en cuya sala de caso estás participando activamente. Puedes acceder directamente a:

- El chat con el dueño
- El mapa de avistamientos
- La coordinación de búsqueda en campo

### 3. Historial de casos

Todos los casos en los que has participado, con su estado final (Reunificado / Cerrado).

---

## Recibir alertas

Para recibir alertas incluso cuando la app está cerrada:

1. Panel del aliado → **Notificaciones** → **Activar notificaciones push**.
2. Acepta el permiso del navegador.
3. Recibirás una notificación push cada vez que se reporte una mascota perdida dentro de tu área de cobertura.

Las alertas enviadas a aliados son **enriquecidas**: incluyen foto en alta resolución, coordenadas GPS exactas del último lugar visto, y el teléfono de contacto del dueño (información no pública).

---

## Participar en una búsqueda

### Acceder a la sala de caso

Desde la alerta, toca **Ver caso**. Desde la sala de caso puedes:

- Ver el mapa de avistamientos
- Chatear con el dueño
- Unirte a la coordinación de búsqueda en campo

### Coordinación en campo (Zonas de búsqueda)

Si el dueño activó la coordinación, toca el enlace de búsqueda que comparte. Verás la cuadrícula 7×7 (49 zonas de 300 m²).

Acciones disponibles en cada zona:

- **Reclamar**: "Estoy buscando aquí" — la zona se marca en tu color.
- **Limpiar**: "Ya revisé, no está aquí" — la zona queda marcada como despejada.
- **Liberar**: "No puedo continuar" — la zona vuelve a quedar disponible.

Los cambios son visibles en tiempo real para todos los participantes.

### Chat con el dueño

El chat es enmascarado y seguro. Tú sí puedes ver el teléfono de contacto del dueño (como rescatista autenticado), pero el dueño no ve tus datos personales.

---

## Estadísticas de recuperación

Accede a `/estadisticas` para ver:

- **Tasa de recuperación** por especie, raza y cantón
- **Resumen general**: total de reportes, reunificaciones, tiempo promedio de recuperación

Esta información es útil para planificar operaciones, reportes institucionales o solicitar recursos.

---

## Voluntario de custodia (Foster)

Los aliados también pueden registrarse como custodios. Ve a tu perfil y completa:

- Ubicación
- Especies y tamaños aceptados
- Días disponibles y disponibilidad actual

Cuando alguien reporta haber encontrado una mascota, el sistema puede sugerirte como custodio temporal.

---

## Probar los features como Aliado

| Feature                      | Cómo                                                                                                  |
| ---------------------------- | ----------------------------------------------------------------------------------------------------- |
| **Panel de aliado**          | `/allies/panel` con cuenta habilitada como Ally                                                       |
| **Alertas de zona**          | Crea un reporte de mascota perdida con un usuario Owner en el área de cobertura del aliado            |
| **Coordinación de búsqueda** | Activa coordinación desde la sala de caso (Owner), comparte el link, únete desde la cuenta del aliado |
| **Chat con dueño**           | Sala de caso → avistamiento → chat                                                                    |
| **Estadísticas**             | `/estadisticas`                                                                                       |

> **Usuario de prueba sugerido:** Cuenta con rol `Ally` con perfil de aliado completo y aprobado.

---

_PawTrack CR — Manual del Aliado Verificado_
