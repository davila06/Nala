# PawTrack CR — Estrategia de Monetización

> Documento de trabajo — actualizado 2026-04-10  
> Uso interno. No publicar.

---

## 1. Productos físicos — Collares y accesorios con QR

El QR ya es SVG/PNG generado en el app (`/api/pets/{id}/qr`). El flujo operativo no requiere cambios en el backend — solo un formulario de pedido.

### Catálogo inicial propuesto

| Producto | Precio venta | Costo estimado | Margen |
|----------|-------------|----------------|--------|
| Placa de aluminio grabada (3×5 cm) | ₡4,500 | ₡1,200 | 73% |
| Tag de silicona con QR impreso UV | ₡5,500 | ₡1,800 | 67% |
| Collar nylon básico + placa incluida | ₡9,500 | ₡3,500 | 63% |
| Tag NFC + QR combo (toca o escanea) | ₡12,000 | ₡4,200 | 65% |
| Pack emergencia (placa + tarjeta bolsillo) | ₡7,000 | ₡2,000 | 71% |

### ¿Quién fabrica?
- **Grabado láser local**: empresas de señalización, trofeos o maquinarias CNC (Publigráfica, Rótulos JR, cualquier imprenta con láser).  
- **Tags de silicona**: AliExpress / Alibaba MOQ 100 uds, ~$0.40 c/u. Agregar QR en serigrafía.  
- **Chips NFC**: NTAG213 a $0.12–0.18 c/u. Escribir la URL del perfil con NFC Tools.  
- **Arranque sin inventario**: hacer por pedido individual las primeras 50 unidades. Sin costo de capital inicial.

### Punto de entrada mínimo
1. Agregar botón **"Pedir collar con QR"** en el perfil de mascota → redirige a WhatsApp con template pre-llenado.
2. Tiempo de implementación: 1 día.
3. Sin cambios de backend.

---

## 2. Plan Freemium — Suscripción de Dueños

### Estructura de tiers

| Feature | **Free** | **Plus** ₡2,990/mes | **Familia** ₡4,990/mes |
|---------|----------|---------------------|------------------------|
| Mascotas registradas | 1 | 3 | Ilimitadas |
| Historial de escaneos del QR | Últimos 5 | Completo + mapa de calor | Completo |
| Predicción de movimiento IA | ✗ | ✔ | ✔ |
| Notificaciones push prioritarias | ✗ | ✔ | ✔ |
| Alertas por SMS | ✗ | ✔ | ✔ |
| Radio de alerta ampliado (10 km vs 3 km) | ✗ | ✔ | ✔ |
| Sala de coordinación de 24h activa | ✗ | ✔ | ✔ |
| Badge "mascota verificada" en perfil público | ✗ | ✔ | ✔ |
| Registros médicos/vacunación en QR | ✗ | ✗ | ✔ |
| Acceso multi-usuario (ej. familia) | ✗ | ✗ | ✔ |

Lo que ya está construido y puede activarse: predicción de movimiento, historial de escaneos, sala de coordinación, radio de alertas.

---

## 3. Clínicas Veterinarias — B2B

El portal de clínicas ya existe. Solo falta activar tiers de pago.

| Tier | Precio | Incluye |
|------|--------|---------|
| **Afiliada básica** | Gratis | Aparece en directorio, puede escanear QR/microchip |
| **Clínica Plus** | ₡15,000/mes | Posición destacada en mapa, badge verificado, estadísticas de escaneos mensuales, logo en alertas de pérdida cercanas |
| **Clínica Partner** | ₡35,000/mes | Todo Plus + integración microchip RFID, widget embebible en su propio sitio, soporte prioritario, banner en caso rooms de mascotas cercanas |

**Potencial**: CR tiene ~600 clínicas veterinarias activas según SENASA. Con 30 en nivel Plus = ₡450,000/mes.

---

## 4. Municipalidades y Perreras — Licencia Institucional

Mencionado en el documento maestro como pendiente. Este es el canal de mayor ticket.

| Paquete | Precio anual | Incluye |
|---------|-------------|---------|
| **Municipalidad Básica** | ₡150,000/año | Portal de control animal, registro de capturados, integración con mapa público |
| **Municipalidad Full** | ₡300,000/año | Todo básico + API de consulta, reportes mensuales, SLA de soporte |
| **Red Regional** | ₡500,000/año | Múltiples cantones bajo misma licencia |

Costa Rica tiene **82 municipalidades**. 5 contratos básicos = ₡750,000/año recurrente.  
Contacto inmediato recomendado: Municipalidad de San José, Cartago, Heredia, Alajuela, Desamparados — todas con programas activos de control animal.

---

## 5. Sistema de Recompensas con Comisión (Modelo Bounty)

### Concepto y propuesta de valor

El modelo Bounty convierte a toda la comunidad de aliados de PawTrack en una red de búsqueda activa con incentivo económico real. El diferenciador clave frente a apps similares (PetAmberAlert, Finding Rover) es que la recompensa no es una promesa verbal — es dinero ya depositado en plataforma. Cuando el aliado ve la alerta, sabe que el pago es una certeza, no una intención.

Ningún actor en Costa Rica tiene esto implementado. Es un diferenciador de producto que genera viralidad orgánica: el aliado que cobró ₡22,500 por encontrar un perro en dos horas va a contar esa historia en WhatsApp, Instagram y con sus vecinos.

---

### Flujo técnico completo (UX + backend)

```
DUEÑO:
1. Activa "Reporte de pérdida" normal
2. Opción adicional: "Ofrecer recompensa"
3. Define monto libre (mínimo sugerido ₡10,000)
4. Paga via SINPE Móvil al número de plataforma (o Stripe para tarjeta)
5. Fondos quedan en escrow — NUNCA en la cuenta del dueño post-confirmación

ALIADO:
6. Ve la alerta en mapa con badge 💰 y monto visible (ej. "₡25,000 de recompensa")
7. Reporta avistamiento con foto + GPS
8. Si el avistamiento es el que lleva a la reunificación, se marca como "avistamiento clave"

REUNIFICACIÓN:
9. Dueño llega al lugar y usa el HandoverCode (ya implementado en backend)
10. Sistema valida el código → caso se cierra automáticamente
11. Recompensa menos fee de plataforma se libera en 24h vía SINPE Móvil al aliado

DISPUTA (si aplica):
12. Si el dueño intenta cerrar sin usar handover code, el fee no se libera automáticamente
13. Período de gracia de 48h para disputa antes de liberación
```

---

### Estructura de fees

| Modalidad | Fee PawTrack | Descripción |
|-----------|-------------|-------------|
| **Recompensa estándar** | 10% | Caso resuelto en tiempo normal |
| **Búsqueda Express** (opt-in) | 15% | Activa notificaciones push prioritarias a aliados en radio de 3km, visibilidad destacada en mapa |
| **Recompensa anónima** | 12% | El nombre del dueño no aparece — útil si la mascota se escapó de una situación delicada |

**Monto mínimo**: ₡10,000 (para cubrir el fee y dejar incentivo real al aliado)  
**Sin mínimo en Búsqueda Express** — el fee más alto ya cubre el costo de operación del push prioritario.

---

### Mecanismo de custodia y cumplimiento legal (SUGEF)

El escrow de pagos en CR tiene restricciones regulatorias. Opciones ordenadas por complejidad:

| Opción | Complejidad | Timeline | Riesgo |
|--------|-------------|----------|--------|
| **Cuenta transitoria en banco** (fondos recibidos en cuenta empresarial de PawTrack, liberados manualmente/API) | Baja | Inmediato | Riesgo operacional — requiere control interno fuerte |
| **Plataforma de pagos con holding** (Stripe Connect o similar con split automático) | Media | 2–4 semanas setup | Correcto técnicamente, puede tener fricción de KYC para aliados |
| **Licencia de operador de sistema de pagos (SUGEF 14-09)** | Alta | 6–18 meses | Overkill para MVP |

**Recomendación para MVP**: Cuenta transitoria empresarial + Stripe Connect para splittear el pago automáticamente. Stripe Connect permite que el aliado reciba su parte directamente a su cuenta bancaria, sin que PawTrack toque los fondos del aliado. Legal, auditable, y disponible en CR.

---

### Prevención de fraude

El sistema ya cuenta con:
- **HandoverCodes** (backend implementado) — la liberación de fondos solo ocurre con código válido
- **Sistema de reportes de fraude** (backend implementado) — aliados con reportes de fraude no pueden recibir recompensas
- **Aliado scoring** — reputación acumulada visible en su perfil

Nuevas capas necesarias para las recompensas:
- **Rate limiting por caso**: solo 1 aliado puede ser marcado como "avistamiento clave" por caso
- **Review window**: 48h de ventana para que el dueño dispute antes de liberación automática
- **Recompensa retenida si disputa abierta**: fondos nunca se mueven con una disputa activa
- **KYC light**: para aliados que quieren recibir recompensas >₡50,000, verificación de cédula (foto) vía el app

---

### Economía del modelo

**Escenario conservador**: 50 mascotas perdidas/mes en la plataforma, 30% activan recompensa, ₡25,000 promedio.

| Métrica | Valor |
|---------|-------|
| Casos con recompensa/mes | 15 |
| Monto promedio en custodia | ₡25,000 |
| Fee PawTrack (10%) | ₡2,500/caso |
| Ingreso mensual mes 6 | **₡37,500** |
| Ingreso mensual mes 12 (100 casos/mes, 35% con recompensa) | **₡122,500** |

El ingreso no es enorme solo, pero el **efecto red** es inmensurable: más aliados activos → más avistamientos → más reunificaciones → más usuarios registrando mascotas → más casos de recompensa. Es el volante del crecimiento.

---

### Lo que ya existe en el backend

| Feature | Estado |
|---------|--------|
| `HandoverCode` — códigos de entrega segura | ✅ Implementado |
| Sistema de reportes de fraude | ✅ Implementado |
| Chat enmascarado dueño-aliado | ✅ Implementado |
| `api/lost-pets/{id}/handover` endpoint | ✅ Implementado |
| Aliado scoring / reputación | ✅ Implementado |

| Feature | Por construir |
|---------|--------------|
| Escrow integration (Stripe Connect) | 🔧 ~3 semanas |
| UI de definición de recompensa en flujo de pérdida | 🔧 ~1 semana |
| Badge 💰 en alertas del mapa | 🔧 ~2 días |
| Lógica de liberación automática tras HandoverCode | 🔧 ~1 semana |
| Panel de disputas (admin) | 🔧 ~1 semana |

---

## 6. Publicidad contextual segmentada

| Formato | Ubicación | Precio sugerido |
|---------|-----------|----------------|
| Banner patrocinado en alertas de pérdida | Caso room + mapa alerta | ₡50,000/mes por zona |
| "Patrocinado por" en notificaciones push | Footer de push | ₡30,000/mes |
| Spot en historial de escaneos | "Escaneos recientes" | ₡25,000/mes |
| Clínica sugerida en perfil público | Sidebar del perfil `/p/:id` | CPA ₡1,500 por clic |

**Clientes naturales**: Purina, Royal Canin, Fancy Feast, Bayer Seresto, PetSmart, tiendas locales de mascotas.

---

## 7. Pasaporte Digital de Mascota

### El problema que resuelve

El proceso aduanal costarricense para viaje internacional con mascotas (SENASA Certificado Zoosan) es **completamente papel** en 2025. El dueño necesita:

1. Certificado veterinario oficial (SENASA) — papel
2. Vacuna antirrábica vigente — papel
3. Tratamiento antiparasitario recent — papel
4. En algunos destinos: microchip ISO 11784/11785 — se verifica manualmente

El proceso toma 2–5 días hábiles, requiere ir físicamente a una clínica afiliada SENASA, y los documentos se pierden o deterioran. Si la mascota enferma durante un viaje, el veterinario extranjero no tiene acceso rápido al historial.

**PawTrack ya tiene** los datos de la mascota. La clínica afiliada ya puede registrar las vacunas. La única pieza que falta es el PDF con firma digital y QR de verificación.

---

### Mercado objetivo

| Segmento | Tamaño estimado (CR) | Frecuencia de uso |
|----------|---------------------|-------------------|
| Expatriados con mascotas (~120,000 residentes legales) | ~15,000 con mascota | 1–2 veces/año (renovación o viaje) |
| Turistas que reingresan con mascotas (USA/EU) | ~2,000–5,000/año | 1 por viaje |
| Ticos que viajan con mascota (Panamá, USA, México) | ~3,000–8,000/año | 1 por viaje |
| Reubicación (familias que se mudan con mascotas) | ~500–1,000/año | 1 por proceso |

**Total addressable**: ~20,000–30,000 generaciones de pasaporte/año a escala.  
**Año 1 target realista**: 500–2,000 pasaportes si la comunidad expat adopta la plataforma.

---

### Propuesta de producto

**Pasaporte Digital PawTrack** — un PDF/A (archivable) de alta seguridad que incluye:

| Sección | Contenido | Fuente de datos |
|---------|-----------|----------------|
| Identidad de la mascota | Nombre, especie, raza, fecha de nacimiento, color, foto | Perfil PawTrack |
| Microchip | Número ISO, fecha de implante, clínica implantadora | Registro PawTrack / scan de clínica |
| Vacunas | Historial completo con fechas, productos, lotes | Registros de clínica afiliada |
| Tratamientos | Antiparasitario, antirrábico con fechas | Registros de clínica |
| Dueño | Nombre, contacto de emergencia | Perfil usuario |
| QR de verificación | URL pública con hash de integridad | Endpoint PawTrack |
| Firma digital | Sello temporal + hash SHA-256 del documento | Generado en servidor |

**Validez del documento**: El QR de verificación apunta a `pawtrack.cr/passport/verify/{uuid}` — cualquier veterinario extranjero puede confirmar que el documento no fue alterado.

---

### Diferenciación frente al proceso SENASA

PawTrack no reemplaza el Certificado Zoosan oficial (eso requiere habilitación gubernamental y es un proceso largo). El posicionamiento correcto es:

> **"El pasaporte digital PawTrack es el documento de referencia rápida y el respaldo digital del certificado oficial."**

Casos de uso claros donde SENASA es inconveniente pero PawTrack sí sirve:
- Emergencia veterinaria en el extranjero (historial médico accesible al instante)
- Viajes a Panamá (no siempre requieren SENASA, pero sí historial de vacunas)
- Consultas internas en CR con veterinario nuevo (acceso al historial sin traer papeles)
- Adopciones internacionales facilitadas por una ONG

**Ruta a largo plazo**: gestionar con SENASA el reconocimiento del pasaporte digital como complemento electrónico del Cert. Zoosan. En la UE y USA ya existen equivalentes digitales (EU Pet Passport, USDA Health Certificate + APHIS). CR puede modernizarse.

---

### Modelo de ingresos

| Modalidad | Precio | Costo para PawTrack |
|-----------|--------|---------------------|
| **Generación individual** | ₡2,500 | ~₡0 (PDF dinámico) |
| **Actualización/re-emisión** (cambio de vacunas, nuevo dueño) | ₡1,000 | ~₡0 |
| **Incluido en Plan Familia** | ₡1,500/año (pricing del plan) | ~₡0 |
| **Pasaporte + Certificado asistido por clínica afiliada** | ₡6,000 (bundle) | Clínica toma ₡3,000 |

**Margen bruto**: ~99% en todas las modalidades (es generación de PDF + almacenamiento S3).

---

### Implementación técnica

**Generación del PDF:**
- Backend: Puppeteer (Node.js) o wkhtmltopdf vía proceso .NET — renderiza HTML template con los datos de la mascota → PDF
- Alternativa más limpia: `QuestPDF` (biblioteca .NET pura, sin headless browser, licencia permisiva)
- Output: PDF/A-1b (formato de archivo standard, compatible con todos los lectores)

**Firma digital / QR verification:**
- Generar UUID v7 por pasaporte al momento de emisión
- Registrar en DB: `PassportId`, `PetId`, `IssuedAt`, `DataHash (SHA-256)`, `IsRevoked`
- QR en el PDF apunta a: `GET /api/passport/verify/{passportId}` → responde JSON con status y campos clave
- No se necesita certificado PKI externo para MVP — el hash + timestamp es suficiente para verificación básica

**Endpoint de verificación pública** (no requiere auth):
```json
GET /api/passport/verify/01970abc-...
{
  "valid": true,
  "issuedAt": "2025-11-15T10:22:00Z",
  "petName": "Luna",
  "species": "Canino",
  "rabiesVaccineUntil": "2026-11-10",
  "microchipNumber": "985141000123456",
  "tampered": false
}
```

---

### Qué construir

| Componente | Esfuerzo | Prioridad |
|------------|----------|-----------|
| `QuestPDF` template con diseño del pasaporte | 3–4 días | Alta |
| Endpoint `POST /api/pets/{id}/passport` (genera y guarda) | 1 día | Alta |
| Endpoint `GET /api/passport/verify/{id}` (público) | ½ día | Alta |
| UI en perfil de mascota: botón "Generar Pasaporte" | 1 día | Alta |
| Integración de pago (Stripe/SINPE) antes de generar | 1 día | Media |
| Módulo clínica: validación de vacunas para el pasaporte | 2 días | Media |

---

## 8. Marketplace de Servicios de Mascota

El sistema de custodios (fosters) ya existe. Extenderlo a:

| Servicio | Comisión PawTrack |
|----------|-------------------|
| Pet sitting (cuido en casa del proveedor) | 12% de la transacción |
| Dog walking | 12% |
| Custodia temporal durante viaje | 10% |
| Peluquería canina (proveedor verifica vía el app) | 8% |

Los providers son aliados ya verificados en el sistema. El pago pasa por la plataforma (SINPE / Stripe) para retener el fee.

**Potencial en CR**: Mercado de servicios de mascotas estimado en $8–12M/año. Tomar 1% = $80k–120k/año.

---

## 9. API Pública para Terceros

Empresas de veterinaria, apps de adopción, sistemas municipales de control animal podrían consumir la API de PawTrack:

| Tier | Precio | Límite |
|------|--------|--------|
| **Developer** | Gratis | 1,000 req/día, solo lectura pública |
| **Startup** | $50/mes | 50,000 req/día, lectura + webhooks |
| **Business** | $200/mes | Ilimitado + SLA + soporte |

Casos de uso: apps de adopción que quieren verificar microchips, hospitales veterinarios que quieren búsqueda de dueño en emergencias, sistemas municipales.

---

## 10. Licencia White-Label para ONGs y Refugios

ASOPROA, ZOOPAL, WUF, Territorios de Zaguates y otros refugios grandes podrían usar PawTrack bajo su propia marca para gestionar su inventario de animales:

- **Precio**: $300–600/año por organización
- **Incluye**: subdominio propio, logo de la org, módulo de adopción, acceso API
- **Costo adicional para PawTrack**: mínimo (tenant isolation en la DB)

---

## Ideas Out of the Box

### 11. Kiosko físico con QR instantáneo

#### Concepto

Una terminal de autoservicio ubicada permanentemente en puntos de alto tráfico con dueños de mascotas. El dueño llega, muestra el QR de su mascota (desde el app) en la pantalla del quiosco, paga ₡3,000 con SINPE Móvil o tarjeta, y recibe una placa de identificación impresa y laminada al instante. Sin esperar envío. Sin necesidad de hablar con nadie.

El objetivo no es solo ingresos directos — el quiosco es una pieza de **marketing físico permanente**: está en la clínica, tiene el logo de PawTrack, genera conversaciones ("¿qué es eso?"), y convierte propietarios que nunca han oído del app en usuarios registrados.

---

#### Hardware y costos

| Componente | Modelo / Proveedor | Costo unitario (USD) |
|------------|-------------------|---------------------|
| Impresora de etiquetas color | Brother QL-820NWB o Zebra ZD421 | $280–350 |
| Mini PC (NUC o RasPi 5) | Intel NUC 13 o Raspberry Pi 5 8GB | $150–200 |
| Pantalla táctil 15" | Pantalla táctil industrial genérica (Aliexpress + proveedor local) | $120–180 |
| Lector QR integrado | Módulo Zebra DS457 o similar | $60–90 |
| Enclosure / Mueble | Acrílico + aluminio custom fabricado en CR (Metalpak o similar) | $180–250 |
| Terminal de pago SINPE | Datafono Credomatic o similar (si aplica; alternativa: QR de SINPE en pantalla) | $80–150 o $0 |
| **Total por unidad** | | **~$870–1,220 USD** |

A tipo de cambio ₡515: **₡448,000–₡628,000 por unidad**.

**Forma de reducir el costo inicial**: Negociar con el local anfitrión que él compra el hardware a cambio de un porcentaje de ingresos más alto (50/50 en vez de 60/40). PawTrack pone el software.

---

#### Material consumible (placa física)

| Tipo | Dimensiones | Costo/unidad | Proveedor |
|------|------------|-------------|----------|
| Etiqueta PVC + laminado | 54mm × 86mm (tamaño tarjeta) | ₡200–350 | Proveedor de etiquetas Brother/Zebra local |
| Plástico rígido impreso | 54mm × 86mm | ₡400–600 (impresión + corte CNC) | Servicio de impresión local |

**MVP más viable**: Etiqueta Brother en material de vinilo resistente al agua, con laminado en frío por el propio quiosco (laminadora Brother BLP-1000 integrada, $180). Costo total por placa: ≈ ₡280. Precio de venta: ₡3,000. **Margen bruto por placa: ₡2,720 → split 60/40 = ₡1,632 PawTrack / ₡1,088 local**.

---

#### Flujo de usuario en el quiosco

```
1. Usuario se acerca al quiosco
2. Pantalla muestra instrucciones: "Abre PawTrack → Perfil de tu mascota → Mostrar QR"
3. Usuario muestra QR en pantalla del teléfono
4. Quiosco lee el QR (lector integrado) → llama a API PawTrack para obtener datos
5. Pantalla muestra preview de la placa: foto de la mascota + nombre + número de contacto
6. Usuario confirma o ajusta (ej. número de teléfono que aparece)
7. Usuario selecciona método de pago: SINPE Móvil (QR en pantalla) o tarjeta (si hay datafono)
8. Pago confirmado → impresión automática
9. Placa sale por la ranura → laminada, lista para usar
10. App PawTrack recibe notificación de confirmación + foto de la placa
```

**Tiempo total**: ~2–4 minutos desde llegada hasta placa en mano.

---

#### Estrategia de despliegue

**Fase 1 — Piloto (mes 1–2):**
- 1 quiosco en la clínica veterinaria afiliada más activa de la red
- Costo total: ~₡550,000 (hardware + enclosure + instalación)
- Objetivo: validar UX, medir throughput real, identificar problemas de hardware
- Break-even a 5 placas/día: ₡1,632 × 5 × 30 días = ₡244,800/mes → break-even en ~67 días

**Fase 2 — Expansión (mes 3–6):**
- 5 quioscos: 2 clínicas veterinarias activas, 1 Automercado, 1 PriceSmart, 1 Pet City
- Negociación coordinada con cadenas (propuesta: el quiosco no ocupa área de venta, genera tráfico)
- Revenue mensual proyectado: 5 × 5 placas/día × ₡1,632 × 30 días = **₡1,224,000/mes**

**Fase 3 — Escala (mes 6–12):**
- 15–20 quioscos en GAM
- Revenue proyectado: 20 × 4 placas/día = **₡3,916,800/mes** (escenario optimista)
- Revenue proyectado: 20 × 2.5 placas/día = **₡2,448,000/mes** (escenario conservador)

---

#### Economía del negocio (20 quioscos, conservador)

| Métrica | Valor |
|---------|-------|
| Quioscos activos | 20 |
| Placas/día por quiosco (promedio conservador) | 2.5 |
| Placas/mes total | 1,500 |
| Precio de venta | ₡3,000 |
| Costo de material | ₡280 |
| Margen bruto por placa | ₡2,720 |
| Split 60% PawTrack | ₡1,632 |
| **Ingreso PawTrack mensual** | **₡2,448,000** |
| Costo mantenimiento (~₡50k/mes en visitas técnicas) | ₡50,000 |
| **Ingreso neto mensual** | **₡2,398,000** |

---

#### Cambios necesarios en el sistema

| Componente | Trabajo |
|------------|---------|
| Endpoint `GET /api/kiosk/pet-data/{qrToken}` (solo campos necesarios para placa) | 1 día |
| Registro de `KioskOrder` en DB (quiosco, pet, timestamp, pago) | ½ día |
| Webhook de confirmación de pago → trigger de impresión | 1 día |
| Notificación push al dueño: "Tu placa fue impresa en [ubicación]" | ½ día |
| Software del quiosco (Electron o app web en kiosk mode) | 1–2 semanas separado |

---

### 12. "Suscripción Caja" mensual de mascota
**Concepto**: Una caja mensual (modelo Barkbox para CR) curada y co-branded con PawTrack:
- Collar o tag QR personalizado
- Snacks premium
- Juguete
- 1 mes de PawTrack Plus incluido

**Precio**: ₡15,000/mes. Producción + logística: ~₡8,000. Margen: ~47%.  
**Canal**: Anunciarlo en el app a todos los dueños con mascota registrada. Base de prospectos ya existe.

---

### 13. Programa "Rescate Corporativo" (CSR)
**Concepto**: Empresas hacen donaciones visibles en el app. Por ejemplo, "Purina patrocina la búsqueda de Toto" aparece en el mapa y en la sala de coordinación.

- **TicketABC**: empresa paga ₡50,000 para ser "rescatista oficial" de un caso
- Logo aparece en el perfil público, mapa y en notificaciones
- Reporte de impacto a la empresa (cuántas personas vieron su marca)
- Empresas objetivo: marcas de alimento de mascotas, veterinarias nacionales, tiendas

---

### 14. Seguro de mascota co-branded con INS

#### El problema de mercado
Costa Rica no tiene un mercado de seguros de mascotas maduro. El INS ofrece "Seguro de Vida para Mascotas" desde 2019 pero su penetración es bajísima porque el canal de distribución es torpe: el dueño tiene que ir a una agencia, llenar formularios en papel, y presentar un certificado veterinario. El resultado: menos del 2% de las mascotas del país están aseguradas.

PawTrack tiene exactamente los datos que una aseguradora necesita para emitir una póliza al instante:
- Especie, raza, sexo, fecha de nacimiento → cálculo actuarial automático
- Foto del perfil → verificación de identidad de la mascota
- Historial de vacunas (registrado por clínica afiliada) → determinación de riesgo
- Email verificado del dueño → datos del tomador de póliza sin formularios adicionales

#### Modelo de negocio

**¿Cómo funciona?**
1. En el perfil de la mascota (y en la confirmación de una pérdida) aparece un banner contextual: "Protege a [Nombre] con seguro médico — desde ₡4,500/mes".
2. El dueño hace clic → PawTrack pre-llena el formulario con los datos ya registrados (cero fricción).
3. El lead es enviado a la aseguradora vía API con los datos del perfil.
4. Si el cliente contrata → PawTrack recibe una comisión CPA por cada póliza emitida.
5. **PawTrack no asume ningún riesgo actuarial**, no maneja dinero de primas, no gestiona siniestros.

**Modelo CPA estimado**

| Tipo de póliza | Prima mensual | CPA para PawTrack | Equivalencia |
|---------------|--------------|-------------------|-------------|
| Muerte accidental básica | ₡2,500 | ₡3,500 por póliza | 1.4 meses de prima |
| Enfermedades + accidentes | ₡6,000 | ₡8,000 por póliza | 1.3 meses de prima |
| Integral (cirugías + internamiento) | ₡14,000 | ₡18,000 por póliza | 1.3 meses de prima |

**Proyección de conversión**

| Usuarios activos | Tasa conversión | Pólizas/mes | CPA promedio | Ingreso mensual |
|-----------------|-----------------|-------------|-------------|----------------|
| 2,000 | 2% | 40 | ₡6,000 | ₡240,000 |
| 10,000 | 3% | 300 | ₡6,500 | ₡1,950,000 |
| 50,000 | 3% | 1,500 | ₡7,000 | ₡10,500,000 |

#### Por qué el contexto importa (timing del banner)
Mostrar el banner cuando el dueño acaba de reportar una pérdida es el momento de máxima angustia — y máxima receptividad. El mensaje cambia: "La próxima vez, que las facturas del veterinario no sean el problema — asegura a [Nombre] hoy". La tasa de conversión post-evento de pérdida puede ser 3–5× la tasa normal.

#### Partners potenciales en orden de prioridad
1. **INS**: único asegurador obligatorio de CR. Tiene producto existente. Contactar Unidades de Negocio Voluntarias.
2. **Océano Seguros**: intermediario ágil, puede estructurar acuerdos API rápidamente.
3. **SURA Costa Rica**: opera en toda Centroamérica, enfoque en productos de nicho.
4. **Mapfre Costa Rica**: tiene línea de mascotas activa en España, potencial expansión regional.

#### Lo que PawTrack debe construir
- Banner contextual en perfil de mascota y en el caso room (condicional: usuarios Free)
- Formulario de cotización pre-llenado con los datos del perfil elevado a la aseguradora vía API
- Panel interno para tracking de leads enviados y comisiones recibidas
- Webhook de confirmación de la aseguradora para registrar cuando se emite la póliza

**Tiempo estimado**: 2 semanas UI + API, más 4–8 semanas de negociación comercial.

#### Consideraciones legales
- PawTrack actuaría como generador de leads en modalidad de **referidos sin comisión variable** (la más simple legalmente): cobra tarifa fija mensual por acceso a la base de usuarios, no porcentaje de prima → no requiere licencia de intermediario de seguros ante SUGESE.
- Si se negocia CPA por póliza emitida → se requiere inscripción como agente de seguros (trámite ~3 meses) o asociarse con un corredor ya inscrito que actúe como intermediario formal.

---

### 15. "Modo Adoptante" — Marketplace de Adopción Verificada
**Concepto**: Refugios registran animales en el sistema. Personas interesadas en adoptar buscan por zona, raza, tamaño. PawTrack verifica la identidad del adoptante (ya tiene el sistema de roles y verificación de email) y facilita el proceso:

- **Refugio paga**: ₡500 por animal listado o ₡2,000/mes suscripción
- **Adoptante paga**: ₡1,000 fee de "verificación de adoptante responsable"
- Diferenciador: post-adopción, la mascota queda registrada automáticamente en PawTrack y el adoptante recibe el QR

---

### 16. Datos agregados — Inteligencia de bienestar animal

#### Por qué este es el activo más valioso a largo plazo
Cada vez que un dueño registra una mascota, reporta una pérdida, sube un avistamiento, o una clínica escanea un microchip, PawTrack acumula datos que **nadie más en Costa Rica tiene**. SENASA no tiene datos georeferenciados de mascotas perdidas. Las municipalidades no tienen tasas de recuperación. La academia no tiene cohortes de comportamiento de mascotas domésticas en zonas urbanas costarricenses.

El dataset que PawTrack acumula pasivamente (sin costo adicional) incluye:

| Dataset | Resolución | Valor de mercado |
|---------|-----------|------------------|
| Mapa de calor de pérdidas por zona, mes, especie | Cantón / barrio | Alto |
| Tasas de recuperación por método (con QR vs sin QR, con aliado vs sin aliado) | Histórico | Muy alto |
| Tiempo promedio de reunificación por raza, zona y época del año | Diario | Alto |
| Patrones de movimiento de mascotas perdidas (rutas predichas vs reales) | GPS punto | Muy alto |
| Razas y especies más reportadas como perdidas vs recuperadas | Mensual | Medio |
| Correlación entre densidad de aliados y tasa de recuperación exitosa | Zonal | Alto |
| Frecuencia de escaneos de QR por zona (popularidad de arterias/parques) | Diario | Medio |
| Cobertura geográfica de microchipping por clínica | Cantonal | Alto |

#### Compradores y modelos de precio

**Gobierno e instituciones**

| Comprador | Qué quieren | Modelo | Precio |
|-----------|-------------|--------|--------|
| SENASA | Epidemiología animal, cobertura de microchipping, razas de riesgo | Informe anual + API de consulta | $2,000–5,000/año |
| Municipalidades | Mapa de pérdidas por barrio para planificar patrullaje animal | Reporte mensual geo | $300–600/año |
| Ministerio de Salud | Vinculación entre densidad de mascotas y enfermedades zoonóticas | Dataset crudo anonimizado | $1,000–3,000 por extracción |
| IFAM | Benchmarking cantonal de bienestar animal | Reporte comparativo anual | $800–1,500 |

**Academia**

| Institución | Qué quieren | Modelo |
|-------------|-------------|--------|
| UCR (Medicina Veterinaria) | Tesis, investigaciones sobre comportamiento y recuperación | API gratuita a cambio de publicación con mención a PawTrack |
| TEC | Proyectos de IA/ML con datos reales costarricenses | Dataset anonimizado + partnership de investigación |
| UNA | Estudios de bienestar animal en zonas rurales vs urbanas | Licencia de datos $500–1,000 |

**Sector privado**

| Comprador | Uso | Precio |
|-----------|-----|--------|
| Purina / Royal Canin | Inteligencia de mercado: razas dominantes, zonas, edad promedio | $3,000–8,000/año |
| Constructoras / desarrolladores | Densidad de mascotas por zona para reglamentos de condominios pet-friendly | $500–1,500 por reporte |
| Aseguradoras (INS, SURA) | Actuaría: frecuencia de pérdidas por raza/zona para calcular primas | $5,000–15,000/año |
| Medios (La Nación, CRHoy) | Infografías: "Las zonas donde más se pierden mascotas en San José" | $200–500 por informe, o gratis a cambio de mención |

#### Cumplimiento legal (crítico)
- **Ley 8968** (Protección de Datos de Costa Rica): prohíbe ceder datos personales sin consentimiento explícito.
- **Lo que PawTrack puede vender sin fricción legal**: datos 100% anonimizados y agregados — nunca a nivel individual, nunca identificable.
- **Proceso técnico requerido**: anonymization pipeline que elimina nombre, email, teléfono, foto y cualquier identificador directo antes de exportar. Solo se conservan: especie, raza, zona geográfica (mínimo barrio), y fecha relativizada.
- **Cláusula de consentimiento en ToS**: agregar en el registro: "PawTrack puede usar datos anonimizados y agregados con fines estadísticos y de investigación". Es práctica estándar de la industria.
- Seguir principios GDPR aunque CR no sea EU: aumenta credibilidad institucional y protege ante futuros cambios legislativos.

#### Infraestructura requerida
1. **Pipeline de anonimización** (job nocturno que exporta a tablas `analytics_*` sin PII)
2. **Dashboard de reportes** — Metabase self-hosted (gratuito) o Power BI embebido, listo en 2 días
3. **API de consulta** autenticada por `api_key` para compradores institucionales con filtros por fecha, especie, cantón
4. **Panel de contratos** donde el comprador puede configurar sus reportes periódicos

**Tiempo estimado de implementación básica**: 3–4 semanas. El dashboard puede estar en producción en 2 días con Metabase.

#### Efecto compuesto (la razón de la prioridad)
A diferencia de todos los otros canales, **el valor de este activo crece exponencialmente con el tiempo y la escala**. Con 1,000 usuarios el dataset es interesante. Con 50,000 usuarios y 3 años de datos históricos, es el dataset más completo de comportamiento de mascotas domésticas en Centroamérica — y nadie más puede construirlo desde cero en menos de 5 años.

---

### 17. Evento físico — PawTrack Day

#### Concepto estratégico
Un evento anual gratuito para el público, financiado íntegramente por patrocinadores, que sirve simultáneamente como:
1. **Motor de adquisición masiva** (objetivo: 1,000–5,000 registros nuevos en un solo día)
2. **Fuente de ingresos directa** por patrocinios y stands
3. **PR y construcción de marca** — cobertura mediática gratuita por ser una causa noble con comunidad visible
4. **Activación de la red veterinaria** — convierte el evento en el punto de encuentro del ecosistema de mascotas de CR

#### Programa del día (propuesta)

| Hora | Actividad | Quién |
|------|-----------|-------|
| 8:00–9:00 | Registro, desayuno patrocinado, demo live del app | PawTrack + sponsor |
| 9:00–17:00 | Microchipping gratuito (stands de clínicas afiliadas) | Red de clínicas |
| 9:00–17:00 | Stands: productos, adopciones, servicios, ONGs | Sponsors + aliados |
| 11:00–12:00 | Demo en vivo: reunificación simulada con el app | Equipo PawTrack |
| 12:00–14:00 | Concurso de fotos "Mi mascota con su QR" (votación en el app) | Comunidad |
| 14:00–15:00 | Charla: bienestar animal en CR — invitado SENASA | Institucional |
| 15:00–16:00 | Premiación, sorteos de productos patrocinados | Sponsors |
| 16:00–17:00 | Cierre: estadísticas del día, reunificación destacada del año | PawTrack |

**Venue ideal**: Parque La Sabana (San José), Parque Los Reyes (La Guácima), o espacio cedido por un sponsor corporativo (PriceSmart, Walmart) con área exterior.

#### Estructuras de patrocinio

| Nivel | Precio | Beneficios |
|-------|--------|------------|
| **Diamante** (1 disponible) | ₡1,500,000 | Nombre en el evento ("PawTrack Day by [Marca]"), stand premium 6×6m, logo en todos los materiales, 5 min en escenario, menciones en RRSS (antes/durante/después), 500 collar tags con su logo incluidos |
| **Oro** (3 disponibles) | ₡600,000 | Stand 4×4m, logo en banner principal y programa, 3 menciones en RRSS, 200 tags co-branded |
| **Plata** (6 disponibles) | ₡250,000 | Stand 2×3m, logo en programa del día, 1 publicación en RRSS |
| **Clínica afiliada** (20 disponibles) | ₡120,000 | Stand de microchipping/consulta, directorio del evento impreso, captación directa de nuevos clientes |
| **ONG / Refugio** (10 disponibles) | ₡35,000 (simbólico) | Espacio para adopciones y difusión, mención en programa |

**Revenue bruto estimado (primer evento):**
```
1 Diamante:  ₡1,500,000
2 Oro:       ₡1,200,000
4 Plata:     ₡1,000,000
15 Clínicas: ₡1,800,000
5 ONGs:        ₡175,000
─────────────────────────
BRUTO:       ₡5,675,000
Costos op:  -₡1,500,000  (logística, sonido, sillas, permisos, seguridad)
─────────────────────────
NETO:        ₡4,175,000
```

#### El microchipping gratuito como motor de adquisición
Este es el elemento más crítico del modelo. La lógica:
- El microchipping en clínica cuesta ₡15,000–25,000 en CR — es la principal barrera para que dueños lo hagan.
- Un evento con microchipping gratuito atrae exactamente al perfil de dueño responsable que es el usuario ideal de PawTrack.
- Con 20 clínicas afiliadas y chips a ₡1,500 c/u (costo cubierto por el sponsor Diamante), se pueden chipear 500–1,000 mascotas en un día.
- Flujo: mascota chipiada → clínica escanea en el portal PawTrack → dueño recibe invitación para completar el perfil → **registro instantáneo**.
- **Costo de adquisición por usuario**: si se chipean 1,000 mascotas y el sponsor cubre ₡1,500 c/u = ₡1,500,000 cubierto por el Diamante. Costo para PawTrack: ₡0.

#### Sponsors naturales para contactar
- **Alimentos y nutrición**: Purina (Nestlé CR), Royal Canin (Mars), Pedigree, Hill's Pro Plan
- **Farmacéutica veterinaria**: Bayer (collar Seresto, Drontal), Zoetis, Boehringer Ingelheim
- **Retail mascotas**: Mundoanimal, Petco (si entra CR), PetSmart regional, Walmart (sección mascotas)
- **Tecnología y telecomunicaciones**: Claro, Kolbi, Movistar (afinidad con app y notificaciones)
- **Banca y finanzas**: BAC Credomatic, Banco Nacional (RSE), BPDC
- **Alimentación humana con mascotas**: Automercado, MaxiPali (Walmart CR)

#### Cobertura mediática esperada (sin costo)
- **La Nación**: sección "País" — historia emocional de reunificación durante el evento es exactamente su formato.
- **Teletica / Repretel**: nota en telenoticiero si se coordina con ángulo de causa noble.
- **Influencers de mascotas en CR** (Instagram/TikTok): invitarlos como "embajadores del día" a cambio de cobertura. Cuentas con 10k–50k seguidores en el nicho de mascotas son suficientes.
- Regla de oro: **un caso de reunificación en vivo durante el evento = contenido viral garantizado**. Coordinar con el equipo para que si hay una mascota perdida en los días previos, se intenta la reunificación en el evento.

#### Escalabilidad por año
- **Año 1**: 1 evento, San José (Gran Área Metropolitana)
- **Año 2**: 1 evento GAM + 2 eventos regionales (Cartago, Alajuela) con sponsors locales
- **Año 3**: Formato franquiciado — municipalidades pueden co-organizar bajo la marca PawTrack Day
- **Largo plazo**: PawTrack Day como el evento de bienestar animal más grande de Centroamérica

---

## Proyección de ingresos — Escenario conservador (12 meses)

| Canal | Mes 1 | Mes 6 | Mes 12 |
|-------|-------|-------|--------|
| Collares físicos (50 → 120 → 200 u/mes) | ₡170,000 | ₡408,000 | ₡680,000 |
| Plus suscripciones (0 → 200 → 500) | ₡0 | ₡598,000 | ₡1,495,000 |
| Clínicas Plus (0 → 5 → 15) | ₡0 | ₡75,000 | ₡225,000 |
| Municipalidades (0 → 1 → 3) | ₡0 | ₡0 | ₡450,000 |
| Publicidad / patrocinios | ₡0 | ₡100,000 | ₡250,000 |
| Bounty fees — 10% recompensas (0 → 15 → 35 casos/mes) | ₡0 | ₡37,500 | ₡87,500 |
| Seguro co-brand INS (CPA 2,500 → 0 → 30 → 80 polizas/mes) | ₡0 | ₡75,000 | ₡200,000 |
| Datos anonimizados (0 → 0 → 1 contrato/trim.) | ₡0 | ₡0 | ₡150,000 |
| Pasaporte digital (0 → 40 → 100 generaciones/mes) | ₡0 | ₡100,000 | ₡250,000 |
| Quioscos físicos (0 → 1 → 5 quioscos) | ₡0 | ₡48,960 | ₡244,800 |
| PawTrack Day (anual, prorrateado) | ₡0 | ₡0 | ₡347,900 |
| **Total mensual** | **₡170,000** | **₡1,442,460** | **₡4,380,200** |

---

## Hoja de ruta de implementación

| Prioridad | Acción | Requiere código | Tiempo |
|-----------|--------|----------------|--------|
| 🟢 **1** | Botón "Pedir collar" → WhatsApp con form | No | 1 día |
| 🟢 **2** | Activar tier Plus con límite de mascotas | Mínimo | 1 semana |
| 🟡 **3** | Portal de clínicas con tier de pago (Stripe/SINPE) | Sí | 2 semanas |
| 🟡 **4** | Sistema de recompensas con SINPE en custodia | Sí | 3 semanas |
| 🟠 **5** | Contactar 5 municipalidades con deck de ventas | No | Inmediato |
| 🟠 **6** | Pasaporte digital PDF verificado | Sí (mínimo) | 1 semana |
| 🔴 **7** | Marketplace de servicios (pet sitting) | Sí (mayor) | 1–2 meses |
| 🔴 **8** | Kiosko físico piloto en 1 clínica | Hardware + BD | 2–3 meses |

---

## Notas estratégicas

- El **collar físico** es el de menor fricción. No requiere cambios, solo un proveedor y un formulario.
- La **suscripción Plus** es el motor de ingreso recurrente más predecible. Priorizar después del collar.
- Las **clínicas y municipalidades** son B2B con ciclo de venta más largo pero mayor valor de vida.
- El **sistema de bounty** es diferenciador único en la región — nadie más lo tiene. Genera viralidad.
- El **pasaporte digital** es de altísimo margen (costo ≈ $0) y tiene demanda real por comunidad expat.
- Los **datos anonimizados** son un activo que se acumula pasivamente y su valor aumenta con escala.
