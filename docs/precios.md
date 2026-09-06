# PawTrack CR — Estrategia de Precios

> Versión: 2026-09-06 | Moneda: Colones costarricenses (₡) | Referencia de cambio: ~520 ₡/USD
> Fuente de verdad: `SubscriptionPricing` en el backend.

---

## Contexto de mercado

La fuente de verdad comercial del producto actual no es un documento histórico ni una tabla duplicada en varios archivos; es el enum y pricing del backend (`SubscriptionTier` + `SubscriptionPricing`). Por ese motivo, los tiers activos son los que aparecen en código, no planificaciones históricas con estados básicos duplicados.

### Competidores globales identificados

| Plataforma         | País   | Modelo                | Precio base                | Diferenciador                     |
| ------------------ | ------ | --------------------- | -------------------------- | --------------------------------- |
| **PawBoost**       | EE.UU. | Freemium + ads        | Gratis / boost pagado      | Red social + Facebook Ads         |
| **PetLink**        | EE.UU. | One-time + GPS $45.99 | Registro vitalicio gratis  | Microchip registry + GPS hardware |
| **FidoFinder**     | EE.UU. | Freemium              | Gratis / featured listings | AI matching, shelter network      |
| **TabCat**         | UK     | Hardware              | £59 kit                    | RFID tracker de corto alcance     |
| **Lost My Doggie** | EE.UU. | Por uso               | $14.95/alerta              | Robocall a vecinos                |

### Vacío en el mercado latinoamericano

Ninguna plataforma ofrece hoy en Costa Rica o Centroamérica:

- **Integración con WhatsApp** como canal primario (penetración >95% en CR)
- **SINPE Móvil** como método de pago nativo
- **Portal para municipalidades** con gestión de animales capturados
- **Red de aliados verificados** (refugios, veterinarias, seguridad privada)
- **Certificados veterinarios PDF** verificables con QR
- **Sala de coordinación en tiempo real** durante búsquedas activas

---

## Segmentos de clientes

```
┌─────────────────────────────────────────────────────────────┐
│  B2C — Dueños de mascotas          ~890,000 hogares en CR   │
│  B2B — Clínicas veterinarias       ~600 clínicas activas    │
│  B2B — Aliados (refugios, etc.)    ~200 organizaciones      │
│  B2G — Municipalidades             82 cantones              │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Planes para Dueños de Mascotas (B2C)

### 📦 Explorador — Gratis

> Para quienes dan sus primeros pasos en la protección digital de mascotas.

**Mascotas y perfil**

- ✅ 1 mascota registrada
- ✅ Placa QR de identidad digital (URL pública)
- ✅ Perfil público con foto, nombre, raza, especie
- ✅ Registro de microchip RFID (ISO 11784)
- ✅ Historial de escaneos: últimos 5 registros
- ✅ Reactivar mascota (Reunida → Activa)

**Emergencia**

- ✅ Reporte de mascota perdida con GPS y foto
- ✅ Aparición en mapa público de casos activos
- ✅ Registro de avistamientos (reportes anónimos)
- ✅ Perfil de emergencia con mensaje público
- ✅ Contacto de rescatador con protección de privacidad
- ✅ Recompensa declarada con badge (CRC)
- ✅ Búsqueda por IA con foto (3 búsquedas/mes)
- ✅ Alertas en radio de 3 km

**Comunidad**

- ✅ Acceso al mapa público de mascotas perdidas
- ✅ Reportar avistamiento de mascotas ajenas
- ✅ Explorar mapa sin cuenta

**Limitaciones del plan gratuito**

- ✗ Solo 1 mascota
- ✗ Historial limitado (5 escaneos)
- ✗ Radio de alertas 3 km
- ✗ Sin alertas SMS/WhatsApp instantáneas
- ✗ Sin predicción de movimiento por IA
- ✗ Sin sala de coordinación activa
- ✗ Sin historial GPS de collar

---

### 🌟 Plus — ₡2,990/mes (~$5.75 USD)

> Para dueños activos que quieren la máxima velocidad de recuperación.

**Todo lo del plan Explorador, más:**

**Mascotas**

- ✅ Hasta 3 mascotas registradas
- ✅ Historial de escaneos ilimitado
- ✅ Exportar historial de actividad

**Alertas y búsqueda**

- ✅ Radio de alertas ampliado: 10 km
- ✅ Alertas instantáneas por **WhatsApp**
- ✅ Búsqueda por foto con IA — **ilimitada**
- ✅ Predicción de movimiento por IA
- ✅ Sala de coordinación activa (Case Room)
- ✅ Coordinación de búsqueda en tiempo real (zonas)
- ✅ 3D Radar de búsqueda

**Sistema de recompensas**

- ✅ Crear recompensa (Bounty) con SINPE
- ✅ Estado en mapa público: recompensa activa
- ✅ Flujo HandoverCode → liberación de pago

**GPS**

- ✅ Tab GPS en perfil de mascota
- ✅ Conectar collar GPS (Tractive, Kippy, genérico)
- ✅ Historial de trayectoria por rango de fechas
- ✅ Posición en tiempo real
- ✅ Alertas de conectividad (offline) y batería baja
- ✅ Modo perdido (búsqueda intensiva)
- ✅ Zonas seguras (geofencing)
- ✅ Transferencia segura del collar entre dueños

---

### 👨‍👩‍👧‍👦 Familia — ₡4,990/mes (~$9.60 USD)

> Para familias con varias mascotas o que necesitan acceso compartido.

**Todo lo del plan Plus, más:**

**Mascotas y usuarios**

- ✅ Mascotas **ilimitadas**
- ✅ Multi-usuario: hasta 5 miembros de familia
- ✅ Perfil compartido por mascota

**Historial médico**

- ✅ Registro de vacunas y desparasitación
- ✅ Registro de visitas veterinarias
- ✅ Recordatorios automáticos (citas, vacunas)
- ✅ Exportar historial médico en PDF

**Alertas**

- ✅ Radio de alertas **sin límite geográfico**
- ✅ Alertas push en dispositivos de todos los miembros

**Soporte**

- ✅ Soporte prioritario
- ✅ Acceso anticipado a nuevas features

---

### Comparativa B2C

| Feature                     | Explorador |   Plus ₡2,990    | Familia ₡4,990 |
| --------------------------- | :--------: | :--------------: | :------------: |
| Mascotas                    |     1      |        3         |   Ilimitadas   |
| Historial escaneos          | 5 últimos  |    Ilimitado     |   Ilimitado    |
| Radio alertas               |    3 km    |      10 km       |   Sin límite   |
| WhatsApp instantáneo        |     ✗      |        ✅        |       ✅       |
| Búsqueda IA por foto        |   3/mes    |    Ilimitada     |   Ilimitada    |
| Predicción movimiento IA    |     ✗      |        ✅        |       ✅       |
| Case Room (coordinación)    |     ✗      |        ✅        |       ✅       |
| GPS collar (tab)            |     ✗      |        ✅        |       ✅       |
| Sistema Bounty (recompensa) |     ✗      |        ✅        |       ✅       |
| Multi-usuario               |     ✗      |        ✗         |     ✅ (5)     |
| Expediente médico (preview) |     ✗      | ✅ (3 registros) |       ✅       |
| Expediente médico completo  |     ✗      |        ✗         |       ✅       |
| PDF historial médico        |     ✗      |        ✗         |       ✅       |
| Usuarios en familia         |     1      |        1         |       5        |

---

## 2. Planes para Clínicas Veterinarias (B2B)

> Facturación mensual. Contrato sin permanencia mínima.  
> Clínica = entidad SENASA registrada con licencia veterinaria activa.

### 🏥 Afiliada Básica — Gratis

> Para clínicas que quieren aparecer en el directorio y escanear pacientes.

**Directorio y visibilidad**

- ✅ Perfil en directorio público de clínicas PawTrack
- ✅ Mapa de clínicas (posición estándar)
- ✅ Información de contacto pública

**Herramientas operativas**

- ✅ Escanear código QR de collar (identificación de mascota)
- ✅ Escanear microchip RFID (identificación vía chip)
- ✅ Ver perfil público y datos del dueño (si mascota perdida)
- ✅ Búsqueda de mascota por número de microchip

**Límites**

- ✗ Sin posición destacada en mapa
- ✗ Sin badge "Clínica Verificada"
- ✗ Sin estadísticas de escaneos
- ✗ Sin certificados PDF
- ✗ Sin logo en alertas de pérdida

---

### ⭐ Clínica Plus — ₡15,000/mes (~$29 USD)

> Para clínicas que buscan diferenciarse y captar nuevos clientes.

**Todo lo de Básica, más:**

**Visibilidad premium**

- ✅ Posición **destacada** en mapa de clínicas
- ✅ Badge "Clínica Verificada" en directorio y alertas
- ✅ Logo de la clínica en **alertas de pérdida** cercanas
- ✅ Banner en Case Rooms de pacientes activos

**Analytics**

- ✅ Estadísticas de escaneos mensuales
- ✅ Métricas de visibilidad en directorio

**Soporte**

- ✅ Soporte prioritario por email
- ✅ Capacitación de equipo (onboarding)

---

### 🤝 Clínica Partner — ₡35,000/mes (~$67 USD)

> Para clínicas líderes que quieren el máximo retorno y herramientas avanzadas.

**Todo lo de Plus, más:**

**Certificación digital**

- ✅ Emisión de **certificados veterinarios PDF** (QuestPDF)
  - Vacunación, examen general, desparasitación, esterilización
  - Código de verificación único (QR en documento)
  - Verificación pública en `pawtrack.cr/verificar/{código}`
  - Firma digital de clínica y médico veterinario

**Integraciones**

- ✅ Widget embebible para sitio web propio
- ✅ Acceso a API de consulta directa (microchip, perfil mascota)
- ✅ Integración microchip RFID avanzada (lectores externos)

**Soporte y visibilidad**

- ✅ Soporte prioritario 24/7
- ✅ Gerente de cuenta dedicado
- ✅ Notificaciones en todos los Case Rooms del cantón
- ✅ Primeros resultados en búsquedas por zona

---

### Comparativa B2B Clínicas

| Feature                          | Básica | Plus ₡15k |  Partner ₡35k  |
| -------------------------------- | :----: | :-------: | :------------: |
| Directorio público               |   ✅   |    ✅     |       ✅       |
| Escanear QR / RFID               |   ✅   |    ✅     |       ✅       |
| Búsqueda por microchip           |   ✅   |    ✅     |       ✅       |
| Posición destacada en mapa       |   ✗    |    ✅     |       ✅       |
| Badge "Clínica Verificada"       |   ✗    |    ✅     |       ✅       |
| Logo en alertas de pérdida       |   ✗    |    ✅     |       ✅       |
| Estadísticas de escaneos         |   ✗    |    ✅     |       ✅       |
| Certificados PDF verificables    |   ✗    |     ✗     |       ✅       |
| API de consulta directa          |   ✗    |     ✗     |       ✅       |
| Widget embebible                 |   ✗    |     ✗     |       ✅       |
| Soporte prioritario              |   ✗    |   Email   | 24/7 + gerente |
| Comisión patrocinador plataforma |   ✗    |     ✗     |       ✅       |

---

## 3. Red de Aliados (B2B — Sin costo, verificación requerida)

> Refugios, negocios pet-friendly, seguridad privada, municipalidades como aliados operativos.  
> **Acceso gratuito** — el valor está en el impacto comunitario y visibilidad.

### ✅ Aliado Verificado — Gratis

**Proceso de incorporación**

- Solicitud de verificación con datos de organización
- Declaración de tipo: Veterinaria / Refugio / Comercio pet-friendly / Seguridad / Municipalidad
- Mapa de cobertura con radio declarado
- Aprobación manual por equipo PawTrack (1-2 días hábiles)

**Herramientas operativas (tras verificación)**

- ✅ Bandeja operativa de alertas dentro de la zona declarada
- ✅ Dashboard KPI: alertas recibidas, tasa de respuesta, radio cubierto
- ✅ Confirmación de acción en campo ("Ya buscamos en nuestra área")
- ✅ Perfil en red de aliados (próximamente: directorio público)

**Impacto en la plataforma**

- ✅ Cada aliado amplifica el radio efectivo de búsqueda
- ✅ Aliados con mayor tasa de respuesta reciben más alertas prioritarias

---

## 4. Licencias Institucionales — Municipalidades (B2G)

> Para gobiernos locales y unidades de control animal de cantones costarricenses.  
> Facturación **anual**. Incluye onboarding y capacitación.

### 🏛️ Municipal Básica — ₡150,000/año (~$288 USD)

- ✅ Portal de control animal municipal (acceso multiusuario)
- ✅ Registro digital de animales capturados
- ✅ Gestión de estados: Recibido / Dueño localizado / Transferido / Adoptado / Liberado
- ✅ Búsqueda y filtro por cantón y estado
- ✅ Enlace automático con mascotas registradas en PawTrack (por chip/QR)
- ✅ Reportes mensuales en PDF
- ✅ Acceso al mapa de mascotas perdidas del cantón
- ✅ Soporte por email

### 🏛️ Municipal Full — ₡300,000/año (~$577 USD)

**Todo lo de Básica, más:**

- ✅ API de consulta pública del registro municipal
- ✅ Dashboard en tiempo real de casos activos
- ✅ Estadísticas de recuperación por barrio y distrito
- ✅ Integración con alertas de pérdida del cantón
- ✅ Exportación para reportes SENASA / PANI
- ✅ SLA de disponibilidad 99.5%
- ✅ Soporte prioritario telefónico

### 🌐 Red Regional — ₡500,000/año (~$962 USD)

**Todo lo de Full, más:**

- ✅ Múltiples cantones bajo un solo contrato
- ✅ Capacitación presencial para equipo de bienestar animal
- ✅ Personalización de marca municipal en la plataforma
- ✅ Gerente de cuenta dedicado
- ✅ Integración con sistemas PANI y SENASA
- ✅ Acceso a API de consulta cruzada inter-cantonal

### Potencial de mercado Municipal

```
82 municipalidades en Costa Rica
5 contratos Básica = ₡750,000/año (~$1,440)
10 contratos Full  = ₡3,000,000/año (~$5,770)
2 contratos Red    = ₡1,000,000/año (~$1,923)
```

---

## 5. Sistema de Recompensas — Comisión por transacción

> No es un plan de suscripción. Es un **ingreso por transacción** sobre bounties activos.

| Evento                                 | Cargo                              |
| -------------------------------------- | ---------------------------------- |
| Crear bounty (depósito SINPE)          | Gratis                             |
| Liberación de recompensa al rescatador | **10% de comisión** sobre el monto |
| Reembolso al dueño (sin entrega)       | ₡1,000 cargo fijo                  |

**Ejemplo:** Dueño declara ₡25,000 de recompensa. Rescatador confirma entrega con HandoverCode. Rescatador recibe ₡22,500. PawTrack retiene ₡2,500.

---

## 6. Productos Físicos (e-commerce vía WhatsApp)

| Producto                                   | Precio estimado | Estado                |
| ------------------------------------------ | --------------- | --------------------- |
| Collar con placa QR grabada                | ₡4,500–₡8,000   | MVP: botón → WhatsApp |
| Placa QR standalone (llavero/sticker)      | ₡1,500–₡2,500   | MVP: botón → WhatsApp |
| Combo collar + placa + registro 1 año Plus | ₡14,990         | Planificado           |

---

## 7. Resumen de ingresos proyectados (escenario conservador, 12 meses)

> Basado en penetración de mercado costarricense en año 1.

| Línea de ingreso            | Objetivo usuarios | Precio           | ARR estimado                     |
| --------------------------- | ----------------- | ---------------- | -------------------------------- |
| Dueños Plus (₡2,990/mes)    | 500 suscriptores  | ₡2,990/mes       | **₡17,940,000**                  |
| Dueños Familia (₡4,990/mes) | 100 suscriptores  | ₡4,990/mes       | **₡5,988,000**                   |
| Clínicas Plus (₡15k/mes)    | 20 clínicas       | ₡15,000/mes      | **₡3,600,000**                   |
| Clínicas Partner (₡35k/mes) | 5 clínicas        | ₡35,000/mes      | **₡2,100,000**                   |
| Municipalidades (mix)       | 5 contratos       | ₡200,000 avg/año | **₡1,000,000**                   |
| Bounties (comisión 10%)     | 200 eventos       | ₡2,500 avg       | **₡500,000**                     |
| Productos físicos           | 300 ventas        | ₡5,000 avg       | **₡1,500,000**                   |
| **TOTAL ARR**               |                   |                  | **≈ ₡32,628,000** (~$62,746 USD) |

---

## 8. Decisiones de pricing — Justificación

### ¿Por qué el plan gratuito es tan generoso?

Costa Rica tiene ~890,000 hogares con mascotas. La masa crítica de mascotas registradas es lo que hace valioso el directorio, el mapa, y los alertas. Un plan gratuito robusto = crecimiento orgánico acelerado = más valor para los planes pagos.

### ¿Por qué ₡2,990/mes para Plus?

- Equivale a ~$5.75 USD — precio de una entrada al cine.
- PawBoost cobra $14.95/alerta puntual (sin suscripción). PawTrack Plus ofrece alertas ilimitadas por mes.
- Netflix en CR cuesta ₡9,900/mes. PawTrack Plus es 3x más barato que Netflix.
- Umbral de dolor mínimo para la clase media costarricense.

### ¿Por qué las clínicas pagan más que los dueños?

- El ROI es directo: una clínica verificada con posición destacada puede captar 5–10 nuevos clientes/mes.
- Un cliente nuevo = ₡30,000–₡80,000 en consultas anuales.
- ₡15,000/mes es <0.5% de la ganancia que genera un solo cliente nuevo.

### ¿Por qué no cobrar por el directorio de aliados?

- Los aliados son el motor de la red de búsqueda. Cobrarles reduce su participación y daña el producto.
- El modelo correcto: aliados gratis → más búsquedas exitosas → más reputación → más dueños pagos.

---

## 9. Comparativa vs competidores globales

| Feature                | PawTrack CR       | PawBoost          | PetLink           | FidoFinder         |
| ---------------------- | ----------------- | ----------------- | ----------------- | ------------------ |
| Precio base            | Gratis            | Gratis            | Gratis            | Gratis             |
| Radio de alertas       | 3–∞ km según plan | Nacional (EE.UU.) | Nacional (EE.UU.) | Configurable       |
| WhatsApp nativo        | ✅                | ✗                 | ✗                 | ✗                  |
| Bot conversacional     | ✅                | ✗                 | ✗                 | ✗                  |
| SINPE Móvil            | ✅                | ✗                 | ✗                 | ✗                  |
| Portal clínicas        | ✅                | ✗                 | Parcial           | ✗                  |
| Portal municipalidades | ✅                | ✗                 | ✗                 | Parcial (shelters) |
| Certificados PDF       | ✅ (Partner)      | ✗                 | ✗                 | ✗                  |
| GPS collar tab         | ✅ (Plus+)        | ✗                 | $45.99 hardware   | ✗                  |
| AI visual match        | ✅                | ✗                 | ✗                 | ✅ (limitado)      |
| Sala coordinación      | ✅ (Plus+)        | ✗                 | ✗                 | ✗                  |
| Sistema bounty         | ✅ (Plus+)        | ✗                 | ✗                 | ✗                  |
| Red de aliados         | ✅                | Rescue Squad      | ✗                 | Shelters           |
| Mercado objetivo       | Costa Rica 🇨🇷     | EE.UU./Global     | EE.UU.            | EE.UU.             |
| Enfoque local LATAM    | ✅                | ✗                 | ✗                 | ✗                  |

---

## 4. Tiendas de Mascotas B2B — Planes Store

| Plan / estado                    | Precio      | Capacidades                                             |
| -------------------------------- | ----------- | ------------------------------------------------------- |
| **StoreBasic** (base/directorio) | Gratis      | Listado en directorio + mapa, catálogo visible          |
| **StorePlus**                    | ₡12,000/mes | + Pedidos in-app SINPE + panel órdenes real-time        |
| **StorePartner**                 | ₡25,000/mes | + Analytics + multi-sucursal + badge verificado premium |

> `StoreBasic` es el estado gratuito de registro/directorio; los planes comerciales activos son `StorePlus` y `StorePartner`.

---

## 5. Vallas Publicitarias — Ingresos adicionales de la plataforma

El sistema de vallas permite a tiendas, clínicas y negocios anunciarse dentro de la app. Cuatro placements disponibles:

| Placement     | Dónde aparece                    | Audiencia                |
| ------------- | -------------------------------- | ------------------------ |
| **Map**       | Overlay en el mapa público       | Todos los visitantes     |
| **Dashboard** | Entre las mascotas del dueño     | Usuarios autenticados    |
| **Directory** | Top del directorio de tiendas    | Compradores potenciales  |
| **Feed**      | Sobre lista de mascotas perdidas | Alta visibilidad urgente |

**Modelo de negocio:** Tarifa negociada directamente. El Admin gestiona las vallas con fechas, prioridad (0-100) e imagen. Sin intermediarios externos.

---

## 10. Política de cambios y cancelación

| Regla                      | Detalle                                                   |
| -------------------------- | --------------------------------------------------------- |
| **Período de facturación** | Mensual, cobrado el primer día del ciclo                  |
| **Cancelación**            | En cualquier momento; acceso hasta fin del período pagado |
| **Cambio de plan**         | Inmediato; prorrateo automático                           |
| **Prueba gratuita**        | Plan Explorador siempre gratis — sin tarjeta              |
| **Reembolsos**             | No aplica para períodos ya usados                         |
| **Método de pago**         | SINPE Móvil (activo) · Stripe (próximamente)              |

_PawTrack CR · alianzas@pawtrack.cr · pawtrack.cr_  
_Precios en colones costarricenses (₡). Sujetos a cambio con 30 días de aviso previo._
