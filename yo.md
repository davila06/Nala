# PawTrack CR, explicado para mí

> **Guía personal de producto y ventas**  
> Corte de evidencia: 2 de octubre de 2026. Basada en el repositorio local, sus documentos canónicos y una lectura focal del código de roles y rutas. No es una aprobación comercial, fiscal ni legal; no acredita producción.

## En una frase

PawTrack CR es una PWA para dar identidad digital a una mascota y coordinar a su familia, personas que la encuentran, organizaciones e instituciones. Su producto principal es el ciclo **registrar → identificar con QR → reportar pérdida o avistamiento → coordinar → reunificar**. No es, ante todo, un fabricante de collares GPS, un expediente clínico certificado ni un marketplace que cobre por cada transacción. El alcance observado está descrito en [`docs/PRODUCT_SCOPE.md`](docs/PRODUCT_SCOPE.md) y la estrategia de posicionamiento, con sus límites, en [`docs/strategy/POSITIONING.md`](docs/strategy/POSITIONING.md).

### Mi explicación de 30 segundos

> “PawTrack ayuda a que una mascota tenga una identidad digital y a que la comunidad sepa cómo actuar si se pierde. El dueño crea el perfil y su QR; quien encuentra la mascota puede abrirlo o reportar un avistamiento; la familia coordina la búsqueda desde un caso compartido. Además, la plataforma conecta ese núcleo con salud, clínicas, refugios, servicios y municipalidades. La recuperación básica no debería depender de pagar.”

## Qué construí

En este repositorio hay una aplicación web progresiva para el producto, una API y una landing de marketing independiente. La solución está organizada en .NET 9 con API, Application, Domain e Infrastructure; la PWA usa React y TypeScript. La landing se exporta como sitio estático. El README describe el stack, pero para decidir qué está realmente implementado uso la auditoría reciente de alcance y no las tablas históricas del README o de documentos comerciales. Véanse [`README.md`](README.md), [`frontend/src/app/routes.tsx`](frontend/src/app/routes.tsx) y [`docs/PRODUCT_SCOPE.md`](docs/PRODUCT_SCOPE.md).

### El flujo central

1. El responsable registra la mascota y descarga su QR digital.
2. Cualquier persona puede consultar el perfil público mínimo o enviar un avistamiento; el acceso a información privada sigue reglas de autenticación y autorización.
3. El dueño abre un reporte de pérdida. El caso concentra estado, avistamientos, actividad y coordinación.
4. La red puede apoyar la búsqueda; hay chat enmascarado, difusión multicanal y código de entrega segura.
5. El dueño registra el resultado. No hay evidencia aquí para prometer un porcentaje de recuperación, cobertura nacional o tiempo de reunificación.

Fuentes: [`docs/NALA.md`](docs/NALA.md) para la explicación de producto, [`docs/PRODUCT_SCOPE.md`](docs/PRODUCT_SCOPE.md) para los estados observados y [`docs/API_AUTHORIZATION_MATRIX.md`](docs/API_AUTHORIZATION_MATRIX.md) para los límites de acceso. La vista pública del QR no equivale a una placa física ni revela automáticamente los datos privados del dueño.

### Alcance técnico, sin inflar la oferta

| Área                     | Qué existe en el repositorio                                                                                                                           | Qué no debo prometer                                                                           |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------- |
| Identidad y recuperación | QR digital, perfil, pérdida, avistamientos, coordinación y flujo de entrega; las capacidades están respaldadas en distinto nivel por código y pruebas. | Reunificación garantizada, cobertura o SLA. QR físico disponible para entrega.                 |
| Salud                    | Historial, recordatorios, permisos explícitos para compartir con clínicas y exportaciones según los gates aplicables.                                  | Diagnóstico, tratamiento autónomo, historia clínica externa o resultado veterinario.           |
| Clínicas y refugios      | Portales y flujos administrativos; el detalle depende del estado, permisos, plan y validaciones requeridos.                                            | Aprobación estatal o una clínica/refugio concreto operando sin validación.                     |
| GPS                      | Plataforma para registrar y gestionar collares, posiciones, historial y zonas.                                                                         | Hardware, cobertura nacional, batería validada, contrato, SLA o conectividad real verificados. |
| Adopciones y servicios   | Directorios, solicitudes, perfiles, disponibilidad y reservas según rol y gate.                                                                        | Pago liquidado, stock reservado, escrow, comisión o reembolso automático.                      |
| Matching visual          | Capacidad parcial condicionada por el servicio externo.                                                                                                | Agente autónomo, precisión garantizada, copiloto, diagnóstico o RAG.                           |
| Suscripciones            | Tiers, catálogo, activaciones y algunos gates; la comercialización y el enforcement no son uniformes.                                                  | Checkout/renovación universal o precio de producción sin comprobar entorno y aprobación.       |
| Institucional            | Capturas, reportes y alcance municipal delimitado.                                                                                                     | Integración oficial con SENASA u otra autoridad, ni reportes nacionales por defecto.           |

La matriz canónica marca GPS como implementado en la plataforma pero con hardware/proveedor no verificados; suscripciones, marketplace e IA como parciales; video/audio de telemedicina como no implementado. Véanse [`docs/PRODUCT_SCOPE.md`](docs/PRODUCT_SCOPE.md), [`docs/FEATURES.md`](docs/FEATURES.md) y [`docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md`](docs/auditoria/FEATURE_TRACEABILITY_MATRIX.md).

## Roles, perfiles y alcance

El enum [`UserRole`](backend/src/PawTrack.Domain/Auth/UserRole.cs) tiene **nueve roles**. Un rol responde “qué tipo de cuenta es”; el perfil de organización, la propiedad del recurso, el consentimiento, el estado de verificación y el plan determinan qué puede hacer concretamente. Tener una ruta visible no sustituye la autorización del backend. La cobertura de permisos está documentada como parcial, no como certificación exhaustiva, en [`docs/testing/NALA_ROLE_PERMISSION_MATRIX.md`](docs/testing/NALA_ROLE_PERMISSION_MATRIX.md) y [`docs/API_AUTHORIZATION_MATRIX.md`](docs/API_AUTHORIZATION_MATRIX.md).

| Actor / rol                 | Para quién y qué puede hacer                                                                                                                           | Límites que debo recordar                                                                                                                                   |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Visitante o finder, sin rol | Consultar superficies públicas, abrir QR y participar en flujos públicos de hallazgo/avistamiento permitidos.                                          | No accede a datos privados ni necesita que yo lo venda como suscriptor.                                                                                     |
| `Owner`                     | Persona responsable: administra sus mascotas, QR, reportes, salud, familia y solicitudes propias. Es el punto de entrada B2C.                          | Ownership se valida por recurso. Un plan no debe bloquear pasos esenciales de recuperación.                                                                 |
| Miembro de familia          | Colabora en una cuenta de hogar mediante `FamilyMemberRole` (`Owner` o `Member`); es un perfil dentro del ecosistema de dueño, no un rol global nuevo. | El catálogo local observa hasta cinco integrantes y 25 mascotas para UserFamilia; son límites técnicos/locales que debo confirmar antes de ofrecer.         |
| `Ally`                      | Organización aprobada que apoya alertas y acciones locales de búsqueda.                                                                                | El perfil `Shelter` habilita además flujos de adopción; no todo aliado es refugio. No hay que cobrar por participar en recuperación sin decisión explícita. |
| `Clinic`                    | Clínica veterinaria: identificación/escaneo, operaciones clínicas y acceso a información médica según autorización.                                    | Un QR o microchip no concede acceso médico. Lectura/escritura requiere grant del responsable. “SENASA-ready” no es integración ni aprobación oficial.       |
| Personal de clínica         | Subperfiles `Veterinarian`, `Receptionist`, `Assistant` o `ReadOnly`, con permisos de agenda, datos o tareas delimitados.                              | No son los roles globales `UserRole`; la clínica administra estos accesos y los grants del dueño siguen aplicando.                                          |
| `Municipality`              | Personal municipal autorizado para capturas, estados, reportes y flujos institucionales dentro de su organización/cantones.                            | Rol asignado administrativamente; alcance institucional y procurement deben verificarse.                                                                    |
| `Store`                     | Cuenta de tienda: perfil, catálogo y solicitudes de pedido, con estados gestionados por la tienda.                                                     | No es POS/ERP: no controla inventario, reserva stock, procesa pago ni liquida comisión.                                                                     |
| `ServiceProvider`           | Profesional/negocio de servicios para mascotas: perfil, servicios, disponibilidad y solicitudes de reserva.                                            | Membresía `Free/Verified/Featured` es técnica. No hay pricing recurrente, comisión ni payout aprobado.                                                      |
| `Support`                   | Personal interno con acceso acotado a casos de bienestar e incidentes de proveedores.                                                                  | Admin lo asigna; no tiene registro público ni portal propio. No sustituye a Admin.                                                                          |
| `Admin`                     | Operación interna: revisiones, planes, organizaciones, campañas, soporte, inventario y funciones administrativas autorizadas.                          | Privilegio interno; no es un segmento de clientes. Las operaciones sensibles deben auditarse.                                                               |
| `SuperAdmin`                | Rol interno de privilegio excepcional; hereda parte del acceso Admin y gestiona privilegios con step-up/MFA según política.                            | No es otro producto ni usuario comercial. Protegerlo con controles y acceso mínimo.                                                                         |

Fuentes de perfiles: [`docs/Manuales/MANUAL_USUARIO.md`](docs/Manuales/MANUAL_USUARIO.md), [`MANUAL_ALIADOS.md`](docs/Manuales/MANUAL_ALIADOS.md), [`MANUAL_CLINICAS.md`](docs/Manuales/MANUAL_CLINICAS.md), [`MANUAL_MUNICIPALIDADES.md`](docs/Manuales/MANUAL_MUNICIPALIDADES.md), [`MANUAL_TIENDAS.md`](docs/Manuales/MANUAL_TIENDAS.md), [`MANUAL_PROVEEDORES.md`](docs/Manuales/MANUAL_PROVEEDORES.md), [`MANUAL_SOPORTE.md`](docs/Manuales/MANUAL_SOPORTE.md) y [`MANUAL_ADMINISTRADOR.md`](docs/Manuales/MANUAL_ADMINISTRADOR.md). Las pantallas protegidas se pueden contrastar en [`RoleGuard.tsx`](frontend/src/app/layout/RoleGuard.tsx) y [`routes.tsx`](frontend/src/app/routes.tsx).

## Cómo monetizarlo

### Regla de producto

Mantener gratis el perfil/QR digital y los pasos esenciales de búsqueda y recuperación. Cobrar por capacidad, automatización o workflow profesional adicional cuando el límite exista en backend, se entienda antes de comprar y tenga aprobación, soporte y economía unitaria. No usar la urgencia de una mascota perdida como palanca de venta. Esta regla coincide con [`docs/PRICING_AND_PLANS.md`](docs/PRICING_AND_PLANS.md) y la propuesta de monetización en [`docs/NALA_ROADMAP_MONETIZATION.md`](docs/NALA_ROADMAP_MONETIZATION.md).

### Lo observado en el catálogo local

`PawTrackDev` registró estos importes; **no los trato como precios publicados ni como prueba de ventas**. Producción no fue consultada en los documentos revisados.

| Segmento / tier                        |                     Importe observado | Posible oferta, sujeta a validación                                                                               |
| -------------------------------------- | ------------------------------------: | ----------------------------------------------------------------------------------------------------------------- |
| Dueño `UserPlus`                       |                            ₡3.000/mes | Más capacidad y funciones de conveniencia según gates efectivos.                                                  |
| Hogar `UserFamilia`                    |                            ₡4.990/mes | Colaboración familiar, capacidad e historial sujeto a gates; máximo técnico local de 25 mascotas, no “ilimitado”. |
| Clínica `ClinicPlus` / `ClinicPartner` |              ₡15.000 / ₡35.000 al mes | Visibilidad y workflows clínicos; Partner requiere grants, verificación y permisos para funciones avanzadas.      |
| Tienda `StorePlus` / `StorePartner`    |              ₡12.000 / ₡25.000 al mes | Catálogo/solicitudes y algunas capacidades técnicas; no venderlo como checkout o multi-sede lista.                |
| Refugio `ShelterPlus`                  |                            ₡8.000/mes | Publicación avanzada y ferias; evaluar primero patrocinio o subsidio para proteger adopciones.                    |
| Municipalidad                          | ₡150.000 / ₡300.000 / ₡500.000 al año | Tiers modelados; venta, compra y renovación institucional permanecen incompletas.                                 |

Fuente de importes y restricciones: [`docs/NALA_PLAN_MAPPING.md`](docs/NALA_PLAN_MAPPING.md). La política consolidada [`docs/PRICING_AND_PLANS.md`](docs/PRICING_AND_PLANS.md) documenta una atestación administrativa local del 1-oct-2026, pero también aclara que no valida automáticamente lo fiscal/legal ni demuestra despliegue en producción. Hay documentación de precios que aún califica importes como hipótesis y pide aprobación antes de publicar: [`docs/NALA_CR_PRICING.md`](docs/NALA_CR_PRICING.md). **Antes de cotizar, comprobar el catálogo y los gates en el entorno real y resolver esa diferencia documental.**

### Oportunidades por cliente

| A quién                                           | Qué problema le vendo                                                                               | Oferta para probar                                                                                       | Estado / condición                                                                                                                                                     |
| ------------------------------------------------- | --------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Familias con mascotas                             | Identidad digital y una manera ordenada de coordinar si la mascota se pierde.                       | QR y recuperación base gratis; probar upgrade Plus/Familia por capacidad, historial y hogar compartido.  | Precios locales observados; validar aprobación, límites, IVA, conversión y renovación. No cobrar por recuperar.                                                        |
| Clínicas independientes                           | Identificar una mascota, notificar al dueño y gestionar información clínica compartida con permiso. | Piloto por clínica/sede: flujo de escaneo + grant + tarea clínica; luego cotizar tier según uso/soporte. | Mejor candidato de piloto B2B del producto, no ingreso comprobado. Acordar alcance y revisar licencias/claims.                                                         |
| Refugios y organizaciones                         | Alertas de casos en su zona y administración de adopciones cuando el perfil aplica.                 | Acceso base y ShelterPlus patrocinado por marcas/municipio, sujeto a medición y aprobación.              | No poner adopción/participación esencial detrás de un pago sin comprobar sostenibilidad y equidad.                                                                     |
| Municipalidades                                   | Registrar capturas y dar seguimiento institucional con reportes de su ámbito.                       | Piloto limitado a una institución/cantón, con alcance, onboarding, privacidad y soporte cotizados.       | Ciclo de compra y renovación no completos; no prometer interoperabilidad oficial ni SLA.                                                                               |
| Tiendas de mascotas                               | Ser encontrables y recibir solicitudes de productos.                                                | Empezar con presencia/directorio o una campaña patrocinada validada.                                     | PawTrack no vende ni cobra por el pedido; no cobrar comisión ni prometer stock o pago integrado.                                                                       |
| Groomers, paseadores, hoteles y otros prestadores | Publicar servicios y recibir solicitudes/reservas.                                                  | Alta de proveedor y piloto para medir contactos/reservas atribuibles.                                    | No cobrar take-rate, payout ni membresía recurrente hasta contar con política, contrato y medición.                                                                    |
| Marcas y negocios pet-friendly                    | Llegar a usuarios relevantes dentro de la app.                                                      | Patrocinio/campaña manual de prueba con entregables acotados.                                            | El producto tiene placements; las tarifas de [`docs/publicidad.md`](docs/publicidad.md) son propuesta y no hay checkout, ledger ni reportes formales para anunciantes. |

### Quién compra, quién usa y cuál es el momento

| Segmento           | Comprador económico                             | Usuario diario                                  | Necesidad / señal para conversar                                                 | Primera oferta que probaría                                                           | Califico con                                                             |
| ------------------ | ----------------------------------------------- | ----------------------------------------------- | -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Dueño/hogar        | Responsable de la mascota                       | Responsable y cuidadores invitados              | Varias mascotas, hogar compartido o necesidad de organizar datos y recordatorios | Demo gratuita del ciclo QR/recuperación; evaluar upgrade solo por necesidad adicional | Mascotas, cuidadores, funciones requeridas, uso y sensibilidad al precio |
| Clínica            | Propietario, administrador o director clínico   | Veterinario, recepción y asistentes autorizados | Identificación de pacientes y datos clínicos compartidos con consentimiento      | Piloto acotado de escaneo, grant y registro; cotizar tras medir soporte               | Escaneos aproximados, roles, flujo actual, datos, permisos y decisor     |
| Refugio/ONG        | Dirección, junta o patrocinador                 | Equipo de rescate/adopciones                    | Alertas territoriales, publicaciones y solicitudes de adopción                   | Acceso base y explorar patrocinio de capacidades avanzadas                            | Tipo de entidad, animales activos, carga de trabajo y sponsor posible    |
| Municipalidad      | Jefatura, tecnología o autoridad presupuestaria | Personal de campo y analistas                   | Capturas y reportes dentro de su ámbito                                          | Discovery y piloto limitado; no contrato anual sin procurement                        | Cantones, volumen, datos, compras, seguridad e integraciones             |
| Tienda             | Dueño o administrador                           | Personal que publica y responde pedidos         | Exposición local y solicitudes                                                   | Perfil/directorio o campaña patrocinada validada                                      | Sede, catálogo, atención, stock, pago y entrega propios                  |
| Proveedor          | Profesional o negocio                           | Profesional que presta el servicio              | Contactos y reservas organizadas                                                 | Piloto para medir solicitudes/reservas                                                | Categoría, horarios, capacidad, territorio y cancelaciones               |
| Sponsor/anunciante | Marca pet-friendly, clínica o comercio          | Responsable de marketing                        | Visibilidad contextual o apoyo a rescate/adopción                                | Campaña manual con placement, fechas y entregables acordados                          | Objetivo, creatividad, presupuesto y métrica reportable                  |

Quien decide el presupuesto no siempre usa el portal. Preguntar ambas cosas antes de preparar una propuesta. Para instituciones, identificar además la autoridad de compra y el custodio de datos.

### Catálogo de ofertas y estado comercial

Esta guía es para conversación y planificación, no un catálogo habilitado para cobro. “Demo” no acredita disponibilidad en producción.

| Oferta                        | Incluye / valor                                     | Excluye                                                 | Estado y requisito para cotizar                                                                          |
| ----------------------------- | --------------------------------------------------- | ------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Identidad y recuperación base | Perfil digital, QR y flujos de búsqueda             | Rescate garantizado o placa física                      | Mostrar en entorno permitido; no cobrar para acceder a recuperación esencial                             |
| Plan dueño Plus/Familia       | Capacidad y funciones según gates                   | GPS físico automático o “ilimitado”                     | Importes locales ₡3.000/₡4.990 observados; confirmar catálogo, aprobación, IVA, pago y gates del entorno |
| Clínica Plus/Partner          | Visibilidad y workflows clínicos autorizados        | Video, multi-sede operativa, aval estatal, soporte 24/7 | Precios locales observados ₡15.000/₡35.000; contrato, grants, alcance, soporte y producción por validar  |
| ShelterPlus patrocinado       | Adopciones/ferias según perfil y gates              | Resultado de adopción garantizado o cargo al adoptante  | Precio local ₡8.000/mes; sponsor y condiciones requieren aprobación                                      |
| Tiers Store                   | Catálogo y solicitudes según gates                  | POS, stock reservado, checkout, payout o comisión       | Precios locales ₡12.000/₡25.000; comprobar alcance parcial antes de ofrecer                              |
| Oferta municipal              | Capturas/reportes con alcance institucional         | Integración oficial, alcance nacional, SLA informal     | Tiers/precios locales modelados; procurement, contrato, costos y renovación incompletos                  |
| Perfil/reserva de proveedor   | Directorio, servicios, disponibilidad y solicitudes | Comisión, settlement, payout o refund automatizados     | Sin tarifa recurrente aprobada; medir leads/reservas primero                                             |
| Valla/patrocinio              | Placement, creatividad y ventana acordados          | Checkout o métricas de entrega garantizadas             | Tarifas de `docs/publicidad.md` son propuesta; aprobar precio y registrar manualmente la entrega         |
| Collar GPS                    | Posible hardware separado                           | Stock, cobertura, garantía o costo cerrado              | AL600 ₡35.000 sugerido / ₡37.000 con QR; costo estimado, cotización OEM del lote pendiente               |

Fuentes: [`docs/NALA_PLAN_MAPPING.md`](docs/NALA_PLAN_MAPPING.md), [`docs/PRICING_AND_PLANS.md`](docs/PRICING_AND_PLANS.md), [`docs/NALA_CR_PRICING.md`](docs/NALA_CR_PRICING.md), [`docs/publicidad.md`](docs/publicidad.md) y [`docs/jimiiot.md`](docs/jimiiot.md). No copiar importes en una propuesta externa sin revalidar vigencia y aprobación.

### Qué vender primero

1. **Primero, una demostración de recuperación** a dueños y organizaciones: registrar mascota, abrir QR, reportar pérdida, enviar avistamiento y cerrar el caso. El núcleo gratis genera la red que hace valioso al resto.
2. **En paralelo, validar un piloto clínico acotado** con una clínica: medir escaneos útiles, tareas semanales, grants concedidos, tiempo ahorrado y soporte requerido. Después se propone un precio por sede, no antes.
3. **Probar patrocinio local** para financiar participación de refugios o visibilidad de campañas, con fechas, placement y reporte definidos. No usar tarifas del borrador como lista vigente.
4. **Abrir discovery municipal** con una entidad y un caso operativo concreto. Preparar cotización solo después de delimitar datos, integración, adquisición pública, soporte y renovación.
5. **Dejar marketplace con comisiones y GPS físico para una fase posterior**: faltan pagos/liquidación o cotización, inventario, contratos, cobertura, garantía, fulfillment y costos completos.

### Demo comercial de 7 minutos

1. Registrar una mascota de prueba y enseñar el perfil/QR público, cuidando que no exponga datos privados.
2. Reportarla como perdida y mostrar la sala del caso, no una promesa de rescate.
3. Enviar un avistamiento y enseñar cómo se agrega al caso.
4. Mostrar chat enmascarado, coordinación o difusión disponible en el entorno demo.
5. Cerrar con el flujo de reunificación y explicar qué datos/métricas se podrían medir en un piloto.
6. Para clínica, repetir con escaneo y consentimiento explícito antes de leer/escribir historial. Para municipio, enseñar alcance de capturas y reportes de su ámbito.

Preguntas de discovery: ¿cómo identifican hoy una mascota?, ¿qué ocurre cuando llega una perdida?, ¿quién mantiene los datos?, ¿cuántas veces por semana ejecutarían el flujo?, ¿qué evidencia justificaría renovar?, ¿qué sistemas y obligaciones deben integrarse? No usar cifras de mercado, recuperación o engagement que no estén medidas.

### Playbook por segmento: abrir, descubrir, demostrar, acordar

Guía de conversación propuesta; no es un guion aprobado ni evidencia de demanda.

| Segmento      | Apertura honesta                                                                          | Discovery                                                                             | Demo relevante                                                  | Siguiente paso                                                                                                        |
| ------------- | ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- | --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Dueño/hogar   | “Veamos si el perfil y el QR ayudan a identificar a tu mascota y coordinar una búsqueda.” | ¿Cuántas mascotas y cuidadores? ¿Qué te cuesta organizar? ¿Qué debería seguir gratis? | Perfil/QR, pérdida, avistamiento, Case Room y handover          | Crear perfil de prueba; valorar un plan solo si resuelve una necesidad adicional y sus gates/precio están confirmados |
| Clínica       | “Revisemos cómo identificar y compartir el historial solo con autorización.”              | ¿Quién escanea y registra? ¿Qué volumen, roles, datos y sistemas participan?          | QR/microchip → notificación → grant read/write → acceso clínico | Acordar responsable, línea base, alcance, soporte y piloto antes de cotizar                                           |
| Refugio/ONG   | “Veamos cómo publican animales y revisan solicitudes hoy.”                                | ¿Quién atiende casos/adopciones? ¿Cuántos animales? ¿Hay sponsor?                     | Perfil Shelter, publicación, solicitud y revisión               | Validar perfil/capacidad y explorar subsidio; no cobrar al adoptante por contactar                                    |
| Municipalidad | “Primero delimitamos proceso, cantones, datos y responsables.”                            | ¿Qué capturan? ¿Quién autoriza? ¿Qué ruta de compra y seguridad aplica?               | Captura, estado y reporte según el ámbito autorizado            | Minuta de discovery y procurement antes de presentar una oferta                                                       |
| Tienda        | “PawTrack puede comunicar solicitudes; tu equipo confirma stock, pago y entrega.”         | ¿Quién responde? ¿Qué catálogo/stock tiene? ¿Cómo completa la venta?                  | Perfil, catálogo y solicitud de pedido                          | Piloto de publicación y respuesta; no ofrecer inventario o pago integrado                                             |
| Proveedor     | “Probemos si tu perfil genera solicitudes que sí puedes atender.”                         | ¿Qué servicios, horarios, territorio y capacidad ofrece?                              | Perfil, servicio, disponibilidad y reserva solicitada           | Medir contactos/reservas; sin comisión hasta aprobar política y contrato                                              |
| Sponsor       | “Podemos acordar una campaña manual con placement y periodo definidos.”                   | ¿Cuál objetivo, audiencia, creatividad y métrica necesita?                            | Placement disponible con ejemplo, no como impresión garantizada | Especificación breve con tarifa aprobada y registro de entrega                                                        |

#### Objeciones y respuestas basadas en evidencia

| Objeción                                         | Respuesta                                                                                                                                        |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| “¿Garantizan que encontrarán la mascota?”        | No. PawTrack organiza información y coordinación; no controla la respuesta de la comunidad ni garantiza reunificación.                           |
| “¿El GPS funciona en todo Costa Rica?”           | No puedo confirmarlo. Hardware, operador, batería, cobertura y SLA requieren validación.                                                         |
| “¿La clínica ve el historial con solo escanear?” | No. El escaneo identifica; lectura/escritura necesita un grant activo y permiso del responsable.                                                 |
| “¿Puedo cobrar reservas dentro de PawTrack?”     | No como liquidación integral. En tienda el pago se acuerda con el negocio; el flujo del proveedor no acredita payout automático.                 |
| “¿El precio incluye IVA y ya está aprobado?”     | Solo después de confirmar catálogo del entorno, aprobación comercial y tratamiento fiscal. Los importes de esta guía son referencias/propuestas. |
| “¿Está integrado o aprobado por SENASA?”         | No hay evidencia de integración ni aprobación oficial. “SENASA-ready” no significa aval estatal.                                                 |
| “¿Qué resultados tienen?”                        | Presentar solo métricas obtenidas, con definición, periodo y fuente. No afirmar tasa de recuperación, CAC, churn o MRR/ARR sin medición.         |

**Proceso sugerido:** calificar necesidad/decisor → demo permitida → alcance y exclusiones por escrito → aprobar precio, privacidad y soporte → piloto → revisar evidencia/costos → decidir continuidad.

## Precios de collares: no confundir costo y venta

La guía histórica del collar propone **₡35.000** de venta del AL600 y **₡37.000** con QR láser. Estima costo landed del primer año de **~US$51,50/unidad** (aprox. ₡26.800 a ₡520/USD), pero el fabricante confirmó precio de muestra de US$32 y dejó pendiente la cotización final del lote. Flete, aranceles, renovación de datos, garantía y costo del QR requieren validación. Esto es una hipótesis comercial, no un precio unitario aprobado ni costo de compra cerrado. Fuentes: [`docs/collarFinal.md`](docs/collarFinal.md), [`docs/jimiiot.md`](docs/jimiiot.md) y la regla general de precios en [`docs/NALA_CR_PRICING.md`](docs/NALA_CR_PRICING.md).

## Ficha de piloto y economía unitaria

### Plantilla de piloto

Completar una ficha por organización antes de iniciar. Duración, precio y alcance se acuerdan caso por caso.

| Campo                     | Completar antes de iniciar                                                                         |
| ------------------------- | -------------------------------------------------------------------------------------------------- |
| Organización y decisores  | Nombre, comprador económico, operador diario y responsable de datos                                |
| Problema / proceso actual | Tarea, frecuencia, alternativa usada hoy y línea base                                              |
| Hipótesis                 | Qué cambio se quiere evaluar; no escribirlo como resultado ya logrado                              |
| Alcance / exclusiones     | Roles, sedes/cantones, usuarios, flujos, datos incluidos y expresamente excluidos                  |
| Precio / financiamiento   | `PENDIENTE`: importe, moneda, periodo, IVA, sponsor/gratuidad y aprobación                         |
| Privacidad / soporte      | Consentimientos/grants, acceso, retención, contacto de incidentes, capacitación y horario acordado |
| Indicadores / fuente      | Definición, fuente, periodo, responsable y valor previo                                            |
| Éxito / pausa / salida    | Umbrales acordados antes de iniciar, criterios de seguridad y cierre/exportación                   |
| Revisión final            | Fecha y decisión: cerrar, extender o cotizar; aprendizajes y aprobaciones pendientes               |

Indicadores candidatos, no resultados actuales: QR activado/escaneado, tiempo al primer avistamiento, casos coordinados, tareas clínicas por semana, tiempo por tarea, solicitudes atendidas, costo de soporte y renovación. No incluir PII, expedientes clínicos ni coordenadas exactas en material comercial salvo necesidad, autorización y canal adecuado.

### Hoja de economía por cliente

No hay costos unitarios completos ni ingresos realizados verificados en las fuentes revisadas. Completar con facturas, contratos y datos operativos. Separar ingreso neto de impuestos cobrados por cuenta del fisco.

| Variable                                  | Valor       | Evidencia requerida                          |
| ----------------------------------------- | ----------- | -------------------------------------------- |
| Ingreso neto cobrado por plan/campaña     | `PENDIENTE` | Pago conciliado, sin IVA recaudado           |
| Comisión de pago / pasarela               | `PENDIENTE` | Extracto y contrato del proveedor            |
| Cloud / servicios externos                | `PENDIENTE` | Factura y consumo atribuible por cuenta/uso  |
| Email, push, Vision, GPS o conectividad   | `PENDIENTE` | Factura/proveedor por evento o dispositivo   |
| Onboarding, capacitación y soporte        | `PENDIENTE` | Horas registradas por rol y costo cargado    |
| Fulfillment, envío, devolución y garantía | `PENDIENTE` | Orden y documentos del proveedor             |
| Descuentos, refund e impagos              | `PENDIENTE` | Conciliación real                            |
| CAC por canal                             | `PENDIENTE` | Gasto atribuible / clientes nuevos pagadores |
| Retención, churn y renovación             | `PENDIENTE` | Cohortes y fechas reales de pago/cancelación |

- **Contribución por cliente** = ingreso neto cobrado − entrega variable − comisión de pago − soporte/onboarding atribuible − devoluciones observadas − CAC atribuible.
- **Margen de contribución (%)** = contribución por cliente / ingreso neto cobrado.
- **Clientes para cubrir costos fijos** = costos fijos del periodo / contribución por cliente, solo si la contribución es positiva.

Para planes recurrentes separar ingreso único/recurrente, costo mensual de servir, renovación efectiva y cohortes. No tratar registro de plan, pedido o click como pago cobrado.

## Glosario rápido

| Término                  | Significado simple                                               | No confundir con                                              |
| ------------------------ | ---------------------------------------------------------------- | ------------------------------------------------------------- |
| PawTrack CR              | Aplicación de producto para identidad y coordinación de mascotas | Autoridad pública o fabricante de GPS                         |
| NALA                     | Nombre interno/superficie de reportes agregados                  | `UserRole` o agente autónomo                                  |
| PWA                      | Aplicación web progresiva que funciona desde navegador           | App nativa u operación offline completa                       |
| QR / perfil público      | Código que abre la ficha pública mínima                          | Placa física o permiso clínico                                |
| Case Room                | Sala de operaciones de un reporte de pérdida                     | Garantía de rescate                                           |
| Finder                   | Persona que encuentra o reporta un avistamiento                  | Necesariamente una cuenta registrada                          |
| Ally                     | Cuenta organizacional aprobada para colaborar                    | Todo Ally es refugio o comprador                              |
| Grant clínico            | Permiso revocable para leer/escribir datos seleccionados         | El escaneo de QR/microchip                                    |
| Rol / tier / entitlement | Tipo de cuenta / plan / capacidad o límite                       | Conceptos intercambiables o permisos automáticos en toda ruta |
| “SENASA-ready”           | Preparación documental/técnica condicionada                      | Certificación, integración o aprobación oficial               |
| Marketplace              | Directorios, catálogos, solicitudes y reservas parciales         | Checkout, inventario, payout o comisión automática            |
| AL600 / TrackSolid       | Dispositivo/plataforma considerados para la ruta GPS             | Hardware, cobertura o costo del lote ya validados             |

Si un término comercial no coincide con código o contrato, describir el comportamiento concreto y enlazar la fuente en vez de inventar otra categoría.

## Qué no debo vender como si ya estuviera resuelto

- **GPS nacional listo**: plataforma sí; hardware, cobertura, supply, soporte y SLA no verificados.
- **Telemedicina por video**: declarada no implementada.
- **Diagnóstico o IA veterinaria**: no existe como oferta respaldada.
- **SENASA integrado/aprobado**: “SENASA-ready” describe preparación técnica/documental, no autorización oficial.
- **Marketplace con checkout o comisión**: no hay liquidación, payout, inventario transaccional ni comisión activa.
- **Resultados garantizados**: no prometer tasas de recuperación, rescate en determinado tiempo, disponibilidad 24/7 ni cobertura territorial sin datos y contrato.
- **Enterprise multi-tenant/SLA**: no existe un tier Enterprise aprobado; la estrategia lo condiciona a trabajo futuro.

## Cómo explico el valor sin exagerar

**Posicionamiento para probar:** “Identidad y red local para ayudar a cuidar y recuperar mascotas”. El valor diferencial es conectar el QR/perfil con coordinación comunitaria e institucional local; no competir como fabricante de GPS ni como sistema clínico integral. Es una hipótesis de posicionamiento, no un claim de impacto aprobado. Fuente: [`docs/strategy/POSITIONING.md`](docs/strategy/POSITIONING.md).

Indicadores que puedo instrumentar o proponer para un piloto (no son resultados actuales): activaciones/escaneos QR, tiempo al primer avistamiento, porcentaje de casos con coordinación, reunificaciones registradas, uso semanal por clínica, solicitudes/reservas completadas, costo de soporte y renovación. El funnel técnico no equivale a ventas, uso medido ni impacto causal.

## Riesgos de documentación y de operación

- Hay documentos de visión/venta más antiguos que presentan GPS, bundles, IVA, cobros y módulos como más completos que la auditoría actual. Ante una diferencia, usar [`docs/PRODUCT_SCOPE.md`](docs/PRODUCT_SCOPE.md), [`docs/NALA_PLAN_MAPPING.md`](docs/NALA_PLAN_MAPPING.md) y la evidencia de código; marcar lo demás como histórico o propuesta.
- Los docs de pricing no coinciden totalmente sobre qué aprobación local existe. La atestación local tampoco acredita producción, revisión tributaria ni ventas; verificar antes de publicar o cobrar.
- Los permisos combinan rol, ownership, estado, tenant, grants y plan; la matriz de pruebas de roles sigue parcial. No entregar datos clínicos o ubicación por un simple QR.
- El reporte de roles registra 1.820 pruebas backend aprobadas al corte 28-sep-2026 y huecos de cobertura. En esta tarea `dotnet test PawTrack.sln --no-build --verbosity minimal` aprobó 1.862/1.862 pruebas usando los binarios existentes. El intento con build completo no pudo copiar DLLs bloqueadas por el host API local (`MSB3021/MSB3027`), así que no hay una compilación fresca completa de esta revisión. No inspeccioné producción. Véase [`docs/testing/NALA_USER_ROLE_VALIDATION_REPORT.md`](docs/testing/NALA_USER_ROLE_VALIDATION_REPORT.md).
- No encontré evidencia documental de MRR/ARR, ventas cobradas, churn, CAC, margen realizado o disposición a pagar. No inventar proyecciones. Véase [`docs/strategy/REVENUE_MODEL.md`](docs/strategy/REVENUE_MODEL.md).

## Próximos pasos para volverlo vendible

1. Confirmar en el entorno que se va a vender catálogo, precio, duración, IVA, gates y flujo real de pago; resolver contradicciones y obtener aprobación fiscal/comercial.
2. Elegir un único piloto clínico y uno de red comunitaria; redactar alcance, privacidad, soporte, exclusiones, precio/financiamiento y criterios de éxito.
3. Verificar contratos y operación externa de WhatsApp, correo, GPS, Azure y cobros antes de anunciarlos como disponibles.
4. Medir activación y valor realizado con datos mínimos; revisar seguridad, consentimiento y costos por uso.
5. Publicar solo los claims, tarifas y compromisos que sobrevivan a esas verificaciones.

## Mapa de fuentes

- Alcance real y limitaciones: [`docs/PRODUCT_SCOPE.md`](docs/PRODUCT_SCOPE.md), [`docs/FEATURES.md`](docs/FEATURES.md), [`docs/KNOWN_LIMITATIONS.md`](docs/KNOWN_LIMITATIONS.md).
- Planes e importes locales: [`docs/NALA_PLAN_MAPPING.md`](docs/NALA_PLAN_MAPPING.md), [`docs/PRICING_AND_PLANS.md`](docs/PRICING_AND_PLANS.md), [`docs/NALA_CR_PRICING.md`](docs/NALA_CR_PRICING.md), [`docs/NALA_PRICING_EXECUTIVE_SUMMARY.md`](docs/NALA_PRICING_EXECUTIVE_SUMMARY.md).
- Roles y seguridad: [`backend/src/PawTrack.Domain/Auth/UserRole.cs`](backend/src/PawTrack.Domain/Auth/UserRole.cs), [`frontend/src/app/routes.tsx`](frontend/src/app/routes.tsx), [`docs/API_AUTHORIZATION_MATRIX.md`](docs/API_AUTHORIZATION_MATRIX.md), [`docs/testing/NALA_ROLE_PERMISSION_MATRIX.md`](docs/testing/NALA_ROLE_PERMISSION_MATRIX.md).
- Venta, mercado y monetización: [`docs/strategy/POSITIONING.md`](docs/strategy/POSITIONING.md), [`docs/strategy/REVENUE_MODEL.md`](docs/strategy/REVENUE_MODEL.md), [`docs/NALA_ROADMAP_MONETIZATION.md`](docs/NALA_ROADMAP_MONETIZATION.md), [`docs/publicidad.md`](docs/publicidad.md), [`docs/collarFinal.md`](docs/collarFinal.md), [`docs/jimiiot.md`](docs/jimiiot.md).
- Manuales por audiencia: carpeta [`docs/Manuales/`](docs/Manuales/).

---

**Versión HTML:** [yo.html](yo.html).  
**Regla personal:** vender lo que puedo demostrar, cobrar lo que está aprobado y medir lo que todavía es hipótesis.
