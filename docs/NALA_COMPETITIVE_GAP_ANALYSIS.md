# Análisis de brechas competitivas

**Corte:** 2026-09-28.  
**Tipo:** estrategia basada en capacidades observadas y comparación de categorías.  
**Advertencia:** las capacidades de terceros requieren validación vigente por país, plan y fecha; no se presentan aquí como auditoría contractual de Tractive, Fi, Whistle, PetHub, Pawfit, Airvet, Dutch, PetDesk, VitusVet, PetPage, Rover, Wag, Petco, Banfield, Digitail, DaySmart Vet, Vetster, Figo, Pumpkin o Chewy.

## Posición actual de NALA

NALA tiene una combinación poco común de recuperación de mascotas, identidad pública, expediente, clínicas, adopciones, proveedores, municipalidades y pagos preparados. Frente a productos especializados, su ventaja potencial es la red local y el ciclo completo de identidad -> pérdida -> hallazgo -> salud -> servicios. Su debilidad actual es que la amplitud supera la evidencia de operación: proveedores, hardware, pagos, telemedicina e IA generativa no están uniformemente verificados.

## Comparación por categoría

| Categoría de líderes | Patrón competitivo observable | Brecha de NALA | Prioridad |
| --- | --- | --- | --- |
| Tractive, Fi, Whistle, Pawfit | Hardware, conectividad, batería, alertas, mapas y suscripción recurrente | NALA tiene API/GPS/TrackSolid, pero no demuestra hardware propio, cobertura, batería, SLA ni escala de dispositivos | Alta |
| PetHub | Identidad/QR y perfil accesible | NALA puede competir con QR + recuperación + salud, pero debe medir scans, conversión y reunificación | Alta |
| Airvet, Dutch, Vetster | Consulta remota, profesionales, agenda, pagos y seguimiento | NALA tiene consulta clínica administrativa, no video/audio ACS ni operación telemédica | Alta |
| PetDesk, VitusVet, Digitail, DaySmart Vet | Workflow diario de clínica, comunicación, recordatorios, agenda, inventario y práctica | NALA tiene varios módulos enterprise, pero debe demostrar uso diario, interoperabilidad y soporte clínico | Alta |
| Rover, Wag | Marketplace de servicios, confianza, disponibilidad, pagos, reseñas y protección | NALA tiene directorio/reserva; pagos, payout, reputación, disputas y liquidez son incompletos | Alta |
| Petco, Banfield, Chewy | Ecosistema, retail, membresías, salud, logística y retención | NALA no tiene escala de inventario, fulfillment, red física ni membership madura | Media |
| Figo, Pumpkin | Seguros, claims, bienestar y suscripción | No existe producto de seguros ni motor de claims; requiere socio regulado | Media |

## Brechas por palanca de crecimiento

### Adquisición

Existe base técnica para QR, perfiles públicos, adopciones, mapas, campañas, referidos/incentivos y partners. Falta un funnel medido de adquisición a activación, atribución por canal, onboarding optimizado y evidencia de CAC/LTV. El quick win es convertir cada scan QR, reporte de pérdida y adopción en eventos de producto con consentimiento y métricas de activación.

### Retención

Recordatorios, notificaciones, historial, familia, GPS y servicios pueden formar hábitos. Falta demostrar cohortes, retención, recuperación de churn, renovación automática y experiencia consistente entre web/PWA y proveedores. La retención debe basarse en utilidad recurrente, no en alarmas excesivas.

### Confianza y comunidad

Hay chat enmascarado, handover, fraude, moderación y bienestar. Faltan reputación verificable, resolución de disputas, antifraude de marketplace, identidad profesional portable y protocolos de crisis con refugios/municipios.

### Salud y telemedicina

El expediente y la clínica son una base fuerte. Las brechas críticas son interoperabilidad, consentimiento granular, firma/verificación clínica, receta, agenda multi-clínica, video, triage no diagnóstico y seguimiento medible. Cualquier IA clínica debe ser apoyo y nunca diagnóstico autónomo.

### IA

El matching visual es el punto de partida. Falta una capa de datos evaluados, RAG con fuentes citadas, copilotos por rol, moderación, observabilidad de modelos, evaluación de sesgos y controles de privacidad. No conviene posicionar IA generativa como actual hasta implementar y evaluar esos flujos.

## Ventaja defendible recomendada

Construir un **Pet Identity Graph de Costa Rica** con consentimiento: identidad/QR, eventos de pérdida y hallazgo, expediente verificable, proveedores y señales municipales agregadas. La defensa no sería acumular features, sino la red de confianza local, la interoperabilidad y los datos longitudinales con controles de privacidad.

## Top brechas para TOP 3 LATAM

1. Prueba repetible de reunificación y valor para el dueño.
2. Distribución local con municipalidades, refugios, clínicas y comercios.
3. Pagos regionales, conciliación, refunds, payout y antifraude.
4. Workflow clínico diario interoperable, no solo portal documental.
5. Telemedicina legalmente revisada y operable por país.
6. Hardware/partners GPS con SLA, batería, cobertura y soporte.
7. Copilotos con evidencia, evaluación y escalamiento humano.
8. Trust layer: verificación, reputación, consentimiento y disputas.
9. Localización fiscal y regulatoria por país.
10. Métricas de crecimiento: activación, retención, reunificación, NPS, GMV y margen.

## Competencia y disciplina de evidencia

Antes de afirmar que un competidor ofrece o no una capacidad, registrar fuente, URL, país, plan, fecha y nivel de verificación. Este documento es una hipótesis estratégica; el código de NALA y [NALA_CAPABILITY_MAP.md](NALA_CAPABILITY_MAP.md) son la referencia de su estado propio.
