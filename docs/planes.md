# PawTrack CR — Catálogo Oficial de Planes, Tiers y Características por Rol

> **Estado del documento:** Catálogo técnico y operativo actualizado al 2026-09-10.
> **Fuente canónica en código:** [backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs](backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs), [backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs](backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs), [backend/src/PawTrack.Domain/ServiceProviders/ProviderMembershipTier.cs](backend/src/PawTrack.Domain/ServiceProviders/ProviderMembershipTier.cs) y [backend/src/PawTrack.Domain/Auth/UserRole.cs](backend/src/PawTrack.Domain/Auth/UserRole.cs).
> **Documentos de soporte:** [PRICING_AND_PLANS.md](PRICING_AND_PLANS.md), [B2B_ESTADO_ACTUAL.md](B2B_ESTADO_ACTUAL.md), [pendientesTiendas.md](pendientesTiendas.md) y [pruebas.md](pruebas.md).

---

## 1. Resumen ejecutivo de roles y planes

PawTrack CR implementa un modelo de monetización multiactor adaptado a Costa Rica. Cada uno de los **8 roles de usuario** (`UserRole`) cuenta con niveles de servicio, capacidades de feature-gating y modelos comerciales claramente delimitados:

| Rol de Usuario        | Segmento             | Planes / Tiers Disponibles                                                                                      | Tarifa Oficial                                               | Modalidad                          |
| :-------------------- | :------------------- | :-------------------------------------------------------------------------------------------------------------- | :----------------------------------------------------------- | :--------------------------------- |
| **`Owner`**           | B2C (Dueños)         | **Explorador** (`Free`)<br>**Plus** (`UserPlus`)<br>**Familia** (`UserFamilia`)                                 | Gratis<br>₡2,990 / mes<br>₡4,990 / mes                       | Mensual / Plazos (1, 3, 6, 12m)    |
| **`Ally`**            | B2B Social (Rescate) | **Aliado Comunitario** (Base)<br>**Shelter Base** (`ShelterBasic`)<br>**Shelter Plus** (`ShelterPlus`)          | Gratis (Verificado)<br>Gratis (≤ 5 animales)<br>₡8,000 / mes | Mensual                            |
| **`Clinic`**          | B2B (Veterinarias)   | **Afiliada Básica** (`ClinicBasic`)<br>**Clínica Plus** (`ClinicPlus`)<br>**Clínica Partner** (`ClinicPartner`) | Gratis<br>₡15,000 / mes<br>₡35,000 / mes                     | Mensual                            |
| **`Store`**           | B2B (Pet Shops)      | **Store Básica** (`StoreBasic`)<br>**Store Plus** (`StorePlus`)<br>**Store Partner** (`StorePartner`)           | Gratis<br>₡12,000 / mes<br>₡25,000 / mes                     | Mensual                            |
| **`ServiceProvider`** | B2B (Servicios)      | **Free** (Directorio)<br>**Verified** (Catálogo/Reservas)<br>**Featured** (Destacado)                           | Gratis<br>30 días prueba gratis<br>Destaque preferencial     | Operativo / Sin fee mensual activo |
| **`Municipality`**    | B2G (Gobierno local) | **Muni Básica** (`MuniBasica`)<br>**Muni Full** (`MuniFull`)<br>**Red Regional** (`MuniRedRegional`)            | ₡150,000 / año<br>₡300,000 / año<br>₡500,000 / año           | Anual institucional                |
| **`Support`**         | Operativo interno    | **Especialista de Bienestar Animal**                                                                            | Sin costo (Asignable)                                        | Rol operativo de plataforma        |
| **`Admin`**           | Gobernanza           | **Superadministrador**                                                                                          | Sin costo (Gobernanza)                                       | Control integral de plataforma     |

---

## 2. Rol `Owner`: Dueños de Mascotas (B2C)

Los dueños de mascotas constituyen el núcleo de la red de búsqueda y tenencia responsable.

### 2.1 Explorador (`Free`) — Gratis

Plan de entrada sin costo para cualquier persona que registre a su mascota.

- **Límite de mascotas:** 1 mascota activa en la cuenta.
- **Identidad digital QR:** Código QR único descargable y visualizable en el perfil público.
- **Historial de escaneos:** Limitado a los últimos 5 escaneos registrados.
- **Búsqueda visual IA:** Hasta 3 comparaciones fotográficas mensuales en `/map/match`.
- **Reportes de pérdida y avistamiento:** Reporte de pérdida activo con radio de notificación base de 3 km a vecinos registrados.
- **Mapa público:** Visualización de alertas de mascotas perdidas y avistamientos anónimos.
- **Expediente médico:** Conteo total de registros existentes (sin desglose detallado de tratamientos).
- **Restricciones técnicas:** Sin tab de collar GPS, sin sala de coordinación en vivo (Case Room), sin depósito de recompensas (Bounty) y sin cuenta multi-usuario.

### 2.2 Plus (`UserPlus`) — ₡2,990/mes

Diseñado para dueños que buscan geolocalización activa y herramientas avanzadas de rescate.

- **Todo lo del plan Explorador, más:**
- **Límite de mascotas:** Hasta 3 mascotas simultáneas en la misma cuenta.
- **Historial de escaneos:** Ilimitado con registro de dispositivo, fecha y ubicación.
- **Búsqueda visual IA:** Comparaciones fotográficas ilimitadas para agilizar la identificación.
- **Radio de alerta extendido:** Difusión geofenceada de 10 km ante reportes de pérdida.
- **Pestaña GPS y hardware:** Vinculación de collares GPS oficiales (CollarTags con serial `PT-[0-9A-Fa-f]{4}-\d{7}`), dispositivos Tractive (vía OAuth2) y hardware genérico HTTP push.
- **Seguridad perimetral:** Creación de zonas seguras (geofencing) con alertas de entrada/salida, monitoreo de nivel de batería y alertas de desconexión (offline).
- **Case Room interactivo:** Sala de búsqueda activa con chat SignalR en tiempo real, mapa de cuadrantes y coordinación de rescatistas en campo.
- **Fondo de Recompensa (Bounty):** Capacidad de fijar recompensas en custodia con depósito SINPE y liberación mediante código PIN (`HandoverCode`).
- **Expediente médico:** Vista previa de los últimos 3 registros clínicos (tipo de atención, fecha, veterinaria actuante y descripción general).

### 2.3 Familia (`UserFamilia`) — ₡4,990/mes

La suite definitiva para hogares con múltiples animales que requieren gestión clínica exhaustiva y colaboración familiar.

- **Todo lo del plan Plus, más:**
- **Límite de mascotas:** Mascotas ilimitadas registradas bajo la misma cuenta.
- **Multi-usuario familiar:** Hasta 5 miembros familiares en la misma cuenta (`FamilyMemberRole.Owner` y `FamilyMemberRole.Member`) mediante invitación por token criptográfico de un solo uso.
- **Expediente médico digital completo:** Acceso total e ilimitado al historial clínico de cada mascota (consultas, vacunas, desparasitaciones, cirugías, exámenes de laboratorio y alergias).
- **Medicación estructurada:** Registro de prescripciones con posología, frecuencia, duración y cálculo automático de fecha fin de tratamiento.
- **Evolución biométrica:** Registro y gráficas de tendencia de peso corporal (`WeightTrendChart`).
- **Calendario y recordatorios:** Agenda preventiva interactiva (`VetReminders`) con alertas automáticas previas a vencimientos de vacunas y desparasitaciones.
- **Exportación oficial:** Generación y descarga del expediente médico en formato PDF consolidado.
- **Adopción particular moderada:** Facilidad para solicitar dar en adopción responsable a una mascota registrada (`/adopciones/solicitar`), sujeta a moderación administrativa.
- **Portabilidad de datos:** Exportación self-service de datos personales bajo cumplimiento de la Ley 8968 (Protección de la Persona frente al Tratamiento de sus Datos Personales).

### 2.4 Términos de contratación y política de ciclo de vida B2C

Los planes `UserPlus` y `UserFamilia` admiten contratación en 4 modalidades de plazo mediante comprobante SINPE Móvil validado por administración:

- **1 mes:** Tarifa mensual regular (₡2,990 para Plus / ₡4,990 para Familia).
- **3 meses:** ₡8,970 (Plus) / ₡14,970 (Familia).
- **6 meses:** ₡17,940 (Plus) / ₡29,940 (Familia).
- **12 meses (Anual con 20% de descuento):** ₡28,704 al año (equivale a ₡2,392/mes para Plus) / ₡47,904 al año (equivale a ₡3,992/mes para Familia).

**Reglas de cancelación y downgrade:**

- La cancelación detiene renovaciones futuras pero mantiene los privilegios del plan vigente hasta su fecha efectiva de expiración (`ExpiresAt`).
- El downgrade voluntario (ej. Familia a Plus) se programa para la fecha de corte sin destruir datos: las mascotas y miembros familiares existentes se conservan intactos; únicamente se congelan nuevas altas si se excede el cupo de 3 mascotas y se reduce la visualización del expediente médico a los últimos 3 registros.

---

## 3. Rol `Ally`: Red de Aliados y Refugios (B2B Social)

El rol `Ally` agrupa a organizaciones comunitarias, rescatistas independientes, veterinarias solidarias y albergues de animales. Los perfiles son clasificados por su `AllyType`: `Shelter`, `VeterinaryClinic`, `PetFriendlyBusiness`, `PrivateSecurity` o `Municipality`.

### 3.1 Aliado Comunitario — Gratis (Verificación documental obligatoria)

Diseñado para comercios pet-friendly, grupos de rescate y empresas de seguridad privada.

- **Validación administrativa:** Requiere solicitud con datos de cobertura física y atestados, aprobada por Admin (`VerificationStatus.Verified`).
- **Bandeja de alertas zonales:** Recepción en tiempo real de notificaciones de mascotas perdidas geofenceadas dentro del radio de cobertura declarado (`CoverageRadiusMetres`, latitud y longitud central).
- **Acción coordinada en campo:** Botón de confirmación operativa ("Ya buscamos en nuestra área") que notifica de inmediato a la familia afectada.
- **Métricas de impacto:** Dashboard de KPIs con conteo de alertas recibidas, alertas atendidas, tasa porcentual de respuesta y radio de cobertura.
- **Presencia comunitaria:** Perfil oficial en el directorio de aliados de PawTrack CR.

### 3.2 Refugios — Acceso Base (`ShelterBasic`) — Gratis

Acceso de entrada para albergues y rescatistas que publican animales rescatados.

- **Directorio público de adopciones:** Publicación de hasta 5 animales activos simultáneamente en `/adopciones`.
- **Gestión de solicitudes:** Recepción de formularios de adopción enviados por usuarios registrados (`AdoptionApplications`).
- **Estados del animal:** Control de estados de adopción (`Available`, `InProcess`, `Adopted`, `Paused`, `Removed`).

### 3.3 Refugios — ShelterPlus (`ShelterPlus`) — ₡8,000/mes

Diseñado para organizaciones formales de rescate y adopción de alto volumen.

- **Todo lo del acceso base, más:**
- **Animales ilimitados:** Publicación sin restricción de cantidad de animales rescatados en adopción.
- **Ferias de adopción presenciales:** Creación y publicación de eventos en `/adopciones/ferias` con fecha, horario, sede en mapa GPS y lista de animales participantes.
- **Difusión geofenceada de ferias:** Notificación automática a todos los usuarios registrados en un radio de 10 km alrededor de la sede de la feria.
- **Destaque en mapa de adopciones:** Pines destacados con distintivo visual prioritario en la capa interactiva de adopciones.
- **Canal de comunicación enmascarado:** Hilo de chat directo con adoptantes precalificados.

---

## 4. Rol `Clinic`: Clínicas Veterinarias (B2B)

Permite a los centros médicos veterinarios vincularse al expediente digital de mascotas, ofrecer servicios de emergencia y emitir certificaciones oficiales.

### 4.1 Afiliada Básica (`ClinicBasic`) — Gratis

- **Directorio y mapa veterinario:** Perfil público estándar con dirección física, coordenadas GPS, teléfono, horario y servicios.
- **Escaneo universal:** Lectura de códigos QR de placas PawTrack para atención inmediata de mascotas extraviadas o heridas.
- **Lector RFID ISO 11784:** Búsqueda directa por número de microchip para identificar pacientes en base nacional.
- **Acceso al expediente médico:** Capacidad de consultar y escribir consultas, vacunas y diagnósticos en el expediente del paciente mediante autorización explícita del dueño (`ClinicMedicalAccessGrant` activo).

### 4.2 Clínica Plus (`ClinicPlus`) — ₡15,000/mes

Enfocado en clínicas que desean maximizar su captación de pacientes y posicionamiento local.

- **Todo lo de Afiliada Básica, más:**
- **Posicionamiento prioritario:** Destaque preferencial en el mapa y directorio general de veterinarias.
- **Badge oficial:** Distintivo de "Clínica Verificada" visible en su perfil público.
- **Patrocinio en alertas de rescate:** Inclusión automática del logotipo de la clínica en las alertas de mascotas perdidas enviadas a usuarios cercanos vía WhatsApp, Telegram y Correo Electrónico.
- **Presencia en Case Rooms:** Banner publicitario de la clínica en las salas de búsqueda activa del cantón.
- **Analítica de visibilidad:** Métricas de visitas al perfil, búsquedas por zona y cantidad de escaneos realizados.
- **Soporte prioritario:** Atención preferencial por correo electrónico.

### 4.3 Clínica Partner (`ClinicPartner`) — ₡35,000/mes

Suite integral para hospitales veterinarios y clínicas que operan con altos estándares de certificación digital.

- **Todo lo de Clínica Plus, más:**
- **Certificados Veterinarios PDF verificables:** Emisión digital de certificados médicos con código criptográfico único, renderizado QuestPDF de alta resolución y verificación pública inmediata en `/verificar/{código}`.
- **Pasaporte Oficial de Vacunas SENASA-Ready:** Emisión del pasaporte oficial digital que valida esquema de vacunación (incluyendo rabia obligatoria en perros), desparasitación y microchip bajo estricta cadena de custodia.
- **Validación SENASA:** Requiere clínica verificada (`ClinicVerification.Verified`) y médico veterinario autorizado con carné activo del Colegio de Médicos Veterinarios de Costa Rica (`ClinicVeterinarian.Authorized`).
- **Gestión de cuerpo médico:** Alta y administración de múltiples médicos veterinarios con control granular de permisos (`medical:read`, `medical:write`, `certificates:issue`).
- **Integración API Keys:** Generación y rotación de claves API seguras para sincronización con software veterinario de escritorio o ERPs externos.
- **Widget web embebible:** Código iframe/script para incorporar el buscador de pacientes y validador de certificados directamente en la página web propia de la clínica.
- **Atención VIP:** Soporte 24/7 y gerente de cuenta dedicado.

---

## 5. Rol `Store`: Tiendas de Mascotas y Pet Shops (B2B)

Permite a tiendas de mascotas comercializar accesorios, alimento y productos directamente a la comunidad PawTrack.

### 5.1 Store Básica (`StoreBasic`) — Gratis

- **Directorio de tiendas:** Presencia comercial en el directorio y mapa interactivo de comercios para mascotas.
- **Ficha pública:** Nombre comercial, descripción, dirección física, ubicación GPS, teléfono de contacto y enlace a WhatsApp comercial.

### 5.2 Store Plus (`StorePlus`) — ₡12,000/mes

- **Todo lo de Store Básica, más:**
- **Catálogo de productos digital:** Publicación de catálogo con fotografías optimizadas (procesadas a 800px), precios en colones y clasificación por categorías canónicas (`Food`, `Accessories`, `Grooming`, `Health`, `Toys`, `Clothing`, `Other`).
- **Recepción de pedidos in-app:** Módulo transaccional de compras dentro de la app con método de pago mediante SINPE Móvil (`StoreOrders`).
- **Máquina de estados de orden:** Ciclo completo de gestión (`PendingPayment` → `PaymentReported` → `Confirmed` → `Preparing` → `ReadyForPickup` / `OutForDelivery` → `Delivered`).
- **Modalidades de despacho:** Retiro en mostrador (Pickup) o entrega a domicilio (Delivery) con dirección y notas del cliente.
- **Panel de órdenes en tiempo real:** Gestión y control de despachos en `/tienda/portal/ordenes` con avisos inmediatos al cliente.
- **Badge comercial:** Distintivo destacado en el mapa y directorio de pet shops.

### 5.3 Store Partner (`StorePartner`) — ₡25,000/mes

- **Todo lo de Store Plus, más:**
- **Soporte multi-sucursal:** Administración centralizada de múltiples sedes físicas (`StoreLocations`).
- **Analítica comercial:** Reportes de volumen de pedidos, productos más vendidos y demanda geográfica por cantón.
- **Posicionamiento preferencial:** Primeros puestos en el marketplace de compras para mascotas.
  _(Nota operativa: en la fase actual de lanzamiento, la prioridad está en el despliegue de tiendas individuales con catálogo directo)._

---

## 6. Rol `ServiceProvider`: Marketplace de Servicios (B2B)

Conecta a profesionales y técnicos independientes con dueños de mascotas. Agrupa 7 categorías de servicio:

1. `Trainer` (Adiestradores y educadores caninos)
2. `Groomer` (Peluquería, baño y estética)
3. `Hotel` (Hospedaje de corta y larga estancia)
4. `Daycare` (Guardería diurna)
5. `Walker` (Paseadores de perros)
6. `Photographer` (Fotografía profesional de mascotas)
7. `Other` (Otros servicios especializados)

### 6.1 Tiers de membresía técnica

| Nivel de Membresía | Costo                                                                                | Características y Capacidades                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| :----------------- | :----------------------------------------------------------------------------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Free**           | Gratis                                                                               | Perfil visible en directorio `/servicios` y mapa. Sin catálogo activo ni reservas in-app (`HasCatalogAccess = false`).                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| **Verified**       | **30 días gratis** (Prueba inicial al aprobarse)<br>_Precio comercial en definición_ | - **Catálogo ilimitado:** Servicios con nombre, descripción, modalidad (`AtProviderFacility`, `AtCustomerHome`, `Virtual`), duración en minutos, precio en CRC y capacidad simultánea.<br>- **Reglas de disponibilidad:** Horarios semanales día a día (09:00 a 17:00) y bloques de excepción para feriados o vacaciones.<br>- **Gestión de reservas in-app:** Ciclo transaccional completo (`Requested` → `Confirmed` → `InProgress` → `Completed` / `Cancelled` / `NoShow` / `Expired`).<br>- **Validación documental:** Cédula y atestados revisados por el equipo de PawTrack. |
| **Featured**       | Destaque preferencial                                                                | Todo lo de Verified, más distintivo de destaque en búsquedas, prioridad en directorio y protección contra expiración automática (`IsMembershipManual = true`).                                                                                                                                                                                                                                                                                                                                                                                                                     |

---

## 7. Rol `Municipality`: Gobiernos Locales (B2G)

Dirigido a las oficinas de Gestión Ambiental, Salud y Control Animal de las 82 municipalidades de Costa Rica. Su contratación se estructura bajo modalidad de **facturación anual institucional**.

### 7.1 Muni Básica (`MuniBasica`) — ₡150,000/año

- **Portal municipal dedicado:** Acceso exclusivo a `/municipalidad` para inspectores y funcionarios autorizados.
- **Gestión cantonal exclusiva:** Cobertura delimitada a un único cantón oficial.
- **Registro digital de capturas:** Bitácora de animales recogidos en vía pública (`CapturedAnimals`) con especie, raza, color, edad estimada y placa/collar.
- **Cruce automatizado con PawTrack:** Identificación inmediata por número de microchip o código QR para avisar al dueño registrado antes del ingreso formal a custodia municipal.
- **Control de estados de custodia:** Flujo operativo: `Received` (recibido), `OwnerFound` (dueño localizado), `Transferred` (transferido), `Released` (liberado/silvestre), `Adopted` (adoptado).
- **Mapa operativo cantonal:** Vista geoespacial de capturas e incidentes en el territorio.

### 7.2 Muni Full (`MuniFull`) — ₡300,000/año

- **Todo lo de Muni Básica, más:**
- **Registro fotográfico digital:** Almacenamiento de fotos en alta resolución de cada captura municipal para facilitar el reconocimiento público.
- **Mapa de calor y estadísticas:** Analítica territorial por distrito y barrio con índices de abandono y tasa de recuperación.
- **Reportes institucionales estructurados:** Exportación de informes compatibles con las directrices de SENASA y PANI.
- **API institucional:** Puntos de integración para alimentar el sistema de atención ciudadana (CRM municipal).

### 7.3 Muni Red Regional (`MuniRedRegional`) — ₡500,000/año

- **Todo lo de Muni Full, más:**
- **Cobertura multi-cantón:** Licencia consorciada para federaciones municipales o alianzas inter-cantonales (ej. Red Regional Norte: Alajuela, Grecia, Poás, San Carlos).
- **Transferencias inter-municipales:** Registro formal de traslados de animales entre albergues de diferentes municipalidades aliadas.
- **Dashboard regional unificado:** Panel macro con estadísticas consolidadas de bienestar animal a nivel de provincia o región.

---

## 8. Rol `Support`: Bienestar Animal y Operaciones Internas

Rol asignable exclusivamente por el Administrador (`UserRole.Support`) sin costo de suscripción. Permite delegar la atención ciudadana de bienestar animal sin otorgar permisos de superusuario.

- **Triage de denuncias ciudadanas:** Acceso a la bandeja `/api/admin/welfare-cases` para revisar casos reportados por la población.
- **Tipologías de caso soportadas:** `Abandonment` (abandono), `SuspectedAbuse` (sospecha de maltrato), `Neglect` (negligencia), `InjuredAnimal` (animal herido), `AnimalAtRisk` (animal en riesgo), `MunicipalCapture` (captura municipal), `Hoarding` (acumulación), `IrregularAdoption` (adopción irregular).
- **Clasificación de severidad:** `Low` (baja), `Medium` (media), `High` (alta), `Critical` (crítica).
- **Bitácora interna confidencial:** Incorporación de notas técnicas de seguimiento (`AnimalWelfareCaseNotes`) no visibles al denunciante.
- **Derivación institucional:** Asignación de casos a refugios verificados o derivación formal a las autoridades correspondientes.

---

## 9. Rol `Admin`: Superadministrador de Plataforma

Rol de gobernanza global (`UserRole.Admin`) responsable de la integridad operativa, comercial y legal de PawTrack CR.

- **Aprobaciones B2B pendientes:** Revisión y autorización de solicitudes de aliados (`AllyProfiles`) y clínicas veterinarias (`Clinics`, `ClinicVerifications`).
- **Moderación de adopciones particulares:** Bandeja de revisión y aprobación obligatoria de mascotas puestas en adopción por dueños particulares (`AdoptionStatus.PendingReview` → `Approve` / `Reject`).
- **Conciliación financiera SINPE Móvil:** Consulta de referencias de pago de 8 caracteres y activación formal de planes en `/admin` → pestaña Suscripciones.
- **Gobernanza publicitaria:** Creación, aprobación, pausa y seguimiento de campañas de Vallas Publicitarias (`Billboards`).
- **Aprovisionamiento de hardware:** Control de inventario de collares, registro de lotes de CollarTags de fábrica y revocación por garantía.
- **Analítica de activación nacional:** Métricas del embudo de producto (`/api/product-events/funnel`) para medir adopción, retención y reunificaciones en el país.

---

## 10. Matriz técnica de acceso al expediente médico digital

El acceso al expediente clínico de la mascota está regulado estrictamente para balancear la privacidad del propietario con la continuidad médica:

| Capacidad del Expediente Médico             | Dueño Explorador (`Free`) | Dueño Plus (`UserPlus`) | Dueño Familia (`UserFamilia`) | Clínica Afiliada (`Clinic`) |
| :------------------------------------------ | :-----------------------: | :---------------------: | :---------------------------: | :-------------------------: |
| **Conteo total de registros**               |            ✅             |           ✅            |              ✅               |             ✅              |
| **Vista previa básica (últimos 3)**         |             ✗             |           ✅            |              ✅               |             ✅              |
| **Historial clínico completo**              |             ✗             |            ✗            |              ✅               |       ✅ (con grant)        |
| **Curva de peso biométrica**                |             ✗             |            ✗            |              ✅               |       ✅ (con grant)        |
| **Medicación estructurada y fin de dosis**  |             ✗             |            ✗            |              ✅               |       ✅ (con grant)        |
| **Calendario y recordatorios veterinarios** |             ✗             |            ✗            |              ✅               |       ✅ (con grant)        |
| **Exportación a documento PDF**             |             ✗             |            ✗            |              ✅               |       ✅ (con grant)        |
| **Escritura de nuevas consultas/vacunas**   |             ✗             |            ✗            |               ✗               |       ✅ (con grant)        |

> **Principio de continuidad veterinaria:** Cualquier clínica veterinaria verificada puede registrar atenciones médicas en el expediente de un paciente si cuenta con un `ClinicMedicalAccessGrant` activo otorgado por el dueño, sin importar qué plan tenga el propietario. Sin embargo, el dueño requiere el **Plan Familia** para acceder a la lectura completa, gráficas y exportación de su expediente.

---

## 11. Productos y servicios complementarios

### 11.1 Vallas publicitarias digitales (`Billboards`)

Espacios de difusión visual ética y contextual dentro de la plataforma para marcas de cuidado animal y anuncios comunitarios:

- **Placements:** `Map` (mapa en vivo), `Dashboard` (panel de mascotas), `Directory` (directorio de comercios), `Feed` (alertas de pérdida), `AdoptionDirectory` (directorio de adopciones), `AdoptionFair` (ferias de adopción).
- **Niveles de campaña:**
  - _Standard:_ Rotación equitativa en placements generales.
  - _VIP:_ Prioridad alta (90), exclusividad por categoría comercial, segmentación por cantón específico y límite de frecuencia diaria por usuario.

### 11.2 Fondo de recompensa garantizada (`Bounty`)

- Exclusivo para usuarios Plus y Familia.
- El dueño deposita el monto de la recompensa en garantía vía SINPE Móvil.
- Al reunificarse la mascota, la liberación de fondos se realiza de forma segura mediante un código de entrega de 6 dígitos (`HandoverCode`).
- PawTrack retiene una comisión de plataforma del 10% únicamente sobre reunificaciones confirmadas con éxito.

### 11.3 Collares reflectivos y hardware GPS

- **Collar oficial PawTrack QR:** Collar de reata reforzada con placa QR grabada en láser y serial alfanumérico verificable.
- **Hardware GPS compatible:** Integración nativa por software con dispositivos Tractive y trackers genéricos con protocolo abierto HTTP push.

---

_PawTrack CR · Documento de Planes, Tiers y Características Comerciales · Actualizado Septiembre 2026_

---

_PawTrack CR · Documento de Planes y Precios · Agosto 2026_
