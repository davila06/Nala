# NALA - Inteligencia competitiva y brechas de producto

> Corte de investigación: 2026-09-23. Fuentes públicas revisadas: Tractive,
> Pawfit, 11pets, PetHub, Petco Love Lost y documentación de Microsoft
> Marketplace. Esta matriz compara capacidades anunciadas públicamente; no
> presume acceso a implementaciones internas ni convierte marketing en evidencia.

## 1. Conclusión ejecutiva

NALA no debe intentar ganar como el mejor rastreador GPS ni como la mejor agenda
veterinaria. Esas categorías ya tienen líderes fuertes. La oportunidad es ser la
**red de identidad, recuperación y cuidado interoperable para mascotas**, con IA
aplicada a decisiones operativas y una capa institucional regional.

La posición competitiva actual es:

- **Ventaja real de NALA:** combinación de recuperación de mascotas, finder sin
  login, chat/contacto enmascarado, matching visual con Azure Vision, proyección
  geográfica, salud, clínicas, refugios, municipalidades, proveedores y
  collares en un mismo modelo de eventos.
- **Ventaja local potencial:** una red territorial Costa Rica-first con cantones,
  SENASA-ready, SINPE, WhatsApp, clínicas, refugios y municipalidades puede tener
  más utilidad local que una app global genérica.
- **Brecha principal:** NALA todavía no es una plataforma IA-first completa.
  Tiene modelos y automatizaciones puntuales, pero no un copiloto con
  herramientas, memoria gobernada, explicaciones, evaluación continua y
  acciones con aprobación humana.
- **Brecha de distribución:** no hay aún oferta SaaS publicada en Azure
  Marketplace, fulfillment, metering, private offers, co-sell ni paquete de
  procurement empresarial.

Por tanto, el objetivo correcto no es afirmar hoy "top 3 mundial". Es ejecutar
un plan para ser líder en Costa Rica en recuperación + identidad animal, y luego
competir globalmente en la categoría más estrecha y defendible:
**AI-native pet recovery and care network**.

## 2. Qué tiene la competencia que NALA no tiene hoy

| Competidor / categoría                  | Capacidad observada públicamente                                                                                                                                                            | Brecha de NALA                                                                                                                                                                                       | Prioridad                                                                                            |
| --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Tractive                                | Producto de hardware propio con LTE global, cobertura en 175+ países, batería de hasta semanas, geofences, actividad, sueño, signos vitales, alertas de salud y scratch/bark monitoring     | NALA tiene integración de collares y alertas, pero no hardware global propio, cobertura contractual equivalente, app madura de actividad/vitales ni una experiencia de dispositivo de escala mundial | P0 para partners OEM y P1 para hardware propio; no construir hardware antes de validar demanda       |
| Pawfit                                  | GPS 4G multi-red, voz/recall desde el dispositivo, speaker, audio ID, smart removal alert, Bluetooth finder, weight tracking, family sharing y catálogo de accesorios                       | NALA no tiene voz bidireccional/recall de dispositivo, audio ID, removal sensor ni experiencia Bluetooth de proximidad comparable                                                                    | P1: integrar capacidades vía OEM o diferenciarse por red de recuperación                             |
| 11pets                                  | Expediente y rutinas muy profundos, más de 50 funciones, compartir datos por mascota/persona/duración, agenda de negocios, citas, facturación, tickets, reportes y pagos directos en la app | NALA tiene expediente, consentimiento, clínicas y reservas, pero carece de profundidad de workflow de grooming/negocio, agenda operativa, facturación y sharing granular tan maduro                  | P1 para clínicas/proveedores; P0 solo para los workflows que aumenten recuperación o retención       |
| PetHub                                  | Tag/QR centrado en recuperación, hotline 24/7, contactos de emergencia ilimitados, records, recursos, community alerts y claim público de recuperación en 24h                               | NALA tiene finder anónimo y relay, pero aún no tiene hotline 24/7 operado, cobertura de call center, volumen público de casos ni evidencia estadística comparable                                    | P0 para un servicio de escalamiento humano en Costa Rica, con SLA y evidencia antes de prometer 24/7 |
| Petco Love Lost                         | Red de shelters/rescues y matching de mascotas perdidas/encontradas con fotografía, distribución institucional y alcance comunitario en EE. UU.                                             | NALA tiene matching visual y módulos institucionales, pero carece de red de refugios masiva, corpus de imágenes, integración shelter nacional y precisión validada a escala                          | P0 local: cerrar red de refugios y dataset consentido; P1 regional: federación de shelters           |
| AirTag / redes Bluetooth                | Gran distribución de dispositivos y red de teléfonos para ubicación oportunista, especialmente en ecosistemas Apple/Google                                                                  | NALA no tiene red de crowdsourcing de dispositivos ni integración de hardware de consumo de ese alcance                                                                                              | P1: interoperabilidad/partners; no competir frontalmente con la red del sistema operativo            |
| Apps veterinarias / pet-care verticales | Profundidad de un workflow específico: citas, recordatorios, farmacia, telemedicina, nutrición o seguros                                                                                    | NALA no tiene telemedicina, farmacia, nutrición clínica, claims de seguros ni una red de partners con SLA en todos esos servicios                                                                    | P1/P2: integrar proveedores y mantener NALA como capa de identidad/consentimiento                    |
| Marketplaces generalistas               | Tráfico, catálogo, checkout, pagos, fulfillment, reviews y promociones con alta liquidez                                                                                                    | NALA tiene directorios, pedidos/reservas y proveedores, pero no liquidez, checkout multi-vendedor, payouts, disputas ni densidad suficiente                                                          | P1: marketplace contextual y local; no activar checkout financiero antes de compliance               |

## 3. Capacidades de IA que los líderes están empezando a normalizar

Estas son las capacidades que NALA debe construir para que "IA-first" sea una
propiedad del producto y no solo el uso de Azure Vision:

1. **Copiloto operativo con herramientas:** consultar un caso, resumir
   evidencia, proponer difusión, detectar duplicados, crear una tarea y pedir
   aprobación humana. Ninguna acción sensible se ejecuta solo por texto.
2. **Matching multimodal gobernado:** foto + especie + rasgos + geografía +
   tiempo + contexto; score calibrado, explicación de señales, umbral por caso,
   feedback de falsos positivos y auditoría de modelo.
3. **Triage de riesgo y bienestar:** priorizar casos por urgencia, clima,
   proximidad, vulnerabilidad y calidad de evidencia, con reglas explícitas y
   revisión humana.
4. **Asistente de salud no diagnóstico:** resumir expediente, detectar cambios,
   preparar preguntas para el veterinario y activar recordatorios sin dar
   diagnóstico ni tratamiento autónomo.
5. **Agentes por rol:** Owner, finder, clínica, refugio, municipio y soporte,
   cada uno con permisos, fuentes, herramientas y límites distintos.
6. **RAG y conocimiento regional:** legislación, protocolos de bienestar,
   SENASA, municipalidades, refugios y proveedores con citación, fecha y
   jurisdicción; nunca responder con una fuente no trazable.
7. **MLOps y Responsible AI:** datasets consentidos, evaluación offline/online,
   drift, fairness territorial/especie, red teaming, rollback y registro de
   decisiones.
8. **Predicción accionable:** pasar de una proyección geométrica de avistamientos
   a recomendaciones de búsqueda explicables y medibles, no a una coordenada
   presentada como certeza.

## 4. Lo que NALA sí tiene y debe convertir en ventaja

- Identidad pública mínima y contacto anónimo para quien encuentra la mascota.
- Flujo QR -> pérdida -> avistamiento -> coordinación -> reunificación.
- Matching visual con embeddings de Azure Vision y matching público rápido.
- Proyección de movimiento derivada de avistamientos con incertidumbre.
- Chat enmascarado, handover seguro, fraude y moderación.
- Salud, consentimiento, grants clínicos, veterinarios y pasaporte verificable.
- Red de aliados, refugios, clínicas, municipalidades, tiendas y proveedores.
- Collares, modo perdido, zonas seguras, alertas de desconexión/batería y auditoría.
- Broadcast multicanal, WhatsApp bot, PWA/offline y analítica de funnel.
- Arquitectura Azure-native con SQL, Blob, Key Vault, Container Apps,
  Application Insights y separación de tenants/roles.

## 5. Moats que son plausibles en Costa Rica y Latinoamérica

### Moat 1 - Densidad territorial verificable

Medir mascotas activas, aliados, clínicas, refugios, tiempo de respuesta,
casos reunidos y cobertura por cantón. No comprar crecimiento vacío: ganar
territorios completos con operadores ancla.

### Moat 2 - Identidad interoperable

Un identificador de mascota que pueda ser reconocido por clínica, refugio,
municipalidad, fabricante de collar y dueño, con exportación, consentimiento y
no lock-in. El producto debe funcionar aunque el finder no tenga cuenta.

### Moat 3 - Datos de recuperación con outcome

Guardar no solo fotos y eventos, sino qué señal llevó a una reunificación,
cuánto tardó, qué canal funcionó y cuándo el modelo se equivocó. El dataset
valioso debe ser consentido, minimizado y auditable.

### Moat 4 - Operación regional

WhatsApp, español, SINPE, moneda/localización, privacidad regional, protocolos
municipales, refugios y soporte humano. La localización operativa es una barrera
más difícil de copiar que otra pantalla con IA.

## 6. Gaps que entran al roadmap ejecutivo

| ID         | Gap                            | Criterio de cierre                                                                                 |
| ---------- | ------------------------------ | -------------------------------------------------------------------------------------------------- |
| COMP-AI    | Copiloto y agentes por rol     | 3 workflows productivos con tool-calling, approval gate, trazas, evaluación y rollback             |
| COMP-MATCH | Matching multimodal explicable | Benchmark consentido, precision/recall por especie, calibración, explicación y feedback            |
| COMP-OPS   | Hotline/escalamiento humano    | SLA, turnos, protocolo, auditoría y prueba de 20 casos; no anunciar 24/7 antes                     |
| COMP-NET   | Red institucional local        | Refugios, clínicas y municipalidades activas en 2-3 cantones con outcomes publicados               |
| COMP-ID    | Identidad portable             | QR/NFC/microchip, exportación verificable, consentimientos y API versionada                        |
| COMP-MLOPS | Responsible AI                 | Model card, dataset register, eval suite, drift/fairness monitoring y incident runbook             |
| MKT-AZURE  | Azure Marketplace              | Oferta SaaS certificable, fulfillment, metering si aplica, tenant mapping, private offer y soporte |

## 7. Métricas para reclamar liderazgo

No usar "top 3 mundial" como claim hasta publicar evidencia independiente. Las
métricas mínimas son:

- Mascotas activas protegidas y porcentaje con interacción en 90 días.
- Casos de pérdida con primer response, reunificación y tiempo mediano/p90.
- Precision@5 y recall del matching por especie, calidad de foto y territorio.
- Tasa de falsos positivos, reportes abusivos y apelaciones resueltas.
- Cobertura territorial y densidad de partners activos por 10.000 hogares.
- Disponibilidad, latencia y costo por evento de recuperación.
- Retención 30/90/180 días y activación QR/NFC.
- NPS/CSAT de owner, finder, clínica, refugio y operador público.
- Porcentaje de respuestas de IA con fuente, aprobación y auditoría.
- Ingresos recurrentes B2B, cuentas activas y expansión por partner.

## 8. Fuentes externas revisadas

- Tractive: https://tractive.com/en-us
- Pawfit: https://www.pawfit.com/
- 11pets: https://www.11pets.com/
- PetHub: https://www.pethub.com/
- Petco Love Lost: https://petcolove.org/lost/
- Microsoft Marketplace partner documentation: https://learn.microsoft.com/en-us/partner-center/marketplace-offers/

Estas fuentes deben revisarse trimestralmente. Las capacidades de la competencia
cambian con rapidez y esta matriz no sustituye validación comercial directa.
