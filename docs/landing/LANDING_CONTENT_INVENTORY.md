# Inventario de contenido

## Hero messages

- Actual: identidad, recuperación y cuidado animal en Costa Rica.
- Recomendado: “Una identidad clara cuando más importa”.
- CTA: `Abrir PawTrack` y rutas específicas de pérdida/hallazgo.
- Evitar: “la mejor plataforma”, garantías de reunificación, estadísticas no medidas.

## Features y módulos

| Tema                       | Estado documental/código                            | Uso permitido                                          |
| -------------------------- | --------------------------------------------------- | ------------------------------------------------------ |
| Identidad digital          | `IMPLEMENTADO_Y_VERIFICADO`                         | Perfil y datos elegidos por el tutor                   |
| QR                         | `IMPLEMENTADO_Y_VERIFICADO`                         | Perfil/escaneo; no prometer placa física o fulfillment |
| NFC                        | `PARCIALMENTE_IMPLEMENTADO`                         | Configuración manual; no presentar NFC nativo          |
| Reporte perdido            | `IMPLEMENTADO_Y_VERIFICADO`                         | Flujo en app; cuenta y mascota registrada              |
| Hallazgo/avistamiento      | `IMPLEMENTADO_Y_VERIFICADO`                         | Flujo en app; landing no recibe datos                  |
| Contacto seguro            | `BLOQUEADO/PARCIAL`                                 | No describir como relay anónimo                        |
| Expediente y recordatorios | `IMPLEMENTADO_Y_VERIFICADO`                         | Salud documentada; no diagnóstico                      |
| GPS                        | Plataforma implementada / proveedor `NO_VERIFICADO` | Capacidad condicionada                                 |
| IA visual                  | `PARCIALMENTE_IMPLEMENTADO`                         | Matching condicionado; no precisión ni diagnóstico     |
| Telemedicina audiovisual   | `DECLARADO_NO_IMPLEMENTADO`                         | Mostrar como no disponible                             |
| Clínicas                   | Código verificado / operación externa no verificada | Capacidades, no afiliaciones                           |
| Refugios                   | Código verificado / operación externa no verificada | Adopción y aliados, no ONG concreta                    |
| Municipalidades            | Código verificado / convenios no verificados        | Reportes institucionales, no integración oficial       |
| Marketplace                | `PARCIALMENTE_IMPLEMENTADO`                         | Directorio/reservas; no checkout liquidado             |
| Planes/precios             | Aprobados en `PawTrackDev`                          | Consumir API; producción no verificada                 |

## Benefits

Identidad organizada, acción rápida ante pérdida/hallazgo, cuidado documentado y coordinación institucional. No convertir estos beneficios en resultados garantizados.

## Trust builders

- Límites explícitos.
- Estados de implementación.
- Catálogo proveniente de API pública aprobada.
- No publicar partners, testimonios o métricas sin evidencia.
- Privacidad y contacto con ownership pendiente donde corresponde.

## Statistics

No existen métricas públicas aprobadas de usuarios, reunificaciones, cobertura, precisión IA, SLA o impacto. No publicar números.

## Partners y alianzas

El código contiene superficies para clínicas, refugios, proveedores y municipalidades. No se publican logos, convenios ni cobertura activa sin evidencia contractual/operativa.

## FAQs

- QR/NFC/GPS no son equivalentes.
- Qué hacer al perder o encontrar una mascota.
- Qué datos se comparten.
- Telemedicina audiovisual no disponible.
- Planes consumidos desde catálogo público.

## Calls to action

- Primario: `Abrir PawTrack`.
- Pérdida: login con retorno contextual.
- Hallazgo: flujo público de hallazgo.
- Organizaciones: ver capacidades y límites.
- Planes: consultar catálogo API, sin checkout en landing.

## Missing / weak / duplicated content

- Falta canal oficial de contacto verificable.
- Falta evidencia pública de operación, partners y métricas.
- Copy histórico de precios debe permanecer fuera del landing.
- No duplicar `SubscriptionPlans` en markdown, frontend o tablas comerciales.
