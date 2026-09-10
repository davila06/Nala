# PawTrack CR - Resumen preliminar para inversionistas

**Documento para potenciales inversionistas**  
**Fecha:** 2026-09-10
**Propuesta preliminar:** USD $2,000 por hasta 10% de participacion, sujeta a estructura legal, due diligence y acuerdo definitivo
**Valoracion indicativa pre-money:** USD $18,000
**Valoracion indicativa post-money:** USD $20,000

> Documento informativo sujeto a due diligence, estructura societaria y acuerdo
> legal. La participacion final debe formalizarse con asesoria legal en Costa
> Rica. Ninguna cifra de proyeccion es una garantia de retorno.

> **Tesis de inversión:** esta ronda no financia una idea sin producto. Financia
> la conversión de una plataforma funcional en evidencia comercial: usuarios
> activos, primeros clientes B2B, ingresos recurrentes verificables y un piloto
> territorial medible.

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
financiar una idea en papel: buscamos capital pequeño y disciplinado para
convertir una plataforma funcional en un lanzamiento territorial medible en
Costa Rica.

### Resumen ejecutivo

| Elemento           | Situación actual                                                                              |
| ------------------ | --------------------------------------------------------------------------------------------- |
| Producto           | Plataforma funcional con QR, recuperación, salud, GPS, adopciones, B2B y NALA                 |
| Etapa              | Validación comercial y piloto territorial; product-market fit aún no demostrado               |
| Monetización       | Tiers técnicos, activación manual y tarifas de vallas asignadas; billing automático pendiente |
| Capital solicitado | USD $2,000, liberado preferiblemente por hitos                                                |
| Uso del capital    | Piloto, onboarding, QA, soporte, ventas iniciales e infraestructura                           |
| Riesgo principal   | Convertir capacidad técnica en usuarios activos, contratos y revenue recurrente               |

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

## 6. Por que puede ganar

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

**USD $2,000 por 10% de participacion.**

Esto implica:

- valoracion post-money: USD $20,000;
- valoracion pre-money: USD $18,000;
- participacion ofrecida: 10% del proyecto, sujeta a la estructura legal que
  se formalice;
- no se promete dividendos, recompra ni retorno fijo.

La participacion exacta, derechos de informacion, gobierno, propiedad
intelectual y tratamiento de futuras rondas deben quedar en un acuerdo formal.

### Uso propuesto de fondos

| Uso                                                          | Monto aproximado |
| ------------------------------------------------------------ | ---------------: |
| Infraestructura Azure y base de datos por los primeros meses |           $1,200 |
| Contingencia operativa y picos de uso                        |             $240 |
| Soporte de lanzamiento, QA y ajustes de onboarding           |             $360 |
| Dominio, canales transaccionales y materiales comerciales    |             $200 |
| **Total**                                                    |       **$2,000** |

Los montos son presupuesto de trabajo, no cotizacion vinculante. El objetivo es
comprar tiempo operativo para validar el mercado, no financiar features sin
usuarios.

### Uso por hitos

| Hito   | Resultado esperado                                          | Liberación sugerida |
| ------ | ----------------------------------------------------------- | ------------------: |
| Hito 1 | Piloto en 1-2 cantones, onboarding y medición base          |              $1,000 |
| Hito 2 | Primeros clientes B2B, conversión B2C y reporte de métricas |              $1,000 |

Indicadores de liberación: Owners registrados y MAU, perfiles con QR activo,
primer avistamiento, clientes B2B activos, suscripciones pagadas, MRR, churn y
costo de soporte por caso.

### Presupuesto detallado de validación

| Partida            | Uso específico                                                    |      Monto |
| ------------------ | ----------------------------------------------------------------- | ---------: |
| Infraestructura    | Hosting, base de datos, almacenamiento, observabilidad y dominios |       $600 |
| Adquisición piloto | Contenido, campañas locales y activación de usuarios              |       $450 |
| Onboarding B2B     | Demos, materiales, desplazamientos y configuración inicial        |       $300 |
| QA y soporte       | Pruebas, correcciones, soporte de casos y moderación inicial      |       $350 |
| Legal/comercial    | Revisión de inversión, privacidad, contratos y billing            |       $200 |
| Contingencia       | Picos de uso e incidentes                                         |       $100 |
| **Total**          |                                                                   | **$2,000** |

### Escenario financiero orientativo

El escenario base de [consolidado.md](consolidado.md) proyecta ingresos de
₡51.54M en el primer año, ₡116.67M en el año 2 y ₡625.21M en el año 5, con
EBITDA orientativo de 18.5%, 24.6% y 31.2%, respectivamente. Son escenarios de
planificación, no resultados actuales ni una promesa de retorno.

## 9. Plan de 90 dias

### Dias 1-30: activar un territorio

- elegir uno o dos cantones piloto;
- reclutar clinicas, refugios y aliados ancla;
- lanzar QR y finder sin login;
- medir registro, perfil completo, QR escaneado y primer avistamiento;
- ejecutar pruebas legales de privacidad, pagos y soporte.

### Dias 31-60: probar la red

- operar casos reales con protocolo de soporte;
- medir tiempo a primer avistamiento y reunificacion;
- entrevistar dueños, finders, clinicas y refugios;
- validar conversion de Free a Plus/Familia;
- medir activacion de clinicas y aliados.

### Dias 61-90: decidir escala

- publicar un reporte agregado del piloto;
- cerrar primeros clientes B2B/B2G si la evidencia lo respalda;
- calcular CAC, conversion, retencion, churn y costo por caso;
- decidir si expandir al siguiente canton o corregir el producto.

## 10. Metricas que definiran el exito

### Panel de métricas antes de enviar como pitch final

Estas métricas deben completarse con datos exportados del entorno real; no deben
rellenarse con proyecciones:

| Métrica                        |  Valor actual | Meta a 90 días |
| ------------------------------ | ------------: | -------------: |
| Owners registrados             | Por completar |          2,500 |
| MAU                            | Por completar |          1,300 |
| Suscripciones pagadas activas  | Por completar |            100 |
| MRR                            | Por completar |     ₡0.3-0.5 M |
| Clínicas activas               | Por completar |              3 |
| Refugios/aliados activos       | Por completar |              5 |
| Primeros anunciantes de vallas | Por completar |    1-2 pilotos |
| Casos con primer avistamiento  | Por completar | Medir baseline |

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

## 12. Que recibe el inversionista

- 10% de participacion conforme al acuerdo legal;
- reporte mensual de metricas durante los primeros 12 meses;
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

Por USD $2,000, el inversionista no esta apostando solo por una idea: esta
entrando en una plataforma funcional que necesita probar densidad, confianza y
ventas en un territorio concreto.

La pregunta no es si podemos construir otra app. Ya construimos la base.

La pregunta es si podemos convertir una mascota perdida en el punto de entrada
a una red local de identidad, cuidado y recuperacion.

**Buscamos un socio que ayude a ganar el primer territorio, medirlo con
honestidad y escalar solo lo que funcione.**

### Condiciones recomendadas antes de firmar

- Confirmar la sociedad receptora, propiedad intelectual, marca y dominio.
- Formalizar acciones, dilución, derechos de información y gobierno.
- Definir si la inversión será equity directo o instrumento convertible.
- Liberar el capital por hitos y entregar reporte mensual durante 12 meses.
- Validar legalmente cualquier futura custodia de fondos, recompensas o billing.

---

**Contacto:** Denis Avila - Fundador, PawTrack CR  
**Email:** davila06@gmail.com  
**Web:** https://pawtrack.cr

_Documento informativo. Requiere revision legal y financiera antes de
compartirse como oferta formal de participacion._
