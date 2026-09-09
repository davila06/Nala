# NALA / PawTrack CR — Análisis financiero, precios y estrategia de ganancias

> Versión: 2026-09-09  
> Objetivo: evaluar si los precios, planes, perfiles y estructura de negocio actual son correctos desde la perspectiva de marketing, administración empresarial y escalabilidad financiera.  
> Fuente técnica: `SubscriptionPricing`, `SubscriptionTier`, documentación interna y estado funcional actual del producto.

---

## 1. Resumen ejecutivo

Mi conclusión es clara: el producto tiene una base muy buena, pero aún no ha llegado a un punto de madurez comercial donde los precios y el portfolio de planes puedan considerarse definitivos.

Lo que sí está bien:

- La estructura de segmentos es lógica: B2C, B2B, B2G y marketplace de servicios.
- La capa B2C tiene un diseño sano para conversión: una versión gratuita, una de entrada y una premium familiar.
- Los planes B2B tienen una lógica comercial clara y el precio actual está alineado con el tipo de valor operativo que entregan.
- El producto tiene un punto de diferenciación real, no es una copia de un directorio genérico.

Lo que aún requiere ajuste:

- El marketplace de proveedores no debería venderse todavía como un negocio recurrente con precios firmes sin haber validado demanda real.
- La estrategia de monetización debe dejar claro qué se vende: acceso, visibilidad, reservas, gestión, seguros, comisiones, soporte o infraestructura.
- La operación debe ir de la mano de la estrategia de adquisición; si no hay densidad de usuarios y proveedores, el modelo no escala.

En pocas palabras: la estructura actual es buena para lanzar, pero no es todavía una estrategia de rentabilidad madura. El negocio necesita decisiones más agresivas en tres frentes: simplificación del valor, validación de demanda y monetización del marketplace.

---

## 2. Mi evaluación de la estructura actual

### 2.1 Segmento B2C: bien pensado, pero con margen de mejora

Los planes actuales de dueños son razonables:

- `Free`: funciona como puerta de entrada y prueba de valor.
- `UserPlus` en ₡2.990/mes: es un precio barato, muy accesible y bastante competitivo para una necesidad de emergencia.
- `UserFamilia` en ₡4.990/mes: se percibe como un plan de continuidad, no solo de cantidad de mascotas.

Esto es positivo porque:

- baja la fricción de compra;
- conecta con la necesidad emocional del usuario;
- crea una transición lógica de gratis a pago;
- permite vender valor en recuperación y no solo en almacenamiento de datos.

Sin embargo, hay que mejorar la comunicación del valor del plan. Hoy el usuario probablemente siente que "el gratis ya es suficiente" hasta que ocurre un problema real. El negocio no vende tecnología; vende tranquilidad, recuperación y control.

Recomendación:

- El mensaje principal debe ser: "Te ayudamos a recuperar a tu mascota antes, con menos caos y con más control."
- No vender solamente funciones; vender resultados.
- En la estrategia comercial, `UserPlus` debe presentarse como la protección que activa la red de búsqueda, notificaciones y coordinación.
- `UserFamilia` debe venderse como "familia + salud + continuidad" y no como "más mascotas".

### 2.2 Segmento B2B: clínicas, tiendas y refugios tienen lógica comercial clara

#### Clínicas

`ClinicPlus` y `ClinicPartner` tienen una lógica muy sólida:

- `ClinicPlus` a ₡15.000/mes: sí parece viable para una clínica con flujo constante y valor de visibilidad local.
- `ClinicPartner` a ₡35.000/mes: sí parece razonable si incluye certificados, API, widgets y funciones institucionales.

Esto tiene sentido porque la clínica compra:

- visibilidad;
- credibilidad;
- eficiencia operativa;
- trazabilidad sanitaria;
- reputación local.

La clave es que este segmento debe venderse con ROI operativo y no con marketing puro. El pitch correcto es: "más búsquedas, más pacientes, más confianza, mejor coordinación con el dueño y más trazabilidad."

#### Tiendas

`StorePlus` a ₡12.000/mes y `StorePartner` a ₡25.000/mes son números razonables en principio, sobre todo si la tienda vende productos, servicios, accesorios, medicamentos o artículos de cuidado.

El problema no es el precio, sino la forma en que se recibe el valor. Si una tienda no tiene demanda del marketplace ni tráfico, no va a percibir valor. Debe venderse como:

- canal de ventas local,
- mayor visibilidad,
- compra in-app,
- atención local durante emergencias,
- relación con dueños de mascotas.

#### Refugios

`ShelterPlus` a ₡8.000/mes es un precio razonable y bastante accesible para una organización con campañas, ferias y publicaciones frecuentes.

Tiene sentido si el refugio necesita:

- más visibilidad,
- más adopciones,
- eventos y ferias,
- gestión de solicitudes,
- mejor posicionamiento en el mapa.

### 2.3 Municipalidades y B2G: buena idea, pero aún no está totalmente monetizado

Los planes anuales en municipalidades son coherentes con la naturaleza institucional del cliente:

- `MuniBasica` ₡150.000/año
- `MuniFull` ₡300.000/año
- `MuniRedRegional` ₡500.000/año

Esto tiene lógica si el producto ayuda a:

- gestionar animales en situación de calle;
- coordinar reportes y adopciones;
- reducir trabajo operativo manual;
- ofrecer reportes y trazabilidad.

El principal riesgo es que el valor institucional no siempre se percibe como ahorro directo. Por eso hay que venderlo como:

- mejor coordinación,
- menos duplicación de trabajo,
- trazabilidad documental,
- servicio a la comunidad,
- mayor capacidad de respuesta.

No como un sistema "bonito"; como un sistema operativo para gestión animal.

### 2.4 Marketplace de proveedores: aún no está listo para cobrar con confianza

Aquí es donde más me detengo.

El producto de proveedores está muy bien pensado técnicamente, pero comercialmente aún no está validado. Tiene un flujo de demostración, perfiles, capacidades, disponibilidad y pruebas, pero no hay todavía una prueba convincente de willingness to pay.

Esto significa:

- no es prudente anunciar precios definitivos para `Verified` o `Featured` en este momento;
- hay que usar un periodo de prueba muy claro;
- hay que medir activación, reserva, retención y pago real;
- si se quiere monetizar, conviene empezar con un modelo muy simple y con poca fricción.

Mi recomendación fuerte: mantener `Free` + verificación + prueba de 30 días, y solo después de medir demanda, decidir pricing. No poner una tarifa recurrente sin validación de mercado.

---

## 3. Qué está bien y qué no

### Lo que está bien

1. Estructura de ofertas clara
   - Gratis + pagado para usuarios finales.
   - B2B con lógica por segmento.
   - B2G con modelo anual.
2. Precios accesibles en B2C
   - ₡2.990 y ₡4.990 son muy razonables para un servicio emocional y de emergencia.
3. Diferenciación real
   - NALA no es solo un directorio; la capa de QR, coordinación, alertas, rescate y expediente añade valor real.
4. El mayor atractivo está en la obra real
   - No se está vendiendo un producto abstracto; se está vendiendo infraestructura de recuperación y confianza.

### Lo que necesita ajuste

1. Falta claridad de pricing para proveedores
   - no debería existir un catálogo comercial que no haya sido probado.
2. La oferta B2C necesita estructura de triggers
   - el usuario no compra por "características"; compra por riesgo, urgencia y tranquilidad.
3. Hay que separar mejor "valor técnico" de "valor negociado"
   - la app ya entrega muchas capacidades; el negocio no debe presentar todo como si ya estuviera aprobado comercialmente.
4. Debe haber una política explícita de descuentos y cuotas
   - sin eso, los equipos de ventas y marketing se desordenan.
5. Se necesita más claridad de adquisición
   - la mejor estructura no sirve si no hay equipo para activar cada segmento.

---

## 4. Sugerencias de estrategia de marketing y precios

### 4.1 Para B2C

Recomendación principal:

- Mejorar el funnel con un modelo de prueba emocional y no solo técnico.

Propuestas:

- Ofrecer un ciclo de activación inmediata: registro + QR + perfil + prueba de recuperación.
- Hacer campañas por eventos: "perdí a mi mascota", "mi perro se escapó", "obtén el QR antes de que pase algo".
- Enfocar mensajes en la pérdida, no solo en preventivos.
- En la UI, mostrar la utilidad del plan en momentos de urgencia y no solo en un catálogo de features.

Estrategia de precios:

- Mantener `UserPlus` y `UserFamilia` como base.
- Considerar promoción de 3 meses por primera activación.
- Considerar programas familiares, no solo precios por mascota.

### 4.2 Para clínicas

Recomendación principal:

- Vender ROI operativo y reputación local.

Propuestas:

- Generar paquetes por zona o cantón.
- Mejorar material de ventas con estudios de caso y métricas de visibilidad.
- Crear un paquete de onboarding y soporte para que la implementación no sea un costo oculto.
- Hacer referencia a certificados, trazabilidad y reputación.

### 4.3 Para tiendas

Recomendación principal:

- Vender operaciones de venta local y reservas.

Propuestas:

- Empezar con `StorePlus` como plan de entrada, no con un paquete complejo.
- Empezar por tiendas con presencia física activa, no todas las categorías.
- Ofrecer una prueba de 30 a 60 días con campañas de activación local.

### 4.4 Para municipalidades

Recomendación principal:

- Venderlo como infraestructura pública de gestión animal.

Propuestas:

- Segmentarlo por tamaño de cantón o índice de flujo.
- Diseñar demo institucional con casos reales.
- Incluir reportes y dashboard para gestión de animales capturados y rehabilitación.

### 4.5 Para proveedores de servicios

Recomendación principal:

- No vender una tarifa recurrente todavía.

Propuestas:

- Modo prueba concentrado: 30 días gratis con activación.
- Luego: pricing por niveles de visibilidad, reserva o afluencia.
- Medir si la oferta genera ganancia por cada reserva, cada lead o cada categoría.

---

## 5. Mi opinión sobre cada plan

### Free / Explorador

Muy bien como plan de entrada. Debe mantener su rol de captación y educación. No hay que volverlo "muy completo"; hay que dejar bien claro qué diferencia la versión superior.

### UserPlus — ₡2.990/mes

Es correcto en precio y posicionamiento para Costa Rica. Es útil, accesible y muy probablemente convierte mejor que un precio mucho más alto. Es el mejor plan para empuje de adquisición.

### UserFamilia — ₡4.990/mes

Es razonable, pero no debe verse como "más mascotas" únicamente. Su valor real es la familia, el historial médico, la compartición y la continuidad de cuidado.

### StorePlus — ₡12.000/mes

Razonable en valor para negocio real con presencia física. Requiere activación enfocada. Si no hay demanda, no es sostenible.

### StorePartner — ₡25.000/mes

Puede funcionar, pero solo si se está cobrando por que el negocio recibe capacidad operativa real, analytics y prioridad. Si se vende como un upgrade superficial, será difícil justificarlo.

### ShelterPlus — ₡8.000/mes

Bien posicionado y claramente útil para refugios. Tiene el mejor perfil de compra B2B funcional y social.

### ClinicPlus — ₡15.000/mes

Buen precio y justo para una clínica con valor local. Tiene sentido si se combina con métricas, destacado y visibilidad.

### ClinicPartner — ₡35.000/mes

Correcto si incluye certificados y API. Si no, se vuelve un precio difícil de defender.

### Proveedores `Verified` / `Featured`

Aún no son definitivos. Son una capa técnica interesante, pero no una capa comercial validada.

---

## 6. Recomendación de modelo de ganancias

El negocio no debería depender solo de la venta de suscripciones. Debe combinar tres patas:

1. Suscripciones recurrentes de dueños y negocios.
2. Packs de activación y soporte B2B.
3. Comisiones o valor agregado del marketplace una vez que exista demanda real.

### Modelo sugerido

#### A. B2C

- `Free` + conversion a ` Plus` / `Familia`.
- Upsell por salud, continuidad y recuperación.
- Promociones: 3 meses con descuento si se activa el QR y se completa el perfil.

#### B. B2B

- Paga por visibilidad y gestión operativa.
- Contratos con onboarding y soporte.
- Posible modelo anual con descuento por prepagado.

#### C. Proveedores / marketplace

- Primero: prueba, activación y datos.
- Segundo: pricing por visibilidad y reserva.
- Tercero: comisión por transacción si hay flujo de pago real.

### Fórmula de rentabilidad orientativa

Si el negocio logra una base de:

- 1.000 usuarios `UserPlus` = ₡2,99 millones/mes
- 500 usuarios `UserFamilia` = ₡2,495 millones/mes
- 50 clínicas `ClinicPlus` = ₡750.000/mes
- 20 clínicas `ClinicPartner` = ₡700.000/mes
- 30 tiendas `StorePlus` = ₡360.000/mes
- 10 tiendas `StorePartner` = ₡250.000/mes
- 20 refugios `ShelterPlus` = ₡160.000/mes

El ingreso recurrente bruto mensual rondaría aproximadamente:

- ₡7,7 a ₡8,0 millones/mes antes de costos de atención, soporte, cumplimiento y marketing.

Eso no es un negocio “gigante”, pero sí es un negocio viable, sobre todo considerando que la barrera de entrada es baja y el valor emocional es fuerte.

---

## 7. Mi recomendación final de estructura comercial

### Estructura recomendada para el próximo ciclo

#### B2C

- `Free`
- `UserPlus` — ₡2.990/mes
- `UserFamilia` — ₡4.990/mes

#### B2B local

- `ShelterPlus` — ₡8.000/mes
- `ClinicPlus` — ₡15.000/mes
- `ClinicPartner` — ₡35.000/mes
- `StorePlus` — ₡12.000/mes
- `StorePartner` — ₡25.000/mes

#### B2G

- `MuniBasica` — ₡150.000/año
- `MuniFull` — ₡300.000/año
- `MuniRedRegional` — ₡500.000/año

#### Proveedores

- `Free`
- `Verified` — prueba de 30 días + pricing validado luego
- `Featured` — solo después de medir activación y demanda

### Qué no haría

- No definir una tarifa fija para proveedores sin demanda.
- No vender demasiado “pack tecnológico” sin cubrir la parte de soporte y operación.
- No mezclar ventas de propiedad, reservas y comisiones sin un marco legal claro.
- No depender de una única fuente de ingresos.

---

## 10. Proyección de ganancias a 5 años

A continuación dejo una estimación concreta de ingresos y rentabilidad para NALA/PawTrack aplicando los precios actuales y la lógica del ecosistema completo: dueños, clínicas, tiendas, refugios, municipalidades y proveedores de servicios.

### 10.1 Supuestos base del modelo

- Mercado objetivo: hogares con mascotas, pequeñas clínicas, tiendas de mascotas, refugios y municipalidades en Costa Rica.
- B2C: combinación de `Free`, `UserPlus` y `UserFamilia` con activación por QR, perfil completo y uso durante eventos de pérdida o seguimiento sanitario.
- B2B: ingresos recurrentes por visibilidad local, gestión operativa, trazabilidad y herramientas de coordinación.
- Proveedores de servicios: ingreso no se asume como recurrente desde el inicio; se proyecta como prueba/activación, y el valor real viene de reservas, visibilidad premium y leads de alta intención.
- Costo de adquisición: marketing, onboarding, soporte, atención a casos y moderación de comunidad.
- Cobertura: se asume crecimiento por corredor geográfico antes que nacional completo.

### 10.2 Vías de ingresos consideradas

1. Suscripciones de dueños.
2. Suscripciones de negocios y organizaciones.
3. Servicios B2B de activación y soporte.
4. Marketplace de servicios con visibilidad premium y reservas.
5. Programas institucionales y aliados municipales o de red.

### 10.3 Proyección base anual de ingresos

Precios base:

- `UserPlus`: ₡2.990/mes
- `UserFamilia`: ₡4.990/mes
- `ClinicPlus`: ₡15.000/mes
- `ClinicPartner`: ₡35.000/mes
- `StorePlus`: ₡12.000/mes
- `StorePartner`: ₡25.000/mes
- `ShelterPlus`: ₡8.000/mes
- `MuniBasica`: ₡150.000/año
- `MuniFull`: ₡300.000/año
- `MuniRedRegional`: ₡500.000/año

#### Escenario estimado para 5 años

| Año | B2C recurrente | B2B recurrente | Marketplace / add-ons | Ingreso anual total |
| --- | -------------: | -------------: | --------------------: | ------------------: |
| 1   |         ₡65,8M |         ₡15,5M |                 ₡1,0M |              ₡82,3M |
| 2   |        ₡197,5M |         ₡50,4M |                 ₡1,5M |             ₡249,4M |
| 3   |        ₡442,9M |         ₡97,2M |                 ₡5,0M |             ₡545,1M |
| 4   |        ₡813,8M |        ₡146,0M |                ₡12,0M |             ₡971,8M |
| 5   |      ₡1.364,4M |        ₡199,4M |                ₡22,0M |           ₡1.585,8M |

### 10.4 Desglose por segmento

#### A. Usuarios B2C

| Año | UserPlus | UserFamilia | Ingreso anual B2C |
| --- | -------: | ----------: | ----------------: |
| 1   |    1.000 |         500 |            ₡65,8M |
| 2   |    3.000 |       1.500 |           ₡197,5M |
| 3   |    6.500 |       3.500 |           ₡442,9M |
| 4   |   11.000 |       7.000 |           ₡813,8M |
| 5   |   18.000 |      12.000 |         ₡1.364,4M |

Cálculo base:

- 1.000 UserPlus × 12 × ₡2.990 = ₡35,9M
- 500 UserFamilia × 12 × ₡4.990 = ₡29,9M
- total año 1 = ₡65,8M

Este segmento es la base más sólida porque responde a una necesidad emocional con alta intención de pago y buena retención si el valor se comunica en el momento correcto.

#### B. Negocios B2B

| Año | Clínicas | Tiendas | Refugios | Municipalidades | Ingreso anual B2B |
| --- | -------: | ------: | -------: | --------------: | ----------------: |
| 1   |    ₡9,6M |   ₡3,7M |    ₡1,4M |           ₡0,8M |            ₡15,5M |
| 2   |   ₡34,2M |  ₡11,0M |    ₡2,9M |           ₡2,3M |            ₡50,4M |
| 3   |   ₡57,6M |  ₡31,0M |    ₡4,3M |           ₡4,3M |            ₡97,2M |
| 4   |   ₡85,2M |  ₡48,7M |    ₡5,8M |           ₡6,3M |           ₡146,0M |
| 5   |  ₡116,4M |  ₡66,5M |    ₡7,7M |           ₡8,8M |           ₡199,4M |

Este flujo es muy valioso porque las clínicas, tiendas, refugios y municipalidades compran resultados reales: visibilidad, coordinación, trazabilidad, menos trabajo manual y mejor atención al cliente.

#### C. Proveedores de servicios y marketplace

| Año | Proveedores activos | Ingreso estimado |
| --- | ------------------: | ---------------: |
| 1   |               30-60 |            ₡1,0M |
| 2   |             100-200 |            ₡1,5M |
| 3   |             300-500 |            ₡5,0M |
| 4   |           700-1.000 |           ₡12,0M |
| 5   |         1.200-1.800 |           ₡22,0M |

Este segmento debería considerarse como una línea con alto potencial, pero no como ingreso seguro desde el inicio. La rentabilidad real llega cuando hay oferta local suficiente, reservas reales y buena activación del proveedor.

### 10.5 EBITDA estimado a 5 años

Con estructura operativa razonable, el negocio puede sostener los siguientes niveles de margen:

| Año | EBITDA estimado |
| --- | --------------: |
| 1   |       ₡20M-₡25M |
| 2   |      ₡90M-₡110M |
| 3   |     ₡220M-₡260M |
| 4   |     ₡430M-₡490M |
| 5   |     ₡770M-₡910M |

Estos números asumen:

- marketing controlado por corredor geográfico;
- soporte y atención a clientes con procesos claros;
- infraestructura digital lean;
- no una estructura corporativa pesada;
- crecimiento de base recurrente por segmentos de valor.

### 10.6 Qué escenario es más probable

#### Escenario conservador

- Crecimiento más lento en B2C.
- Menos clínicas y proveedores activos.
- Resultado: alrededor de ₡850M-₡1.100M anuales a 5 años.

#### Escenario base

- Mejor adopción de usuarios, más negocios activos y una base de proveedores funcional.
- Resultado: alrededor de ₡1.585,8M anuales a 5 años.

#### Escenario agresivo

- Densidad alta en un corredor urbano clave, fuerte activación clínica y marketplace operativo.
- Resultado: ₡2.000M-₡2.800M anuales a 5 años.

### 10.7 Conclusión de la proyección

La operación es viable y tiene potencial de rentabilidad estructural en 5 años, pero solo si se ejecuta con disciplina:

1. B2C debe crecer con activación emocional y retención clara.
2. B2B debe venderse por ROI, no por “bonitas features”.
3. municipalidades y aliados deben ser tratados como clientes institucionales, no como add-ons.
4. marketplace de proveedores debe mantenerse en fase de validación hasta comprobar demanda real.

En otras palabras: NALA/PawTrack tiene un modelo de ingresos sólido, pero su rentabilidad más fuerte llega cuando la red local se vuelve densa y la operación se vuelve repetible.

---

## 11. Conclusión

El app actual tiene una base bastante sólida y la lógica comercial general es buena. Los precios de los planes B2C son razonables, los planes B2B tienen sentido y el producto está bien posicionado para un mercado local con alta necesidad emocional y mucho potencial de adopción.

Sin embargo, el mayor riesgo no está en la tecnología: está en la monetización del marketplace, en la claridad del valor percibido y en la disciplina del crecimiento. El negocio debe crecer con un modelo claro, soportado por evidencia, no por supuesto técnico ni por precios que aún no han sido validados en el mercado.

La recomendación más importante es esta:

- mantener los precios actuales donde sí tienen sentido;
- no cerrar aún el pricing del marketplace de proveedores;
- vender valor emocional para dueños, impacto operativo para negocios y eficiencia institucional para municipalidades;
- usar pruebas, conversiones y activación como base definitiva para definir la tarifa comercial del siguiente ciclo.

En mi criterio, el modelo ya está en la zona correcta para lanzar, pero aún no está en la zona correcta para declarar “precio final y completamente maduro” en todos los segmentos.

---

## 12. Decisión ejecutiva resumida

Si tuviera que tomar una decisión hoy como gerente de negocio o marketing, diría:

- Sí, los precios actuales son correctos para una primera etapa comercial y tienen lógica en Costa Rica.
- Sí, la estructura de planes es buena y está orientada a un producto real con utilidad concreta.
- No, el marketplace de proveedores no está todavía listo para pricing definitivo.
- Sí, el negocio puede ser rentable si se ejecuta con enfoque en densidad local, activación y soporte.
- No, no conviene escalar sin validación del mercado real de proveedores y sin claridad de operación de pagos y soporte.

Eso significa: el producto tiene potencial, pero la rentabilidad real dependerá menos de la app y más de la ejecución comercial.
