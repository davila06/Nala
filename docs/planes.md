# PawTrack CR — Planes y precios

> Fuente de verdad: backend (`SubscriptionTier` y `SubscriptionPricing`).
> Revisión: 2026-09-06
> Estado: alineado con la implementación actual.

## 1. Resumen ejecutivo

La base real de planes implementada hoy es la siguiente:

- B2C: `Free`, `UserPlus`, `UserFamilia`
- Tiendas: `StorePlus`, `StorePartner`
- Refugios: `ShelterPlus`
- Clínicas: `ClinicPlus`, `ClinicPartner`
- Municipalidades: `MuniBasica`, `MuniFull`, `MuniRedRegional`

Los estados `ClinicBasic`, `StoreBasic` y `ShelterBasic` existen como marca libre o de directorio, pero no son la fuente final de billing ni de feature gating en la app actual. Es decir, el producto real se rige por los tiers activos pagados en producción y los free/public states no deben confundirse con planes de venta.

---

## 2. B2C — Dueños de mascotas

### Explorador — Gratis

- 1 mascota registrada
- Historial de escaneos limitado a 5 entradas
- Búsqueda visual por IA: 3 búsquedas/mes
- Reporte de pérdida, mapa público y avistamientos anónimos
- No incluye GPS ni funciones premium

### Plus — ₡2,990/mes

- Todo lo del plan Explorador
- Hasta 3 mascotas
- Historial de escaneos ilimitado
- IA ilimitada
- Alertas ampliadas y coordinación activa
- Tab GPS, integraciones de collar, Case Room, Bounty

### Familia — ₡4,990/mes

- Todo lo de Plus
- Mascotas ilimitadas
- Hasta 5 miembros en la cuenta familiar
- Historial médico completo
- Peso por visita, medicación estructurada, recordatorios, calendario y exportación PDF

---

## 3. B2B — Tiendas de mascotas

### StorePlus — ₡12,000/mes

- Catálogo público de productos
- Pedido in-app con SINPE Móvil
- Gestión de pedidos
- Badge / destaque base en directorio y mapa

### StorePartner — ₡25,000/mes

- Todo lo de StorePlus
- Analytics avanzados
- Multi-sucursal / multi-location
- Mejor posicionamiento y funciones premium de gestión

---

## 4. Refugios / adopciones

### ShelterPlus — ₡8,000/mes

- Publicación ilimitada de animales en adopción
- Ferias de adopción
- Pin destacado en mapa
- Gestión completa de solicitudes y panel del refugio

> El tier `ShelterBasic` es un límite libre de 5 animales activos; no es el plan de pago principal del sistema actual.

---

## 5. B2B — Clínicas veterinarias

### ClinicPlus — ₡15,000/mes

- Destacado en mapa y directorio
- Badge verificado
- Estadísticas de escaneos y métricas de visibilidad
- Certificados PDF verificables

### ClinicPartner — ₡35,000/mes

- Todo lo de ClinicPlus
- API keys para integración
- Widget embebible
- Endpoints especializados y funciones premium de proveedor

> El tier `ClinicBasic` aparece como un estado o descriptor de entrada, pero la app actual no usa ese nombre como flujo de pricing/activación en producción.

---

## 6. B2G — Municipalidades

### MuniBasica — ₡150,000/año

- Portal básico de gestión
- Un cantón

### MuniFull — ₡300,000/año

- Todo lo de Básica
- Fotos de animales capturados
- Estadísticas y reportes

### MuniRedRegional — ₡500,000/año

- Todo lo de Full
- Multi-cantón
- Red regional y dashboard consolidado

---

## 7. Pricing oficial vigente en código

| Tier              |   Precio | Modalidad |
| ----------------- | -------: | --------- |
| `UserPlus`        |   ₡2,990 | mensual   |
| `UserFamilia`     |   ₡4,990 | mensual   |
| `StorePlus`       |  ₡12,000 | mensual   |
| `StorePartner`    |  ₡25,000 | mensual   |
| `ShelterPlus`     |   ₡8,000 | mensual   |
| `ClinicPlus`      |  ₡15,000 | mensual   |
| `ClinicPartner`   |  ₡35,000 | mensual   |
| `MuniBasica`      | ₡150,000 | anual     |
| `MuniFull`        | ₡300,000 | anual     |
| `MuniRedRegional` | ₡500,000 | anual     |

---

## 8. Regla para documentación

Este documento es la fuente de verdad comercial del producto actual. Cualquier otro documento que describa precios o features debe alinearse con estos tiers y sus límites. Duplicar nombres, tarificaciones o planes fuera del código ha generado inconsistencias y debe corregirse si aparece en otra documentación interna.

---

## B2B — Clínicas Veterinarias

> Facturación mensual. Sin permanencia mínima. Requiere registro SENASA activo.

### 🏥 Afiliada Básica — Gratis

| Feature                                                                          |
| -------------------------------------------------------------------------------- |
| Perfil en directorio público + mapa (posición estándar)                          |
| Escanear QR de collar / microchip RFID                                           |
| Ver perfil público y datos del dueño (si mascota perdida)                        |
| Búsqueda por número de microchip                                                 |
| **Acceso y escritura al expediente del paciente** _(con grant activo del dueño)_ |

---

### ⭐ Clínica Plus — ₡15,000/mes (~$29 USD)

Todo lo de Básica, más:

| Feature                                            |
| -------------------------------------------------- |
| Posición **destacada** en mapa de clínicas         |
| Badge "Clínica Verificada" en directorio y alertas |
| Logo en alertas de pérdida cercanas                |
| Banner en Case Rooms de pacientes activos          |
| Estadísticas de escaneos mensuales                 |
| Métricas de visibilidad en directorio              |
| Soporte prioritario por email + onboarding         |

---

### 🤝 Clínica Partner — ₡35,000/mes (~$67 USD)

Todo lo de Plus, más:

| Feature                                                          |
| ---------------------------------------------------------------- |
| Certificados veterinarios PDF verificables (QuestPDF + QR único) |
| Verificación pública `/verificar/{código}`                       |
| Firma digital de clínica y médico veterinario                    |
| Widget embebible para sitio web propio                           |
| API de consulta directa (microchip, perfil mascota)              |
| RFID avanzado (lectores externos)                                |
| Soporte prioritario 24/7 + gerente de cuenta                     |
| Notificaciones en todos los Case Rooms del cantón                |
| Primeros resultados en búsquedas por zona                        |

---

### Comparativa B2B

| Feature                 | Básica | Plus ₡15k |  Partner ₡35k  |
| ----------------------- | :----: | :-------: | :------------: |
| Directorio + mapa       |   ✅   |    ✅     |       ✅       |
| Escanear QR/RFID        |   ✅   |    ✅     |       ✅       |
| Expediente del paciente |   ✅   |    ✅     |       ✅       |
| Posición destacada      |   ✗    |    ✅     |       ✅       |
| Badge Verificada        |   ✗    |    ✅     |       ✅       |
| Estadísticas            |   ✗    |    ✅     |       ✅       |
| Certificados PDF        |   ✗    |     ✗     |       ✅       |
| API directa             |   ✗    |     ✗     |       ✅       |
| Widget embebible        |   ✗    |     ✗     |       ✅       |
| Soporte                 |   —    |   Email   | 24/7 + gerente |

---

## Red de Aliados — Gratis (verificación requerida)

Para rescatistas, refugios, comercios pet-friendly, seguridad privada.

| Feature                                             |
| --------------------------------------------------- |
| Bandeja operativa de alertas en la zona declarada   |
| Dashboard KPI: alertas recibidas, tasa de respuesta |
| Confirmación de acción en campo                     |
| Perfil en red de aliados                            |

---

## B2G — Municipalidades (facturación anual)

### 🏛️ Básica — ₡150,000/año (~$288)

Portal control animal · registro de capturas · gestión de estados · reportes PDF · enlace con mascotas PawTrack · mapa del cantón · soporte email.

### 🏛️ Full — ₡300,000/año (~$577)

Todo Básica + API pública · dashboard tiempo real · estadísticas por barrio/distrito · exportación SENASA/PANI · SLA 99.5% · soporte telefónico.

### 🌐 Red Regional — ₡500,000/año (~$962)

Todo Full + múltiples cantones · capacitación presencial · personalización de marca · gerente de cuenta · integración PANI y SENASA · API inter-cantonal.

---

## Otros ingresos

| Fuente                   | Modelo                                                          |
| ------------------------ | --------------------------------------------------------------- |
| **Bounty (recompensas)** | 10% comisión al liberar recompensa + ₡1,000 en reembolsos       |
| **Afiliado Tractive**    | $20 USD fijo por tracker vendido vía link afiliado (Impact.com) |
| **Collar físico QR**     | ₡4,500–₡8,000 · placa standalone ₡1,500–₡2,500                  |
| **Combo GPS Pack**       | Collar + 12 meses Plus: ~₡55,000                                |

---

## Acceso al expediente médico — detalle por plan

> El expediente médico tiene acceso **gradual** por diseño para facilitar el upsell.

| Plan           | Qué ve                                                                                                                          |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **Explorador** | Solo count total de registros (cuántos existen, sin ver el contenido)                                                           |
| **Plus**       | Últimos **3 registros** — tipo, fecha, descripción, veterinario · sin documentos adjuntos · sin peso · sin campos de medicación |
| **Familia**    | Historial completo · todos los campos · editar/eliminar · PDF · weight trends · calendario                                      |
| **Clínica**    | Historial completo (requiere grant del dueño) · puede agregar registros desde cualquier plan                                    |

**Nota:** Las clínicas pueden **siempre escribir** registros en el expediente de un paciente con grant activo, independientemente del plan que tenga el dueño. El dueño necesita Plan Familia para leer lo que su veterinaria registró.

---

_PawTrack CR · Documento de Planes y Precios · Agosto 2026_
