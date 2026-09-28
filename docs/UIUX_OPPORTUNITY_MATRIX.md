# Matriz de Oportunidades UX/UI

**Corte:** 2026-09-28. Priorización inicial, no estimación financiera. No existen en esta auditoría datos de funnel, satisfacción, soporte ni revenue para calcular ROI real.

## Método de prioridad

Escala 1-5. `IU` impacto usuario, `IB` impacto negocio, `U` urgencia, `E` esfuerzo (1 bajo, 5 alto). `Score = (IU + IB) × U / E`; desempate por protección de vida/mascota, privacidad y reversibilidad. La certeza distingue problema observado en código (`Alta`) de hipótesis que requiere ejecución/entrevista (`Media/Baja`). Prioridad recomendada: score ≥20 o bloqueo de tarea crítica = P0; 12-19.9 = P1; menor de 12 = P2. Ajustar tras investigación y revisión de negocio.

## Oportunidades

|    ID | Oportunidad                                          | Área/actor                                          |  IU |  IB |   U |   E | Score | Certeza y señal de éxito                                         |
| ----: | ---------------------------------------------------- | --------------------------------------------------- | --: | --: | --: | --: | ----: | ---------------------------------------------------------------- |
| OP-01 | Corregir contraste de tokens pequeños                | Todas las personas, WCAG                            |   5 |   5 |   5 |   3 |  16.7 | Alta; todos los pares de texto normal AA                         |
| OP-02 | Accesibilizar wizard de reporte perdido              | Tutores                                             |   5 |   5 |   5 |   2 |  25.0 | Alta; completitud de tarea por teclado/screen reader             |
| OP-03 | Añadir CTA móvil persistente para reportar pérdida   | Tutores en crisis                                   |   5 |   5 |   5 |   2 |  25.0 | Alta; tiempo desde dashboard a formulario                        |
| OP-04 | Añadir alternativa de lista a mapa y pin             | Tutores, personas con discapacidad                  |   5 |   4 |   5 |   4 |  11.3 | Media; igualdad de tareas mapa/lista                             |
| OP-05 | Completar focus trap del onboarding                  | Nuevos tutores                                      |   4 |   4 |   5 |   2 |  20.0 | Alta; foco confinado y devuelto                                  |
| OP-06 | Teclado completo de QRFlipCard                       | Tutores, halladores                                 |   4 |   5 |   5 |   2 |  22.5 | Alta; QR se revela/descarga sin puntero                          |
| OP-07 | Validación por paso y foco de error en reporte       | Tutores                                             |   5 |   5 |   5 |   3 |  16.7 | Alta; errores corregibles sin perder datos                       |
| OP-08 | Dialogs accesibles uniformes                         | Todas las personas                                  |   4 |   4 |   4 |   4 |   8.0 | Alta; teclado, Escape, foco y nombre                             |
| OP-09 | Accesibilidad de PhotoUpload                         | Tutores/clínicas/refugios                           |   4 |   4 |   5 |   2 |  20.0 | Alta; subida completa sin pointer                                |
| OP-10 | Divulgación de ubicación antes de pedir permiso      | Tutores                                             |   5 |   4 |   4 |   2 |  18.0 | Alta; consentimiento informado, entrada manual disponible        |
| OP-11 | Navegación móvil según rol                           | Clínicas, tiendas, proveedores, municipios, aliados |   5 |   5 |   5 |   3 |  16.7 | Media; éxito de tareas top por rol en móvil                      |
| OP-12 | Copy veraz sobre IA, QR y NFC                        | Tutores/ventas/soporte                              |   4 |   5 |   5 |   2 |  22.5 | Alta; comprensión de lo que sí/no hace el producto               |
| OP-13 | Instrumentación consentida del flujo de recuperación | Producto/operaciones                                |   4 |   5 |   4 |   3 |  12.0 | Media; embudo medible sin PII/GPS preciso                        |
| OP-14 | E2E axe/teclado por journeys                         | QA/producto                                         |   4 |   5 |   4 |   3 |  12.0 | Alta; violaciones críticas bloquean release                      |
| OP-15 | Borradores/recuperación de formularios largos        | Refugios, clínicas, proveedor                       |   4 |   4 |   4 |   3 |  10.7 | Media; abandono recuperable                                      |
| OP-16 | Estado de consentimiento y adjunto en expediente     | Tutores/veterinarias                                |   5 |   4 |   4 |   3 |  12.0 | Media; acceso correcto sin dudas                                 |
| OP-17 | Navegación de tareas clínicas en tablet              | Veterinarias                                        |   4 |   5 |   4 |   4 |   9.0 | Media; tiempo por tarea y errores de sede                        |
| OP-18 | Claridad de estados de caja/transacción              | Clínicas                                            |   5 |   5 |   4 |   3 |  13.3 | Media; menos doble cobro/reintento                               |
| OP-19 | Flujo de autorizaciones clínicas/revocación          | Tutores/clínicas                                    |   5 |   5 |   4 |   4 |  10.0 | Media; autorización/revocación completada sin soporte            |
| OP-20 | Paginación y filtros persistentes en directorios     | Marketplace/adopción                                |   3 |   4 |   3 |   3 |   7.0 | Media; búsqueda exitosa/retorno sin perder filtros               |
| OP-21 | Estado de disponibilidad y reserva inequívoca        | Servicios                                           |   4 |   5 |   4 |   3 |  12.0 | Media; reservas no confundidas con pago confirmado               |
| OP-22 | Transparencia de fees, impuestos y cancelación       | Tiendas/servicios/pagos                             |   5 |   5 |   5 |   3 |  16.7 | Media; menos disputa/abandono y reclamo                          |
| OP-23 | Edición de publicación con preview/borrador          | Refugios                                            |   4 |   4 |   3 |   3 |   8.0 | Media; publicación completa a la primera                         |
| OP-24 | Bulk status con preview y undo                       | Municipalidades                                     |   5 |   4 |   4 |   3 |  12.0 | Media; menos correcciones de registros                           |
| OP-25 | Vista de captura con modo baja conectividad          | Municipalidades                                     |   5 |   4 |   4 |   4 |   9.0 | Media; captura de campo terminada sin señal                      |
| OP-26 | Progreso verificable de revisión de negocio          | Clínicas/refugios/tiendas                           |   4 |   4 |   3 |   2 |  12.0 | Alta; solicitudes con menos consultas de estado                  |
| OP-27 | Segmentación del onboarding por actor                | Todos los roles                                     |   4 |   5 |   4 |   4 |   9.0 | Media; activación por cohorte/rol                                |
| OP-28 | Upsell contextual no intrusivo                       | Tutores                                             |   3 |   5 |   3 |   2 |  12.0 | Media; conversión neta y no descenso de confianza                |
| OP-29 | Eliminar upsell durante tarea de emergencia          | Tutores                                             |   5 |   4 |   5 |   1 |  45.0 | Alta si callsites lo muestran; cero interrupción del flujo       |
| OP-30 | Activación de primer valor QR                        | Tutores                                             |   4 |   5 |   4 |   3 |  12.0 | Media; perfil publicado y QR probado                             |
| OP-31 | Mapa de límites/claims del GPS                       | Tutores                                             |   4 |   4 |   4 |   2 |  16.0 | Alta; expectativa entendida, devoluciones por cobertura          |
| OP-32 | Alternativa de contacto si notificaciones fallan     | Tutores/aliados                                     |   4 |   4 |   4 |   3 |  10.7 | Media; alerta confirmada/estado legible                          |
| OP-33 | Lista accesible de alertas/eventos                   | Tutores/clínicas                                    |   4 |   3 |   4 |   3 |   9.3 | Media; tareas disponibles sin color/mapa                         |
| OP-34 | Señalar fuente y nivel de verificación en salud      | Tutores/veterinarias                                |   5 |   4 |   4 |   3 |  12.0 | Alta; usuarios distinguen declaración de registro verificado     |
| OP-35 | Revisión de textos preventivos no diagnósticos       | Tutores                                             |   5 |   3 |   4 |   2 |  16.0 | Media; validación clínica y menor interpretación clínica errónea |
| OP-36 | Tabla/lista adaptable para catálogos grandes         | Admin y B2B                                         |   3 |   3 |   3 |   3 |   6.0 | Baja; scroll horizontal/zoom sin pérdida de columnas             |
| OP-37 | Reflow y zoom 200-400%                               | Todas las personas                                  |   5 |   4 |   4 |   4 |   9.0 | Baja; pruebas viewport sin contenido recortado                   |
| OP-38 | Temas dark/light consistentes                        | Personas con preferencias del sistema               |   3 |   2 |   2 |   3 |   3.3 | Media; sin controles ilegibles por hardcoded colors              |
| OP-39 | Rendimiento percibido de dashboards                  | B2B/admin                                           |   4 |   4 |   3 |   4 |   6.0 | Baja; LCP/INP/CLS de producción/staging                          |
| OP-40 | Pruebas de usabilidad moderadas                      | Todos los roles                                     |   5 |   5 |   4 |   4 |  10.0 | Alta necesidad; tasa de éxito por tareas prioritarias            |

## Matriz de producto por experiencia

| Dominio         | UI observable en código                                                | Oportunidad                                                                     | Estado de experiencia                                     |
| --------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------- | --------------------------------------------------------- |
| Telemedicina    | No hay UI de llamada/sala; existen consultas clínicas registradas      | No presentar CTA futuro como disponible; diseñar solo tras decisión/proveedor   | No implementado                                           |
| QR              | Perfil QR, descarga/flip card y lectura pública                        | Hacer la acción explícita, teclado completa, impresión/escaneo probados         | Parcialmente verificado visualmente                       |
| NFC             | Tutorial manual externo, aparece tras pedido combo entregado           | Reescribir guía y verificar URL/compatibilidad, sin llamarlo integración nativa | Tutorial presente; pairing no implementado                |
| Veterinarias    | Registro, directorio, perfiles, dashboard, caja, staff/CRM, expediente | Tareas en tablet, identidad/sede/grant persistentes, transacción confirmable    | Implementado en UI; validación de uso real pendiente      |
| Refugios        | Dashboard, publicar, solicitudes y directorio de adopción              | Borradores, procesamiento de fotos, revisión de requisitos, acciones por lote   | Implementado en UI; validación de operadores pendiente    |
| Municipalidades | Portal público, dashboard/capturas y reportes                          | Campo/offline, bulk-safe, listas alternativas, acceso por rol                   | Implementado en UI; validación con funcionarios pendiente |
| IA visual       | Entrada a matching/búsqueda foto y copy de IA                          | Explicar disponibilidad/confianza, errores y alternativa de reporte manual      | Parcial; proveedor no evaluado aquí                       |
| Marketplace     | Catálogos, perfiles, booking, order y portales                         | Diferenciar solicitud/confirmación/pago/inventario y estado cancelación         | Parcial; pagos no acreditados                             |

## Riesgo residual

La matriz no incluye mediciones de conversión ni impacto monetario real porque no hay baseline y no se revisaron datos de producción. No se ejecutó un browser visual por falta de runtime de API/DB configurado para esta auditoría; los valores de contraste son cálculos de tokens/pares muestreados, no un crawl completo de estilos computados.
