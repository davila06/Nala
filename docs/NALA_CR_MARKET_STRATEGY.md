# Estrategia de mercado: Costa Rica

**Corte:** 2026-09-28.  
**Principio:** Costa Rica es el mercado de validación; contratos, regulación y adopción deben demostrarse por piloto, no inferirse del código.

## Tesis local

La oportunidad inicial no es competir por cantidad de features con un tracker global. Es resolver confianza e identidad en un mercado pequeño y conectable: QR accesible, recuperación coordinada, expediente compartible, clínicas verificadas, refugios y reportes institucionales. SINPE, WhatsApp, español, geografía cantonal y relaciones con veterinarias son factores de adopción más relevantes que una arquitectura global sofisticada en la primera fase.

## Prioridades

### Alta prioridad Costa Rica

| Oportunidad | Por qué | Código existente | Próxima evidencia requerida |
| --- | --- | --- | --- |
| QR + pérdida/hallazgo | Baja barrera, funciona con cualquier teléfono y conecta el núcleo | Pets, LostPets, Sightings, chat/handover | Piloto con métricas de scans, reportes, tiempos y reunificaciones |
| Clínicas y expediente compartible | Aumenta recurrencia y confianza; aprovecha el módulo clínico | Medical, Clinics, Certificates | 3-5 clínicas activas, consentimiento, flujo diario y soporte |
| Refugios/adopción | Reduce fragmentación y genera inventario comunitario | Allies, Adoptions, Fosters | ONG piloto, moderación, estados y resultados reales |
| SINPE y cobro simple | Método local conocido; evita depender solo de tarjeta | Payments/SINPE | Conciliación, evidencia de proveedor, reversos y soporte |
| Municipal piloto | Diferenciador institucional y fuente de adopción | Municipal, reports, welfare | Convenio, roles, SLA, protección de datos y un cantón piloto |
| WhatsApp y notificaciones | Canal de alta penetración | WhatsApp/Broadcast/Notifications | Credenciales, plantillas, opt-in, entregabilidad y costos |

### Prioridad media Costa Rica

- TrackSolid y collares mediante partner con soporte local, inventario y SLA.
- Marketplace de grooming, hoteles, paseadores y entrenadores con reserva y reputación.
- Campañas de castración coordinadas con municipalidades/ONGs.
- Widgets para clínicas y partners con dominios autorizados.
- Dashboard agregado de bienestar y recuperación, con anonimización y propósito definido.

### Prioridad baja Costa Rica

- NFC masivo antes de demostrar adopción QR.
- Seguro propio: requiere socio regulado y análisis actuarial.
- Retail/fulfillment propio y logística nacional.
- Telemedicina audiovisual antes de cerrar licenciamiento, profesionales y soporte.
- Copilotos clínicos autónomos o predicción médica.

## Segmentos

| Segmento | Producto inicial | Riesgo principal |
| --- | --- | --- |
| Dueños | Identidad, QR, pérdida, salud básica y alertas | Confianza, privacidad, costo y activación posterior al registro |
| Veterinarias | Expediente compartible, certificados, agenda, CRM e inventario | Cambio de workflow, interoperabilidad y responsabilidad clínica |
| Refugios/ONG | Adopción, casos, foster, campañas y evidencia | Moderación, recursos limitados y continuidad operativa |
| Municipalidades | Reportes agregados, campañas, bienestar y recuperación | Contratación pública, gobernanza, SLA y protección de datos |
| Grooming/hoteles/entrenadores | Directorio, disponibilidad y reservas | Liquidez, pagos, reputación y disputas |
| Pet shops | Catálogo, solicitudes y promociones | No prometer inventario/checkout si el código no lo garantiza |
| SENASA | Interoperabilidad futura de certificados/reportes | No afirmar integración oficial sin convenio y especificación |

## Regulación y confianza

El repositorio documenta trabajo sobre Ley 8968, consentimiento de datos de salud, edad, exportación y retención. Quedan pendientes evidencias de registro/aprobación institucional y DPA. Los datos de salud, ubicación, contacto y fotografías requieren minimización, finalidad, consentimiento y controles de acceso. Las afirmaciones de “SENASA-ready” o “verificado” deben distinguir formato técnico de aval estatal.

## Métricas del piloto

- Activación: mascota registrada -> QR publicado -> primer scan.
- Recuperación: pérdida reportada -> avistamiento -> contacto seguro -> cierre.
- Salud: usuarios con recordatorio y clínicas con uso semanal del expediente.
- Comunidad: refugios activos, publicaciones moderadas y adopciones completadas.
- Comercial: conversión, retención, costos de soporte, conciliación SINPE y margen.
- Confianza: incidentes, reportes de fraude, tiempo de respuesta y consentimientos válidos.

## Secuencia recomendada

1. Un cantón y una red pequeña de clínicas/refugios.
2. Validar QR, recuperación, expediente y notificaciones con soporte humano.
3. Añadir municipalidad y reportes agregados con contrato.
4. Añadir servicios con reservas, reputación y pago controlado.
5. Expandir a otros cantones solo después de métricas y runbook repetibles.
