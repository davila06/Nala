# PawTrack CR - Resumen preliminar para inversionistas

**Documento para potenciales inversionistas**  
**Fecha:** 2026-09-11
**Propuesta preliminar:** USD $2,000 por hasta 10% de participación para la salida LIVE de una demo/piloto de 6 meses hasta alcanzar el punto de equilibrio (proyección conservadora)
**Valoracion indicativa pre-money:** USD $18,000
**Valoracion indicativa post-money:** USD $20,000

> Documento informativo sujeto a due diligence, estructura societaria y acuerdo
> legal. La participacion final debe formalizarse con asesoria legal en Costa
> Rica. Ninguna cifra de proyeccion es una garantia de retorno.

> **Tesis de inversión:** esta ronda no financia una idea sin producto. Financia
> la salida a producción (LIVE) de una plataforma funcional durante un periodo de
> demo operativa de 6 meses. El objetivo es ejecutar marketing, adquirir dominios
> y licencias, aprovisionar servicios cloud en Azure y lograr el punto de equilibrio
> financiero bajo el escenario de proyección más bajo.

> **Uso recomendado:** este documento sirve para iniciar conversaciones. No es
> una oferta pública, promesa de rendimiento ni contrato de inversión.

---

## 1. El pitch en 30 segundos

Cuando una mascota se pierde, la familia no necesita otra red social: necesita
identidad, velocidad y coordinacion.

**PawTrack CR** es una plataforma web progresiva que conecta el QR de una
mascota con una red local de dueños, encontradores, clinicas, refugios,
municipalidades y servicios. Permite identificar, reportar, coordinar y
reunificar sin obligar a quien encuentra una mascota a instalar una app.

El producto ya tiene una base tecnica amplia. Lo que buscamos ahora no es
financiar una idea en papel: buscamos un capital inicial de $2,000 USD para ir
**LIVE** en una demo operativa de 6 meses, cubriendo marketing, dominios, licencias
y servicios cloud hasta alcanzar el punto de equilibrio con la proyección más baja.

### Resumen ejecutivo

| Elemento           | Situación actual                                                                                          |
| ------------------ | --------------------------------------------------------------------------------------------------------- |
| Producto           | Plataforma funcional con QR, recuperación, salud, GPS, adopciones, B2B y NALA                             |
| Etapa              | Salida LIVE en producción; piloto territorial y demo comercial de 6 meses                                 |
| Monetización       | Tiers técnicos, activación manual y tarifas de vallas asignadas; billing automático pendiente             |
| Capital solicitado | USD $2,000 por hasta 10% de participación (liberado preferiblemente por hitos)                            |
| Uso del capital    | Marketing, compra de dominios y licencias, aprovisionamiento de servicios cloud, onboarding, QA y soporte |
| Meta financiera    | Alcanzar el punto de equilibrio operativo al mes 6 (incluso en la proyección más baja)                    |
| Riesgo principal   | Convertir capacidad técnica en usuarios activos, contratos y revenue recurrente                           |

### Qué está probado y qué falta probar

**Probado técnicamente:** producto funcional, arquitectura modular, gates de
planes, flujos de recuperación, adopciones, clínicas, collares, publicidad y
pruebas automatizadas documentadas.

**Por probar comercialmente:** conversión de usuarios, retención, disposición
de pago, CAC, churn, clientes B2B que renuevan y demanda real por vallas y
proveedores.

## 2. La oportunidad

El problema actual esta fragmentado:

- avisos que se pierden en grupos de redes sociales;
- contacto directo que expone telefonos y direcciones;
- poca coordinacion entre dueños, rescatistas y clinicas;
- expedientes y documentos medicos dispersos;
- municipalidades y refugios operando sin una capa comun de trazabilidad.

Costa Rica ofrece un mercado inicial concentrado y manejable: dueños de
mascotas, clinicas veterinarias, refugios, tiendas, proveedores de servicios y
municipalidades dentro de un mismo territorio.

Las cifras de hogares, mascotas y establecimientos que aparecen en documentos
internos son hipotesis de mercado y deben validarse comercialmente. No se
presentan aqui como usuarios actuales ni como ventas garantizadas.

## 3. La solucion

PawTrack convierte un incidente aislado en un flujo operativo:

1. registrar mascota y generar QR;
2. mostrar un perfil publico minimo y seguro;
3. activar perdida y notificar a la red;
4. recibir avistamientos con ubicacion y foto;
5. coordinar busqueda en mapa en tiempo real;
6. conectar por chat enmascarado;
7. verificar la entrega con un codigo seguro;
8. conservar salud, actividad y evidencia con consentimiento.

La persona que escanea un QR puede ayudar desde el navegador. Esa friccion baja
es central para el modelo: el finder no tiene que convertirse primero en
usuario para aportar valor a la red.

## 4. Producto construido

La implementacion actual incluye, entre otras capacidades:

- identidad digital, QR, microchip y perfil publico;
- reportes de perdida, avistamientos y mapa;
- Case Room y coordinacion SignalR;
- matching visual por IA;
- chat enmascarado y handover seguro;
- broadcast por email, WhatsApp, Telegram y Facebook;
- aliados, refugios, adopciones y ferias;
- clinicas, expediente medico, consentimiento y certificados verificables;
- collares GPS, modo perdido, geofencing, bateria, auditoria y handover;
- tiendas, catalogo y solicitudes de pedido;
- proveedores de servicios, disponibilidad y reservas;
- municipalidades, capturas y reportes institucionales;
- suscripciones, MFA, exportacion y retencion de datos.

Estas capacidades estan implementadas en codigo y pruebas, pero no todas son
productos comerciales ya validados. El siguiente paso es medir uso real,
conversion, retencion y calidad de recuperacion. La referencia consolidada de
roles, planes, precios tecnicos, propuestas y limites comerciales es
[consolidado.md](consolidado.md).

## 5. Evidencia tecnica

Al corte de la última evidencia registrada en [STATUS.md](STATUS.md), el repo
registra:

- build backend correcto;
- suite backend unitaria e integración reportada como correcta en el corte documentado;
- typecheck frontend correcto;
- tests frontend y build de produccion correctos;
- lint frontend sin errores ni warnings;
- escenario E2E principal de recuperacion validado contra stack local real.

Los conteos deben revalidarse en CI antes de circular este documento como pitch.
La evidencia tecnica demuestra capacidad de construccion y una base de calidad.
No demuestra aun product-market fit, ingresos recurrentes ni escala productiva.

## 6. Por qué puede ganar

### Red territorial

La ventaja no es el QR aislado. Es la densidad de relaciones y eventos entre
mascota, familia, finder, clinica, refugio, municipalidad y evidencia temporal
/geografica.

### Adaptacion local

- WhatsApp como canal natural;
- SINPE como referencia de pago local;
- español y contexto costarricense;
- cantones como unidades concretas de lanzamiento;
- integracion entre comunidad, salud y entidades publicas.

### Confianza por diseño

El sistema incorpora contacto enmascarado, ownership, scopes, grants medicos,
auditoria, rate limiting, retencion y MFA para roles privilegiados. La
privacidad no es un texto de marketing: es parte de la arquitectura.

### Plataforma, no feature aislada

El mismo perfil de mascota puede abrir recuperacion, salud, adopcion, clinica,
collar, tienda y servicios. Cada interaccion util puede aumentar el valor de la
red sin obligar a lanzar productos separados.

### Lo que compra el inversionista

La oportunidad combina tres activos:

1. **Producto operativo:** QR, recuperación, salud, GPS, adopciones, clínicas,
   tiendas, proveedores, municipalidades y analítica institucional.
2. **Monetización escalonada:** suscripciones B2C, planes B2B/B2G, inventario de
   vallas con tarifas de referencia asignadas y futuras membresías de proveedores.
3. **Evidencia medible:** activación, densidad territorial, conversión,
   retención, costo por caso y revenue por segmento.

## 7. Modelo de negocio

### B2C

| Plan          | Precio tecnico actual | Valor                                         |
| ------------- | --------------------: | --------------------------------------------- |
| `Free`        |                    ₡0 | 1 mascota, QR y funciones base                |
| `UserPlus`    |            ₡2,990/mes | hasta 3 mascotas, GPS, IA y coordinacion      |
| `UserFamilia` |            ₡4,990/mes | mascotas ilimitadas, familia y salud completa |

`UserPlus` y `UserFamilia` aceptan plazos de 1, 3, 6 o 12 meses; solo el plazo
anual aplica 20% de descuento. La contratación se solicita y se activa
manualmente después de verificar SINPE; no se presenta como checkout recurrente
universal.

### B2B/B2G

| Segmento        | Oferta tecnica                                                       |
| --------------- | -------------------------------------------------------------------- |
| Clinicas        | `ClinicPlus` ₡15,000/mes; `ClinicPartner` ₡35,000/mes                |
| Tiendas         | `StorePlus` ₡12,000/mes; `StorePartner` con gates tecnicos avanzados |
| Refugios        | `ShelterPlus` ₡8,000/mes                                             |
| Municipalidades | tiers anuales de ₡150,000 a ₡500,000                                 |
| Proveedores     | prueba `Verified` de 30 dias; pricing pendiente de validacion        |

Adicionalmente existen oportunidades en publicidad contextual, bundles GPS y
recompensas. No se presentan como ingresos consolidados. Tiendas actualmente
no tienen checkout o intermediacion de pagos por PawTrack; las recompensas
requieren validacion legal y operativa antes de escalar custodia de fondos.

### Escalera de monetización

| Etapa | Motor                        | Estado                             | Validación buscada                     |
| ----- | ---------------------------- | ---------------------------------- | -------------------------------------- |
| 1     | Plus/Familia                 | Precio técnico y activación manual | Conversión, ARPU y churn               |
| 2     | Clínicas, tiendas y refugios | Tiers/gates implementados          | Primeros contratos y retención B2B     |
| 3     | Municipalidades              | Tiers técnicos anuales             | Validar ciclo institucional y ticket   |
| 4     | Vallas                       | Tarifas de referencia asignadas    | Primeros anunciantes, CTR y renovación |
| 5     | Proveedores                  | Prueba técnica de 30 días          | Validar willingness-to-pay             |

El plan es validar primero densidad territorial y suscripciones, y luego
convertir vallas y proveedores en ingresos adicionales.

## 8. La ronda

### La solicitud

**USD $2,000 por hasta 10% de participacion.**

Esto implica:

- valoracion post-money: USD $20,000;
- valoracion pre-money: USD $18,000;
- participacion ofrecida: hasta 10% del proyecto, sujeta a la estructura legal que
  se formalice;
- destino del capital: financiar la salida **LIVE en producción para un periodo de demo/piloto operativo de 6 meses**, horizonte en el cual se proyecta alcanzar el **punto de equilibrio financiero (break-even)** bajo el escenario de ingresos más conservador;
- no se promete dividendos, recompra ni retorno fijo.

La participacion exacta, derechos de informacion, gobierno, propiedad
intelectual y tratamiento de futuras rondas deben quedar en un acuerdo formal.

### Uso propuesto de fondos

El capital de $2,000 USD se utilizará exclusivamente para llevar la solución a entorno **LIVE** y operarla de manera continua durante los 6 meses de la demo:

| Categoría                           | Uso específico                                                                                                                       | Monto ($ USD) |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ | ------------: |
| **Marketing y adquisición**         | Pauta digital local (Meta/WhatsApp), campañas de lanzamiento en cantones piloto y material comercial                                 |          $650 |
| **Aprovisionamiento cloud (Azure)** | Hosting App Service Linux, Azure SQL Database, Blob Storage, App Insights, CDN y Key Vault (6 meses 24/7)                            |          $600 |
| **Dominios y licencias**            | Compra/renovación del dominio oficial (`pawtrack.cr`), certificados SSL, licencias y conectores transaccionales (WhatsApp API/email) |          $250 |
| **Onboarding B2B, QA y soporte**    | Integración de clínicas, refugios y tiendas, pruebas de calidad en vivo y atención inicial de soporte operativo                      |          $350 |
| **Contingencia y legal**            | Fondo para picos de tráfico, incidentes y formalización legal del vehículo de inversión                                              |          $150 |
| **Total**                           |                                                                                                                                      |    **$2,000** |

Los montos son un presupuesto de trabajo disciplinado para comprar tiempo operativo y alcance comercial, no para financiar desarrollo de software sin usuarios.

### Uso por hitos (6 meses de demo LIVE)

| Hito                   | Resultado esperado                                                                                                    | Liberación sugerida |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------- | ------------------: |
| **Hito 1 (Meses 1-3)** | Aprovisionamiento cloud, dominios, licencias, lanzamiento LIVE, marketing en 1-2 cantones y primeros usuarios pagados |              $1,000 |
| **Hito 2 (Meses 4-6)** | Adquisición B2C/B2B sostenida, optimización de conversión y alcance del punto de equilibrio (break-even) en el mes 6  |              $1,000 |

Indicadores de liberación: Owners registrados, MAU, perfiles con QR activo, casos reales atendidos, suscripciones pagadas B2C y B2B, MRR y costo de soporte por caso.

### Análisis del Punto de Equilibrio (Break-even a 6 meses)

- **Costos fijos recurrentes**: Los costos de infraestructura cloud en Azure y licencias esenciales suman aproximadamente **~$100–$150 USD/mes** (~₡50,000–₡75,000/mes) en etapa inicial.
- **Ingreso requerido para break-even**: Se requiere un MRR de ~₡150,000–₡200,000/mes para cubrir el 100% de los costos operativos recurrentes.
- **Unidades necesarias**: Esto equivale a únicamente **~30 a 40 clientes activos pagados** combinando B2C y B2B (por ejemplo: 25 usuarios `UserPlus`/`UserFamilia` + 2 clínicas `ClinicPlus` o tiendas `StorePlus`).
- **Proyección más baja / conservadora**: Incluso en la hipótesis de adquisición más modesta (3-5 conversiones B2C/mes y 1 aliado B2B cada 2 meses), el volumen acumulado al **Mes 6** permite cubrir holgadamente los costos recurrentes, logrando la autosostenibilidad operativa antes de agotar el capital de la demo.

### Escenario financiero orientativo

El escenario base de [consolidado.md](consolidado.md) proyecta ingresos de
₡51.54M en el primer año, ₡116.67M en el año 2 y ₡625.21M en el año 5, con
EBITDA orientativo de 18.5%, 24.6% y 31.2%, respectivamente. Son escenarios de
planificación, no resultados actuales ni una promesa de retorno.

## 9. Plan de 6 meses: Demo LIVE a Punto de Equilibrio

### Meses 1-2: Aprovisionamiento y Lanzamiento LIVE

- Adquirir dominio oficial (`pawtrack.cr`), SSL y licencias de conectores transaccionales;
- Aprovisionar infraestructura en Azure (App Service, Azure SQL, Blob Storage, App Insights);
- Desplegar la demo en producción (LIVE) e iniciar campañas de marketing digital local en 1-2 cantones piloto;
- Reclutar e integrar las primeras clínicas veterinarias, refugios y aliados ancla.

### Meses 3-4: Adquisición y Tracción Comercial

- Escalar pauta publicitaria en Meta/WhatsApp y dinámicas en comunidades locales;
- Operar la red con casos reales de pérdida, avistamiento y coordinacion en tiempo real;
- Medir conversión de usuarios `Free` a planes pagados (`UserPlus` / `UserFamilia`) y cerrar primeros contratos B2B;
- Ajustar onboarding y canal de soporte en función del feedback directo.

### Meses 5-6: Consolidación y Punto de Equilibrio

- Publicar reporte consolidado con métricas de uso, tiempo de reunificación y retención (MRR);
- Alcanzar el punto de equilibrio financiero (break-even) bajo la proyección más conservadora;
- Evaluar métricas de unit economics (CAC, LTV, Churn, Costo por caso);
- Presentar resultados a los inversionistas y decidir la estrategia de expansión geográfica.

## 10. Métricas que definirán el éxito

### Panel de métricas para la demo LIVE (6 meses)

Estas métricas deberán completarse con datos reales extraídos de producción a lo largo de la demo:

| Métrica                                   |  Valor actual | Meta al Mes 6 (Break-even) |
| ----------------------------------------- | ------------: | -------------------------: |
| Owners registrados                        | Por completar |                      2,500 |
| MAU (Usuarios activos mensuales)          | Por completar |                      1,300 |
| Suscripciones pagadas activas (B2C + B2B) | Por completar |                    40 - 50 |
| MRR (Ingreso Mensual Recurrente)          | Por completar |        ₡150,000 - ₡250,000 |
| Clínicas activas (B2B)                    | Por completar |                      3 - 5 |
| Refugios/aliados activos                  | Por completar |                          5 |
| Primeros anunciantes de vallas            | Por completar |                1-2 pilotos |
| Casos con primer avistamiento             | Por completar |             Medir baseline |

El documento no debe circular como evidencia de tracción hasta sustituir
“Por completar” con datos verificables y fecha de corte.

La north star es **mascotas activas protegidas que completan una interaccion
verificable en los ultimos 90 dias**.

El tablero del piloto debe medir:

- registros a perfiles completos;
- perfiles con QR activado y escaneado;
- tiempo a primer avistamiento;
- tiempo a reunificacion;
- reportes validos y falsos positivos;
- usuarios Free a planes pagados;
- clinicas, aliados y refugios activos por canton;
- retencion a 30, 90 y 180 dias;
- costo de soporte por caso;
- incidentes de privacidad, fraude y abuso.

No usamos como traccion actual la cifra historica de 68% de recuperacion ni el
claim de menos de 72 horas: los documentos internos indican que no tienen
validacion externa ni muestra productiva suficiente.

## 11. Riesgos y como se reducen

| Riesgo                            | Mitigacion                                                               |
| --------------------------------- | ------------------------------------------------------------------------ |
| Red insuficiente en un canton     | lanzar con organizaciones ancla y densidad territorial                   |
| Sin usuarios o ingresos iniciales | piloto medible antes de expansion                                        |
| Claims regulatorios sobre SENASA  | presentar solo como preparado para trazabilidad, no como aval oficial    |
| Datos medicos y ubicacion         | consentimiento, minimizacion, grants, auditoria y retencion              |
| Pagos y recompensas               | no custodiar fondos ni prometer payouts sin asesoria legal               |
| Dependencia de WhatsApp/Azure     | email, web y canales alternos; presupuesto de contingencia               |
| Soporte 24/7                      | runbook, severidades, escalamiento y SLA progresivo                      |
| Tarifas B2B/vallas sin conversión | venta consultiva, piloto por hitos y no reconocer revenue hasta cobrar   |
| Monetización manual inicial       | automatizar billing solo después de validar demanda y requisitos legales |

## 12. Qué recibe el inversionista

- Hasta 10% de participación conforme al acuerdo legal;
- reporte mensual de métricas durante los primeros 12 meses;
- acceso a demos y avances del piloto;
- informacion razonable para due diligence;
- reconocimiento como inversionista fundador, si asi se acuerda;
- acceso preferente a conversaciones de futuras rondas, sin garantia de
  condiciones ni valoracion.

No se promete retorno fijo, recompra, dividendos ni exclusividad territorial.

## 13. Due diligence disponible

Antes de firmar, un inversionista puede solicitar revisar:

- demo funcional;
- arquitectura y repositorio bajo acuerdo de confidencialidad;
- resultados de pruebas y CI;
- modelo de planes y costos;
- estado de infraestructura;
- politica de privacidad y terminos;
- riesgos de datos, pagos y claims regulatorios;
- estructura legal de la participacion.

### Checklist previo a recibir fondos

- Sociedad o vehículo legal identificado.
- Propiedad de código, marca, dominio y activos confirmada.
- Cap table y porcentaje ofrecido documentados.
- Tipo de instrumento definido: equity directo o convertible.
- Derechos de información, dilución y futuras rondas acordados.
- Cuenta receptora y tratamiento tributario definidos.
- Due diligence técnica, legal y financiera documentada.

## 14. El cierre

Por USD $2,000, el inversionista no está apostando por una idea en papel: está
financiando la salida a producción (**LIVE**) de una plataforma 100% construida,
cubriendo 6 meses de marketing, compra de dominios y licencias, aprovisionamiento
cloud en Azure y operación comercial hasta alcanzar el punto de equilibrio financiero,
incluso bajo el escenario de proyección de ingresos más conservador.

La pregunta no es si podemos construir la plataforma. Ya construimos la base tecnológica.

La pregunta es si podemos convertir una demo en vivo de 6 meses en un negocio
autosostenible y rentable que sirva de punto de entrada a la red nacional de identidad,
cuidado y recuperación animal.

**Buscamos un socio que nos acompañe a ir LIVE, alcanzar el punto de equilibrio en el mes 6 y escalar sobre bases financieras sólidas.**

### Condiciones recomendadas antes de firmar

- Confirmar la sociedad receptora, propiedad intelectual, marca y dominio.
- Formalizar acciones, dilución, derechos de información y gobierno.
- Definir si la inversión será equity directo o instrumento convertible.
- Liberar el capital por hitos y entregar reporte mensual de métricas durante los 6 meses del demo y el primer año.
- Validar legalmente cualquier futura custodia de fondos, recompensas o billing.

---

**Contacto:** Denis Avila - Fundador, PawTrack CR  
**Email:** davila06@gmail.com  
**Web:** https://pawtrack.cr

_Documento informativo. Requiere revision legal y financiera antes de
compartirse como oferta formal de participacion._
