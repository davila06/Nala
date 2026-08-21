# PawTrack CR — Features Completas del Producto

> Versión: 4.0 | Última actualización: 2026-08-19  
> Estado: MVP Completo + Enterprise Hardened

---

## 1. Identidad digital de mascotas

| Feature               | Plan                              | Descripción                                          |
| --------------------- | --------------------------------- | ---------------------------------------------------- |
| Registro de mascota   | Explorador                        | Nombre, especie, raza, fecha de nacimiento           |
| Foto de perfil        | Explorador                        | Upload con resize automático a 800px                 |
| QR de identidad       | Explorador                        | URL pública `/p/{id}` con toda la info de la mascota |
| Microchip RFID        | Explorador                        | ISO 11784; campo libre de texto                      |
| Perfil público        | Explorador                        | Visible a cualquier persona que escanea el QR        |
| Historial de escaneos | Explorador (5) / Plus (ilimitado) | Quién y cuándo escaneó el QR                         |
| Hasta 3 mascotas      | Plus                              | —                                                    |
| Mascotas ilimitadas   | Familia                           | —                                                    |
| Exportar actividad    | Plus                              | CSV de escaneos e historial                          |
| Reactivar mascota     | Todos                             | Reunida → Activa                                     |

---

## 2. Emergencia y recuperación

| Feature                       | Plan                              | Descripción                                                   |
| ----------------------------- | --------------------------------- | ------------------------------------------------------------- |
| Reporte de pérdida            | Todos                             | GPS, foto, descripción, notas públicas, reward                |
| Mapa público                  | Todos                             | Pin en mapa visible a la comunidad                            |
| Avistamientos anónimos        | Todos                             | PiiScrubber automático; sin datos del reportante              |
| Contacto seguro               | Todos                             | Número de contacto encriptado; sin exposición directa         |
| Búsqueda visual por IA        | Explorador 3/mes · Plus ilimitado | Match de foto de mascota encontrada vs. perdidas activas      |
| Chat enmascarado              | Todos                             | Conversación directa owner ↔ rescatador sin revelar identidad |
| SignalR real-time chat        | Todos                             | Mensajes en tiempo real; sin polling visible                  |
| Predicción de movimiento      | Plus                              | Modelo probabilístico de dónde puede estar                    |
| Case Room activo              | Plus                              | Panel centralizado para el dueño: avistamientos, mapa, chat   |
| Difusión multicanal           | Plus                              | WhatsApp · Telegram · Facebook · Email al mismo tiempo        |
| Coordinación en campo         | Plus                              | Case Room con zonas 7×7 en tiempo real                        |
| Bot WhatsApp                  | Todos                             | Reporte conversacional sin abrir la web                       |
| Código de entrega segura      | Todos                             | 4 dígitos TOTP para reunificación física sin revelar datos    |
| Recompensa económica (Bounty) | Plus                              | Escrow SINPE + release automático al verificar código         |

---

## 3. Expediente médico digital

| Feature                     | Plan                     | Descripción                                                           |
| --------------------------- | ------------------------ | --------------------------------------------------------------------- |
| Ver count de registros      | Explorador               | Solo el número total; sin acceso al contenido                         |
| Vista previa 3 registros    | Plus                     | Tipo, fecha, descripción, veterinario                                 |
| Historial completo          | Familia                  | Todos los registros; editar y eliminar                                |
| 7 tipos de registros        | Plus/Familia             | Vaccine · Deworming · Checkup · Medication · Surgery · Dental · Other |
| Peso por visita             | Familia                  | `WeightKg` con tendencia histórica en gráfica                         |
| Medicación estructurada     | Familia                  | Dosis · frecuencia · duración · fecha fin                             |
| Recordatorios veterinarios  | Familia                  | Push notification antes de la fecha de vencimiento                    |
| Vista calendario            | Familia                  | Todos los recordatorios en vista mensual                              |
| Dashboard multi-mascota     | Familia                  | Resumen de alertas de salud para todas las mascotas                   |
| Exportar PDF anual          | Familia                  | Reporte completo por año con QR de verificación                       |
| Acceso clínica veterinaria  | Familia + consentimiento | Clínica autorizada puede ver expediente en su portal                  |
| Audit log de acceso         | Familia                  | Registro de cada acceso de clínica al expediente                      |
| HealthScore                 | Plus                     | Score 0-100 basado en cumplimiento de protocolos por especie          |
| Alertas proactivas de salud | Plus                     | Notificación cuando un protocolo está próximo a vencer                |

---

## 4. Collar GPS

| Feature                | Plan | Descripción                                             |
| ---------------------- | ---- | ------------------------------------------------------- |
| Integración Tractive   | Plus | OAuth2 → posición en tiempo real                        |
| Soporte genérico OEM   | Plus | HTTP push desde cualquier dispositivo                   |
| Historial de ubicación | Plus | Últimas 24h de trayectoria                              |
| Tab GPS en perfil      | Plus | Vista de última posición + historial                    |
| Ownership protegido    | —    | Solo el dueño puede ver el GPS de su mascota (BOLA fix) |

---

## 5. Red colaborativa

| Feature                           | Descripción                                                                 |
| --------------------------------- | --------------------------------------------------------------------------- |
| **Aliados verificados**           | Refugios, veterinarias, seguridad privada con zona de cobertura declarada   |
| **Custodios temporales**          | Voluntarios que cuidan mascotas mientras el dueño no puede                  |
| **Alertas geofenceadas**          | Push a vecinos dentro del radio cuando se reporta una pérdida               |
| **Red vecinal (Guardia Vecinal)** | Vecinos que optan por recibir alertas ultra-locales de su cuadra            |
| **Leaderboard**                   | Ranking público de rescatadores más activos (solo primer nombre)            |
| **Score de contribución**         | Puntos por reunificaciones exitosas; badges: Helper/Rescuer/Guardian/Legend |

---

## 6. Tiendas de mascotas (B2B Store)

| Feature                       | Plan         | Descripción                                                                    |
| ----------------------------- | ------------ | ------------------------------------------------------------------------------ |
| Registro de tienda            | Público      | Formulario + ubicación en mapa + aprobación admin                              |
| Directorio público `/tiendas` | Público      | Búsqueda por nombre y dirección                                                |
| Mapa con pins de tiendas      | Público      | Click en pin → abre catálogo directamente                                      |
| Deep-link `/mapa?storeId=X`   | Público      | Abre el mapa con la tienda seleccionada                                        |
| Catálogo de productos         | Público      | 7 categorías: Food, Accessories, Grooming, Health, Toys, Clothing, Other       |
| Imágenes de productos         | Store        | Upload con resize 800px; blob storage                                          |
| Carrito multi-tienda guard    | Público auth | Zustand persist; aviso si se mezclan tiendas                                   |
| Checkout SINPE Móvil          | StorePlus+   | Referencia de pago generada; cliente reporta pago                              |
| Pedidos in-app                | StorePlus+   | Plan gate: solo StorePlus y StorePartner                                       |
| Dashboard de tienda           | Store        | Stats, pedidos recientes, accesos rápidos                                      |
| Gestión de pedidos            | Store        | Confirmar, avanzar estado, cancelar                                            |
| Estado máquina de pedidos     | —            | PendingPayment→PaymentReported→Confirmed→Preparing→(Pickup/Delivery)→Delivered |
| Mis pedidos `/mis-pedidos`    | Autenticado  | Historial paginado con progress bar por estado                                 |
| Notificación nuevo pedido     | Store        | Push + in-app notification al recibir pedido                                   |
| Aprobación admin              | Admin        | Tab "Tiendas" en panel admin                                                   |

---

## 7. Vallas publicitarias (Billboard)

| Feature              | Descripción                                  |
| -------------------- | -------------------------------------------- |
| 4 placements         | Map · Dashboard · Directory · Feed           |
| Estado máquina       | Draft → Active ↔ Paused → Expired            |
| Prioridad            | 0-100; mayor prioridad se muestra primero    |
| Imagen               | Upload 5MB; resize automático a 1200px       |
| CTA seguro           | URL validada: solo same-origin o HTTPS       |
| Dismissal por sesión | sessionStorage; no vuelve hasta nueva sesión |
| Max 5 por placement  | Cap para no saturar la UI                    |
| Admin CRUD completo  | Crear · editar · activar · pausar · imagen   |

---

## 8. B2B Clínicas veterinarias

| Feature               | Plan          | Descripción                                  |
| --------------------- | ------------- | -------------------------------------------- |
| Portal veterinario    | ClinicBasic+  | Escanear QR/RFID de mascotas                 |
| Notificación al dueño | ClinicBasic+  | Push cuando la clínica escanea a su mascota  |
| Directorio clínicas   | Público       | Listado con filtro de emergencia 24h         |
| Expediente compartido | ClinicPlus+   | Con consentimiento explícito del dueño       |
| Audit log acceso      | ClinicPlus+   | Registro de cada consulta al expediente      |
| Certificados PDF      | ClinicPlus+   | Verificables con QR; hash en blockchain-lite |
| API keys              | ClinicPartner | Integración con HIS propietario              |
| Posición destacada    | ClinicPartner | Ícono 24h emergencia en mapa                 |

---

## 9. B2G Municipalidades

| Feature            | Plan        | Descripción                                |
| ------------------ | ----------- | ------------------------------------------ |
| Portal de capturas | Básica+     | Registro de animales capturados            |
| Fotos de animales  | Full+       | Upload de fotos en capturas                |
| Estadísticas       | Full+       | Reportes de capturas por período           |
| Multi-cantón       | RedRegional | Coordinación entre cantones                |
| Red regional       | RedRegional | Compartición de registros entre municipios |

---

## 10. Suscripciones y planes

| Feature                             | Descripción                                                      |
| ----------------------------------- | ---------------------------------------------------------------- |
| Feature gating completo             | Todas las funciones premium verifican el plan activo             |
| Freemium sin fricción               | Explorador completamente funcional para emergencias              |
| SINPE Móvil nativo                  | Pago directo sin pasarela externa; referencia generada           |
| Activación manual                   | Admin verifica pago y activa el plan                             |
| Renovación                          | Manual; sin renovación automática en MVP                         |
| Plan Familia multi-usuario          | Hasta 5 miembros; alertas push a todos                           |
| Token de invitación CSPRNG          | `RandomNumberGenerator.GetBytes(16)` — criptográficamente seguro |
| Verificación de email en invitación | El invitado debe tener el mismo email que la invitación          |

---

## 11. Seguridad y privacidad

| Feature                     | Descripción                                                 |
| --------------------------- | ----------------------------------------------------------- |
| JWT + refresh tokens        | Access 15min; refresh 30 días; absolute max 90 días         |
| Token theft detection       | Refresh rotado → detecta replay → revoca todas las sesiones |
| Lockout de cuenta           | 5 intentos fallidos → 15 min lockout                        |
| JTI Blocklist distribuido   | SQL-backed; funciona en multi-instancia                     |
| bcrypt work factor 12       | OWASP recommended para 2026                                 |
| Anti-enumeración            | Register, forgot-password siempre 201/Accepted              |
| PiiScrubber                 | Notas de avistamiento y mensajes de chat                    |
| Teléfonos hasheados         | HMAC-SHA256 con clave secreta — no SHA-256 plain            |
| BOLA protegido              | Collars, Bounties, Family, Pets — ownership check           |
| Leaderboard privacidad      | Solo primer nombre (max 20 chars)                           |
| Push subscription ownership | No se puede registrar el endpoint de otro usuario           |
| AllowedHosts restringido    | No `*`; hostnames específicos en producción                 |
| CSP en SWA                  | globalHeaders en staticwebapp.config.json                   |
| SW sin open redirect        | Validación de URL antes de navegar desde notificación       |
| Auth excluida del SW cache  | `/api/auth/*` no se cachea en NetworkFirst                  |

---

## 12. PWA y experiencia móvil

| Feature                   | Descripción                                       |
| ------------------------- | ------------------------------------------------- |
| Installable PWA           | Add to homescreen en Android/iOS                  |
| Offline ready             | Caché de assets con Workbox                       |
| Push notifications web    | VAPID sin proveedor externo                       |
| Update banner inteligente | Muestra "Actualizar / Después" — no fuerza reload |
| Pull to refresh           | Dashboard y otras listas clave                    |
| Mapa full-screen          | Leaflet con layers: eventos + clínicas + tiendas + adopciones  |
| SignalR real-time         | Chat de mensajes y coordinación de búsqueda       |

---

## 13. Módulo de Adopciones

### Para adoptantes (público / Owner)

| Feature | Plan | Descripción |
|---|---|---|
| Directorio público de animales | Todos | Grid paginado filtrable por especie, tamaño, edad, vacunación, zona |
| Filtro geográfico (GPS) | Todos | Radio configurable 10–100 km desde la ubicación del visitante |
| Pins de adopción en mapa | Todos | Toggle "Adopciones" en el mapa público; pins morados distintos a los de mascotas perdidas |
| Perfil detallado del animal | Todos | Galería de fotos, historia, requisitos, notas médicas, badges de salud |
| Solicitar adopción | Owner | Formulario con nota personal; un solo pending por animal |
| Ver mis solicitudes | Owner | Estado de cada solicitud; respuesta de la organización |
| Retirar solicitud | Owner | Solo si está en Pending o UnderReview |
| Ferias de adopción | Todos | Listado geofenceado de eventos presenciales próximos |

### Para shelters (Ally verificado con AllyType.Shelter)

| Feature | Plan | Descripción |
|---|---|---|
| Publicar animal en adopción | ShelterBasic (gratis, máx 5) / ShelterPlus (ilimitado) | Nombre, especie, tamaño, historia, fotos (hasta 5), zona de referencia |
| Gestionar fotos | Ally | Upload multi-foto a Azure Blob `adoption-photos/`; delete individual |
| Editar perfil del animal | Ally | Actualizar texto, características y flags de salud |
| Cambiar estado del animal | Ally | Available → InProcess → Adopted / Paused / Removed |
| Ver solicitudes por animal | Ally | Lista de aplicantes con nota personal de cada uno |
| Aprobar / rechazar solicitud | Ally | Con nota de respuesta opcional; se notifica al adoptante |
| Marcar como adoptado | Ally | Cierra el ciclo; el animal desaparece del directorio activo |
| Crear ferias de adopción | ShelterPlus | Evento con fecha, lugar GPS, lista de animales presentes |
| Panel del shelter | Ally | Lista paginada de todos sus animales con estado y acciones |

### Notificaciones de adopción

| Tipo | Destinatario | Canal |
|---|---|---|
| `AdoptionInterest` | Shelter | In-app + push |
| `AdoptionApproved` | Adoptante | In-app + push |
| `AdoptionRejected` | Adoptante | In-app + push |
| `AdoptionFairAlert` | Usuarios en radio 10km | Push geofenceado con rate limiting |

### Monetización de adopciones

| Plan | Precio | Límite |
|---|---|---|
| ShelterBasic | Gratis | 5 animales activos simultáneos; sin ferias |
| ShelterPlus | ₡8,000/mes | Animales ilimitados + ferias de adopción + pin destacado |

### WhatsApp Bot — intents de adopción

| Keyword | Respuesta |
|---|---|
| "adoptar", "adopcion", "quiero adoptar" | Link al directorio + ferias |
| "dar en adopcion", "tengo animales", "shelter", "refugio" | Instrucciones para registrarse como Ally Shelter |
