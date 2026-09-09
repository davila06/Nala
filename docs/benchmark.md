# NALA / PawTrack CR — Benchmark de mercado para Costa Rica

> Version: 2026-09-08  
> Alcance: benchmark estrategico de producto, mercado y monetizacion.  
> Estado: documento de decision; no sustituye validacion comercial, legal ni investigacion de campo.

## 1. Resumen ejecutivo

NALA no compite contra una sola aplicacion. Compite contra un conjunto de
soluciones parciales que los dueños ya combinan cuando una mascota se pierde o
necesita cuidado:

- grupos de WhatsApp, Facebook e Instagram;
- llamadas a veterinarias, refugios y municipalidades;
- placas QR, microchips y publicaciones estaticas;
- collares GPS de fabricantes internacionales;
- directorios informales de groomers, paseadores, hoteles y entrenadores.

La ventaja estrategica de NALA es integrar identidad, recuperacion, coordinacion,
servicios y datos sanitarios en una red local. La desventaja principal no es
tecnica: es la falta de densidad inicial. Un mapa, una red de aliados, un
marketplace y las alertas solo se vuelven mejores cuando existen suficientes
mascotas, proveedores, clinicas y usuarios activos en cada zona.

### Veredicto

1. **Problema:** fuerte y frecuente; la perdida de una mascota tiene alta carga
   emocional y requiere coordinacion inmediata.
2. **Diferenciacion:** alta en integracion local; media en cada componente
   individual, porque QR, GPS, redes sociales y directorios ya existen por
   separado.
3. **Monetizacion:** razonable en B2C y B2B, pero debe probarse con uso real.
   El marketplace de proveedores tiene gates tecnicos, pero su precio recurrente
   aun no esta aprobado.
4. **Riesgo de ejecucion:** alto por efecto red, confianza, datos sensibles,
   soporte operativo y dependencia de canales externos como WhatsApp/SINPE.
5. **Prioridad:** ganar un corredor geografico concreto de Costa Rica antes de
   intentar cobertura nacional.

## 2. Metodologia y limites

Este benchmark usa:

- el codigo actual del backend y frontend como fuente de capacidades;
- `SubscriptionPricing` como fuente de precios tecnicos;
- documentos internos existentes, marcando sus cifras como estimaciones cuando
  no hay fuente primaria;
- fuentes publicas institucionales para contexto regulatorio y territorial;
- comparacion por comportamiento sustituido, no solo por nombres de empresas.

No se presenta como un censo exhaustivo de competidores. En Costa Rica muchas
alternativas son informales, locales o cambian de canal con frecuencia. Antes
de invertir en adquisicion se debe hacer una ronda de entrevistas, mystery
shopping y medicion de grupos/canales por canton.

## 3. Mercado costarricense: lectura operativa

### 3.1 Rangos internos de planificacion

Los documentos del repositorio usan cifras diferentes. Para evitar falsa
precision, este benchmark adopta rangos:

| Indicador                       |  Rango de planificacion | Uso recomendado                                    |
| ------------------------------- | ----------------------: | -------------------------------------------------- |
| Hogares con mascotas            |         800.000-890.000 | TAM indicativo B2C, no dato oficial consolidado    |
| Municipalidades/cantones        |                      82 | TAM B2G de referencia institucional                |
| Clinicas veterinarias           |               600-1.400 | Hipotesis amplia; validar con directorios y SENASA |
| Organizaciones aliadas/refugios |                    ~200 | Hipotesis de prospeccion; validar por provincia    |
| Perdidas de mascotas            | No fijar cifra nacional | Medir desde reportes NALA y fuentes oficiales      |

Estos rangos sirven para escenarios, no para publicidad ni para un pitch como
si fueran estadisticas oficiales. El benchmark recomienda publicar la fuente y
fecha de cualquier cifra externa antes de usarla comercialmente.

### 3.2 Condiciones favorables

- Alta adopcion de telefono movil y mensajeria favorece un PWA sin instalacion.
- WhatsApp reduce friccion para difusion y soporte, pero introduce dependencia de
  Meta, plantillas, opt-in y costos operativos.
- SINPE es un diferenciador local para reportes de pago, pero no elimina la
  necesidad de conciliacion, evidencia, reembolsos y controles antifraude.
- La presencia territorial de SENASA, oficinas cantonales y clinicas crea
  posibles nodos de confianza y distribucion.
- La geografia por cantones permite lanzar por corredores medibles en vez de
  prometer cobertura nacional desde el dia uno.

### 3.3 Barreras

- El usuario puede publicar gratis en redes sociales y no percibir valor hasta
  que ocurre una perdida.
- Un marketplace sin oferta local suficiente parece vacio; un proveedor no paga
  por un directorio sin demanda.
- GPS requiere hardware, bateria, conectividad, privacidad y soporte; no es una
  feature puramente digital.
- Los registros medicos exigen consentimiento, control de acceso y claridad
  sobre que es un documento de PawTrack y que es un certificado oficial.
- NALA no debe presentarse como integrada oficialmente con SENASA sin convenio,
  interoperabilidad y autorizacion explicita.

## 4. Alternativas competitivas

### 4.1 Matriz de sustitutos

Puntaje: 1 = debil, 5 = fuerte. Es una comparacion estrategica cualitativa, no
una medicion de cuota de mercado.

| Alternativa                        | Alcance | Recuperacion coordinada | Identidad/QR |  GPS  | Servicios/reservas | Confianza local | Costo de entrada |
| ---------------------------------- | :-----: | :---------------------: | :----------: | :---: | :----------------: | :-------------: | :--------------: |
| WhatsApp/Facebook/grupos vecinales |    5    |            2            |      1       |   1   |         2          |        3        |        5         |
| Placa QR/microchip aislado         |    2    |            1            |      5       |   1   |         1          |        3        |        4         |
| Collar GPS internacional           |    3    |            1            |      2       |   5   |         1          |        2        |        2         |
| Clinica veterinaria/SENASA         |    2    |            2            |      4       |   1   |         2          |        5        |        3         |
| Directorios/redes de proveedores   |    3    |            1            |      1       |   1   |         3          |        2        |        4         |
| Portal municipal/refugio           |    2    |            3            |      3       |   1   |         1          |        5        |        3         |
| **NALA/PawTrack**                  |  **3**  |          **5**          |    **5**     | **4** |       **4**        |      **4**      |      **3**       |

La lectura importante es que NALA no necesita ser mejor que Facebook en alcance
ni mejor que Tractive en hardware. Debe ser la capa que convierte una señal
dispersa en un caso trazable: perfil, ubicacion, alerta, avistamiento,
coordinacion, contacto protegido y reunificacion.

### 4.2 Benchmark funcional de NALA

| Capacidad                           | Estado actual en NALA                                  | Diferenciacion en Costa Rica                                     | Riesgo                                  |
| ----------------------------------- | ------------------------------------------------------ | ---------------------------------------------------------------- | --------------------------------------- |
| Perfil publico y QR                 | Implementado                                           | Identidad interoperable sin app para quien escanea               | Adopcion del QR y datos actualizados    |
| Reporte de perdida                  | Implementado                                           | Flujo estructurado en vez de post aislado                        | Densidad de usuarios                    |
| Mapa publico                        | Implementado                                           | Casos, avistamientos, clinicas, tiendas y servicios en una vista | Privacidad y ruido                      |
| Matching visual IA                  | Implementado                                           | Reduce trabajo manual al comparar fotos                          | Calidad de fotos y costo por consulta   |
| Difusion multicanal                 | Implementado                                           | WhatsApp, email y redes desde un caso                            | Dependencia de terceros                 |
| Case Room y zonas                   | Implementado                                           | Coordinacion operacional en tiempo real                          | Requiere voluntarios activos            |
| Chat enmascarado/handover           | Implementado                                           | Contacto seguro sin exponer PII                                  | Moderacion y soporte                    |
| Bounty/SINPE                        | Implementado en flujo operativo                        | Incentivo verificable para recuperacion                          | Custodia, disputas, fraude y regulacion |
| Collar GPS                          | Implementado para CollarTag e integraciones soportadas | Une GPS con red local de recuperacion                            | Hardware, conectividad y bateria        |
| Registro medico/PDF                 | Implementado con gates por plan                        | Continuidad sanitaria entre clinicas                             | Consentimiento y caracter oficial       |
| Marketplace de servicios            | Implementado                                           | Directorio + disponibilidad + reservas locales                   | Oferta inicial y responsabilidad        |
| Tiendas y pedidos                   | Implementado                                           | Descubrimiento y compra local                                    | Inventario, entrega y reembolsos        |
| Clinicas y portales institucionales | Implementado parcialmente por modulo                   | Nodos de confianza y datos sanitarios                            | Billing, roles y adopcion institucional |

## 5. Benchmark de precios

Los siguientes son precios tecnicos definidos en `SubscriptionPricing`; no todos
representan billing comercial completamente operacional en cada entorno.

| Segmento               | Tier         |       Precio | Valor principal                                               |
| ---------------------- | ------------ | -----------: | ------------------------------------------------------------- |
| Dueño                  | Explorador   |       Gratis | 1 mascota, QR, perdida, mapa y comunidad                      |
| Dueño                  | Plus         |   ₡2.990/mes | Hasta 3 mascotas, IA, coordinacion y GPS                      |
| Dueño                  | Familia      |   ₡4.990/mes | Mascotas ilimitadas, familia y expediente medico completo     |
| Clinica                | Plus         |  ₡15.000/mes | Visibilidad, badge, estadisticas y alertas                    |
| Clinica                | Partner      |  ₡35.000/mes | Certificados, API/widget e integraciones avanzadas            |
| Tienda                 | Plus         |  ₡12.000/mes | Pedidos in-app y operacion comercial                          |
| Tienda                 | Partner      |  ₡25.000/mes | Analytics y multi-sucursal                                    |
| Refugio                | Plus         |   ₡8.000/mes | Animales ilimitados, ferias y visibilidad                     |
| Municipalidad          | Basica       | ₡150.000/año | Portal de control animal por canton                           |
| Municipalidad          | Full         | ₡300.000/año | API, fotos, estadisticas y reportes                           |
| Municipalidad          | Red Regional | ₡500.000/año | Multi-canton y dashboard regional                             |
| Proveedor de servicios | Free         |       Gratis | Directorio y contacto                                         |
| Proveedor de servicios | Verified     |    Pendiente | Catalogo, disponibilidad y reservas; trial tecnico de 30 dias |
| Proveedor de servicios | Featured     |    Pendiente | Prioridad, badge y estadisticas; no checkout comercial        |

### Lectura de precio

- ₡2.990/mes es una barrera de entrada baja para un producto de emergencia,
  siempre que el usuario entienda el valor antes de perder la mascota.
- Familia debe venderse por continuidad sanitaria y acceso compartido, no solo
  por cantidad de mascotas.
- Clinicas, tiendas y municipalidades compran resultados operativos; requieren
  onboarding, soporte, auditoria y evidencia de retorno.
- El precio del proveedor de servicios debe probarse despues de demostrar
  demanda. El trial de 30 dias mide activacion; no demuestra willingness to pay.
- El precio de una reserva no es ingreso de NALA por si mismo. Comision,
  impuestos, payout, reembolso y custodia deben aprobarse antes de publicarse.

## 6. Posicionamiento recomendado

### Para dueños

> "La red costarricense para identificar, encontrar y proteger a tu mascota,
> antes, durante y despues de una perdida."

Mensaje de entrada: QR y reporte gratuito. Mensaje de conversion: velocidad de
recuperacion, coordinacion, alertas y GPS. Mensaje de retencion: expediente,
familia, servicios y continuidad sanitaria.

### Para proveedores de servicios

> "Convierte tu disponibilidad local en reservas verificables de dueños de
> mascotas."

No vender "verificacion" como licencia estatal. Presentarla como revision
documental interna de PawTrack hasta que exista base legal o convenio distinto.

### Para clinicas y municipalidades

> "Un nodo institucional para identificar, atender y devolver mascotas con
> trazabilidad."

La venta debe priorizar casos de uso medibles: escaneos, casos resueltos,
tiempo de respuesta, certificados, reportes y cobertura territorial.

## 7. Estrategia de entrada al mercado

### Fase 1 — Un corredor, no todo el pais

Elegir un corredor con suficiente densidad de dueños, clinicas, refugios y
proveedores. Medir durante 90 dias:

- mascotas registradas por canton;
- porcentaje con QR activado y perfil completo;
- reportes de perdida y tiempo hasta primer avistamiento;
- reunificaciones y tiempo mediano de recuperacion;
- usuarios activos mensuales;
- clinicas/aliados que responden alertas;
- proveedores con catalogo, disponibilidad y primera reserva.

### Fase 2 — Nodos de confianza

Priorizar clinicas, refugios, rescatistas, tiendas y municipalidades como
canales de alta confianza. Cada nodo debe tener un codigo de origen para medir
registros, escaneos, reportes y conversion a Plus/Familia.

### Fase 3 — Monetizacion basada en uso

No optimizar primero por cantidad de features. Optimizar por eventos de valor:

1. mascota registrada y perfil completo;
2. QR escaneado correctamente;
3. perdida difundida;
4. primer avistamiento recibido;
5. reunificacion confirmada;
6. servicio reservado y completado;
7. expediente medico consultado/exportado.

## 8. KPIs y umbrales de decision

| Area         | KPI                                        | Senal de traccion | Senal de riesgo |
| ------------ | ------------------------------------------ | ----------------: | --------------: |
| Activacion   | Perfil completo / registros                |            >= 60% |           < 35% |
| Identidad    | QR probado en 30 dias                      |            >= 25% |           < 10% |
| Recuperacion | Perdidas con primer avistamiento en 24 h   |            >= 40% |           < 15% |
| Red          | Alertas respondidas por aliado             |            >= 50% |           < 20% |
| Retencion    | Usuarios activos a 30 dias                 |            >= 30% |           < 15% |
| Pago B2C     | Free a Plus/Familia en 90 dias             |             >= 3% |            < 1% |
| Proveedores  | Primer catalogo en 14 dias                 |            >= 50% |           < 25% |
| Marketplace  | Proveedor con primera reserva en 60 dias   |            >= 30% |           < 10% |
| Clinicas     | Primer escaneo en 30 dias                  |            >= 60% |           < 30% |
| Operacion    | Incidentes resueltos dentro de SLA interno |            >= 90% |           < 70% |

Estos umbrales son hipotesis iniciales para experimentacion, no benchmarks
publicados del mercado.

## 9. Riesgos y controles

| Riesgo                    | Impacto | Control recomendado                                                         |
| ------------------------- | ------- | --------------------------------------------------------------------------- |
| Red vacia                 | Alto    | Lanzamiento por zona y nodos ancla                                          |
| Spam o denuncias falsas   | Alto    | Rate limits, auditoria, reputacion y moderacion                             |
| Exposicion de PII         | Alto    | Contacto enmascarado, consentimiento y minimizacion                         |
| Confusion con SENASA      | Alto    | Usar "SENASA-ready" solo cuando corresponda; no afirmar integracion oficial |
| Custodia de recompensas   | Alto    | No ampliar payout hasta definir legal, conciliacion y disputas              |
| Reserva fallida/no-show   | Medio   | Snapshot comercial, politicas, reembolso e incidentes                       |
| Datos medicos incorrectos | Alto    | Fuente, clinic access log, permisos y exportaciones trazables               |
| Dependencia de WhatsApp   | Medio   | Email, PWA y canales alternativos                                           |
| Hardware GPS sin soporte  | Medio   | Limitar modelos soportados y publicar estado de conectividad                |

## 10. Plan de validacion de 30 dias

### Semana 1: demanda

- Entrevistar 15 dueños, 5 clinicas, 5 proveedores y 2 organizaciones.
- Medir como resuelven hoy una perdida y cuanto tardan.
- Validar si pagarían ₡2.990 o ₡4.990 despues de un caso de uso concreto.

### Semana 2: oferta

- Reclutar 10 proveedores de servicios en dos categorias.
- Activar catalogo, reglas horarias y contacto publico.
- Medir tiempo hasta primera disponibilidad y primera solicitud.

### Semana 3: recuperacion

- Ejecutar simulacros controlados de perdida con QR, avistamiento y Case Room.
- Medir tiempo de alerta, respuesta, matching y cierre.
- Verificar que no se exponga PII a usuarios anonimos.

### Semana 4: conversion

- Probar onboarding Plus/Familia y dos mensajes de valor.
- Probar activacion de proveedor sin anunciar precio definitivo.
- Decidir si existe evidencia para aprobar pricing, ampliar la zona o corregir
  primero el producto.

## 11. Analisis de ingresos y gastos

### 11.1 Principios del modelo

Este modelo no presenta ingresos proyectados como ventas realizadas. Usa los
precios tecnicos del backend y supuestos de adopcion para responder tres
preguntas:

1. Cuanto ingreso recurrente puede producir un corredor local si alcanza
   densidad suficiente.
2. Que costos crecen con usuarios, mensajes, imagenes, GPS y operaciones.
3. Que lineas deben excluirse del caso base porque aun no tienen precio,
   contrato, payout o validacion comercial.

Se excluyen del caso base: membresias de proveedores de servicios, seguros,
venta de datos anonimizados, quioscos, PawTrack Day y cualquier comision de
marketplace no aprobada. El marketplace puede generar reservas, pero una
reserva no equivale automaticamente a ingreso de NALA.

### 11.2 Ingresos por linea

| Linea                    |        Precio tecnico | Naturaleza          | Estado para el modelo                        |
| ------------------------ | --------------------: | ------------------- | -------------------------------------------- |
| UserPlus                 |            ₡2.990/mes | Recurrente B2C      | Activo en catalogo                           |
| UserFamilia              |            ₡4.990/mes | Recurrente B2C      | Activo en catalogo                           |
| ClinicPlus               |           ₡15.000/mes | Recurrente B2B      | Tier activo; venta debe validarse            |
| ClinicPartner            |           ₡35.000/mes | Recurrente B2B      | Tier activo; venta debe validarse            |
| StorePlus                |           ₡12.000/mes | Recurrente B2B      | Tier activo; venta debe validarse            |
| StorePartner             |           ₡25.000/mes | Recurrente B2B      | Tier activo; venta debe validarse            |
| ShelterPlus              |            ₡8.000/mes | Recurrente B2B      | Tier activo; mercado por validar             |
| Municipalidades          | ₡150.000-₡500.000/año | Contrato B2G        | Catalogo tecnico; ciclo de venta largo       |
| Bounty                   |         10% propuesto | Transaccional       | No contar hasta validar custodia/payout      |
| Proveedores de servicios |             Pendiente | Membresia propuesta | Excluir del caso base                        |
| Productos fisicos/GPS    |              Variable | Margen por venta    | Excluir hasta tener proveedor y costo landed |

### 11.3 Escenarios de ingresos mensuales

Los escenarios siguientes representan el mes 12 de un corredor local. Son
escenarios de planificacion, no un forecast estadistico. La municipalidad se
prorratea para visualizar MRR equivalente; en la realidad puede cobrarse anual.

| Fuente en mes 12             |             Conservador |                    Base |                  Traccion |
| ---------------------------- | ----------------------: | ----------------------: | ------------------------: |
| Plus: usuarios pagos         | 150 x ₡2.990 = ₡448.500 | 300 x ₡2.990 = ₡897.000 | 700 x ₡2.990 = ₡2.093.000 |
| Familia: usuarios pagos      |  30 x ₡4.990 = ₡149.700 |  75 x ₡4.990 = ₡374.250 |   180 x ₡4.990 = ₡898.200 |
| Clinicas Plus/Partner        |                 ₡45.000 |                ₡150.000 |                  ₡450.000 |
| Tiendas Plus/Partner         |                 ₡84.000 |                ₡240.000 |                  ₡600.000 |
| ShelterPlus                  |                      ₡0 |                  ₡8.000 |                   ₡24.000 |
| Municipalidad prorrateada    |                      ₡0 |                 ₡12.500 |                   ₡50.000 |
| Publicidad/patrocinios       |                      ₡0 |                 ₡50.000 |                  ₡150.000 |
| Bounty/comisiones            |                      ₡0 |                 ₡25.000 |                  ₡100.000 |
| **Ingreso mensual modelado** |            **₡727.200** |          **₡1.756.750** |            **₡4.365.200** |

El caso base equivale aproximadamente a ₡21,1 millones anualizados antes de
costos, impuestos, reembolsos y churn. No incluye ingresos de proveedores de
servicios porque su precio sigue pendiente de aprobacion.

### 11.4 Estructura de gastos mensuales

| Gasto                                                      |                  Piloto |            Operacion base | Comentario                                        |
| ---------------------------------------------------------- | ----------------------: | ------------------------: | ------------------------------------------------- |
| Azure SQL, App Service, Storage, Key Vault, monitorizacion |        ₡50.000-₡150.000 |         ₡150.000-₡450.000 | Depende de tier, backups, logs y carga            |
| IA/matching, mapas y procesamiento de imagen               |         ₡10.000-₡60.000 |          ₡60.000-₡250.000 | Variable por consultas y volumen de imagen        |
| WhatsApp, email, push y SMS                                |          ₡5.000-₡50.000 |          ₡50.000-₡250.000 | Depende de plantillas, conversaciones y proveedor |
| Pagos, conciliacion y comisiones bancarias                 |              ₡0-₡25.000 |          ₡25.000-₡120.000 | No asumir costo cero por usar SINPE               |
| Soporte y operaciones                                      |       ₡100.000-₡300.000 |         ₡300.000-₡900.000 | Atencion de casos, proveedores y disputas         |
| Ventas, campo y activacion de nodos                        |       ₡100.000-₡350.000 |       ₡350.000-₡1.000.000 | Clinicas, aliados, municipalidades y proveedores  |
| Legal, contabilidad y cumplimiento                         |        ₡50.000-₡150.000 |         ₡100.000-₡300.000 | Privacidad, contratos, impuestos y revisiones     |
| **Gasto operativo estimado**                               | **₡315.000-₡1.085.000** | **₡1.035.000-₡3.270.000** | Antes de salarios completos e impuestos           |

Los rangos no son cotizaciones. Azure, Meta, proveedores de correo, mapas,
procesadores de pago y hardware deben cotizarse con el volumen real. El costo
de desarrollo, depreciacion de hardware, salario del fundador y costo de
adquisicion de clientes deben agregarse al presupuesto de caja, aunque no
aparezcan como gasto de infraestructura.

### 11.5 Punto de equilibrio operativo

Con un gasto operativo base de ₡1,5 millones mensuales y un margen de
contribucion aproximado del 85% para suscripciones digitales, NALA necesitaria
alrededor de **₡1,76 millones de ingresos mensuales** para cubrir operacion:

```text
Break-even = gasto operativo / margen de contribucion
Break-even = ₡1.500.000 / 0,85 = ₡1.764.706 MRR
```

El caso base del mes 12 (₡1.756.750) queda practicamente en equilibrio antes
de salarios completos, impuestos y CAC. Esto indica que el producto necesita
una de estas palancas para ser sostenible:

- mas conversion B2C sin elevar proporcionalmente soporte;
- contratos B2B/B2G con onboarding y renovacion medibles;
- publicidad/patrocinios de margen alto;
- automatizacion de soporte y conciliacion;
- una densidad local suficiente para reducir CAC.

### 11.6 Economia unitaria y adquisicion

| Metrica                       | Hipotesis inicial | Regla de decision                            |
| ----------------------------- | ----------------: | -------------------------------------------- |
| ARPU combinado B2C            | ₡3.400-₡3.800/mes | Medir por cohorte, no por promedio global    |
| Margen de suscripcion digital |           80%-90% | Revisar IA, mensajes y soporte por usuario   |
| CAC B2C de piloto             |     ₡1.500-₡5.000 | No escalar si supera 4 meses de margen bruto |
| CAC B2B clinica/tienda        |  ₡50.000-₡250.000 | Exigir payback menor a 6-9 meses             |
| Churn mensual objetivo B2C    |              < 4% | Investigar cualquier cohorte sobre 6%        |
| Payback B2C                   |         < 4 meses | Incluir descuentos, soporte y activacion     |

Estas hipotesis deben validarse con cohortes reales. El evento de perdida puede
crear conversion alta, pero no debe usarse para justificar patrones permanentes
sin medir retencion despues de la emergencia.

### 11.7 Gastos que no deben ocultarse

- **Payout y custodia:** una recompensa o reserva puede mover dinero de terceros
  aunque la plataforma no contabilice ese monto como ingreso.
- **Impuestos y factura:** el precio publicado no es margen neto.
- **Soporte de seguridad:** disputas, reportes falsos, privacidad y moderacion
  requieren personas y evidencia.
- **Hardware:** GPS, tags, lectores RFID y quioscos tienen costo de compra,
  importacion, garantia y reemplazo.
- **Ventas B2B:** los contratos municipales y clinicos tienen ciclos de meses,
  no conversion inmediata.
- **CAC:** alianzas y eventos pueden reducir CAC, pero tienen costo de campo.

### 11.8 Fuentes de costos y validacion

- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) —
  cotizar SQL, App Service, Storage, Key Vault y observabilidad.
- [WhatsApp Business Platform Pricing](https://business.whatsapp.com/products/platform-pricing) —
  validar conversaciones, plantillas y pais/categoria.
- [SENASA Costa Rica](https://www.senasa.go.cr/tramites-y-servicios/tarifas) —
  separar tarifas oficiales de precios NALA.
- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs` —
  precios tecnicos del producto.
- `docs/pricing.md` — supuestos internos de ingresos y lineas futuras; no todos
  estan aprobados ni activos.

## 12. Fuentes y calidad de evidencia

### Fuentes internas

- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionPricing.cs` — precios tecnicos.
- `backend/src/PawTrack.Domain/Subscriptions/SubscriptionTier.cs` — tiers.
- `backend/src/PawTrack.Domain/ServiceProviders/ProviderMembershipTier.cs` y `ServiceProvider.cs` — tiers y trial tecnico.
- `docs/precios.md` — catalogo comercial y supuestos existentes.
- `docs/B2B_ESTADO_ACTUAL.md` — estado B2B/B2G verificado.
- `docs/NALA.md` — vision, flujo de recuperacion y capacidades de producto.

### Fuentes publicas de contexto

- [SENASA Costa Rica](https://www.senasa.go.cr/) — autoridad, tramites, oficinas, bienestar animal y programas nacionales.
- [SENASA: tarifas y tramites](https://www.senasa.go.cr/tramites-y-servicios/tarifas) — referencia para no confundir precios de PawTrack con tarifas oficiales.
- [INEC Costa Rica](https://www.inec.cr/) — estadisticas oficiales, poblacion, territorio y metodologia.
- [Geoportal INEC](https://geoportal.inec.go.cr/) — referencia para segmentacion geografica por canton/distrito.

Las cifras de hogares con mascotas, clinicas y mascotas perdidas que aparecen en
otros documentos internos deben pasar por validacion primaria antes de usarse en
material de inversion, publicidad o ventas.

## 13. Decisiones pendientes

1. Elegir corredor geografico y nodos ancla para el piloto.
2. Definir la fuente oficial o metodologia de las cifras de mercado.
3. Aprobar si `Verified`/`Featured` tendran precio separado o un solo plan.
4. Aprobar comision, impuestos, payout, reembolsos y responsabilidad del
   marketplace.
5. Definir SLA de soporte para clinicas, municipalidades y proveedores.
6. Medir conversion y retencion antes de aumentar gasto de adquisicion.

## Conclusion

NALA tiene una posicion defendible si se presenta como **infraestructura local de
recuperacion y cuidado**, no como otro directorio ni como otro collar GPS. El
benchmark favorece un lanzamiento concentrado, con evidencia de reunificaciones
y primeras reservas, antes de vender cobertura nacional o precios B2B como si
ya fueran contratos validados.
