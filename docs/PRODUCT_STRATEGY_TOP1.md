# PawTrack CR - Estrategia para ser el ecosistema #1 animal

> Estado: activo para estrategia y discovery. No sustituye contratos legales ni
> declara capacidades no verificadas. Corte: 2026-09-09.

## 1. Tesis central

PawTrack no debe competir como otra app de mascotas, otro mapa de perdidos o
otro marketplace. Debe convertirse en la **capa de identidad, confianza y
coordinacion de cada animal por territorio**.

El activo defensible no es el QR aislado: es el grafo de relaciones y eventos
entre mascota, familia, finder, clinica, tienda, refugio, municipalidad,
proveedor y evidencia temporal/geografica.

## 2. North Star Metric

**Mascotas activas protegidas que pueden completar un evento de cuidado o
recuperacion verificable en los ultimos 90 dias.**

Un animal cuenta cuando tiene perfil util (foto/contacto/QR o microchip), al
menos un propietario confirmado y una interaccion verificable: escaneo,
actualizacion de salud, alerta, avistamiento, adopcion o handover.

Esta metrica evita optimizar registros vacios. Debe acompañarse de:

- activacion: registro -> perfil completo -> QR activado;
- densidad por canton: mascotas activas, aliados y clinicas por 10,000 hogares;
- recuperacion: perdida reportada -> primer avistamiento -> reunificacion;
- velocidad: tiempo a primer respuesta y tiempo a reunificacion;
- confianza: reportes validos, falsos positivos, fraude y disputas;
- retencion: mascotas con actividad a 30/90/180 dias;
- conversion: planes, QR fisico, servicios y partners;
- equidad territorial: cobertura de cantones y poblaciones vulnerables.

## 3. Posicionamiento ganador

> La identidad digital y red local que ayuda a cuidar, encontrar y devolver a
> cada mascota, incluso cuando quien la encuentra no tiene la app.

Pilares:

1. **Siempre identificable:** QR/NFC/microchip, perfil publico minimo y contacto
   seguro.
2. **Siempre recuperable:** alertas, mapa, avistamientos, IA, broadcast y
   handover.
3. **Siempre cuidable:** expediente, vacunas, recordatorios y clinicas.
4. **Siempre conectado al territorio:** vecinos, refugios, municipalidades,
   tiendas y servicios.
5. **Siempre confiable:** consentimiento, trazabilidad, reputacion y antiabuso.

## 4. Las apuestas que pueden crear liderazgo

### A. Finder de 30 segundos

Un finder debe poder escanear QR, abrir perfil, contactar sin PII y reportar
ubicacion/foto sin crear cuenta. El flujo debe funcionar en movil lento, sin
instalar nada y con fallback SMS/WhatsApp.

**Moat:** cada encuentro alimenta la red aunque el finder nunca se registre.

### B. Pasaporte de mascota interoperable

Un perfil portable con identidad, QR/NFC, microchip, vacunas, alergias,
contactos, documentos y consentimiento granular. Exportable y verificable, sin
encerrar al dueño en PawTrack.

**Moat:** el historial util crea costos de cambio honestos y mejora cada visita.

### C. Dominio territorial antes que expansion vacia

Lanzar cantones como unidades de crecimiento: activar dueños, clinicas,
refugios, comercio y funcionarios en el mismo territorio. Publicar cobertura,
tiempo de respuesta y casos reunidos con denominadores transparentes.

**Moat:** densidad local hace que cada nueva mascota aumente el valor para todas.

### D. Centro de confianza

Reputacion separada por rol: finder, aliado, tienda, clinica y proveedor.
Combinar verificacion, historial de aportes, evidencia, reportes de fraude,
moderacion y derecho de apelacion.

**Moat:** reduce ruido y convierte seguridad en ventaja competitiva.

### E. Red de cuidado contextual

El estado del animal debe activar acciones relevantes: vacuna vencida -> clinica;
perdido -> red local y servicios; adoptable -> refugio; QR escaneado -> contacto
seguro; alta de riesgo -> protocolo de bienestar.

**Moat:** el marketplace deja de ser publicidad y se vuelve infraestructura de
cuidado.

### F. Infraestructura institucional abierta

Ofrecer API y exports versionados a municipalidades, clinicas, refugios,
aseguradoras y fabricantes. Mantener consentimientos, auditoria y minimizacion.

**Moat:** estandar de identidad animal local, no solo una app de consumidor.

## 5. Mejoras necesarias por horizonte

### Ahora: confiabilidad y activacion

- [ ] Cambiar claims de produccion por estados verificables.
- [ ] Crear funnel de onboarding y evento de activacion QR.
- [ ] Simplificar home en Mi mascota, Perdida/Encontrada, Salud y Red local.
- [ ] Finder sin login con QR, foto, ubicacion, contacto enmascarado y offline.
- [ ] Completar E2E del ciclo de recuperacion.
- [ ] Limpiar fuentes documentales y fijar una unica matriz comercial.
- [ ] Cerrar lint, contratos API, secret scanning y gates CI.
- [ ] Resolver alertas, errores de red y observabilidad de negocio.

### Siguiente: confianza y red

- [ ] Perfil de confianza y reputacion por rol.
- [ ] Moderacion de contenido, fraude, apelaciones y cola de seguridad.
- [ ] Pasaporte digital con permisos por dato y exportacion verificable.
- [ ] Campañas territoriales para QR/NFC subsidiado con clinicas y municipios.
- [ ] Matching de mascota encontrada con explicacion, confianza y control de
      falsos positivos.
- [ ] Programa de aliados con respuesta, cobertura y calidad medibles.
- [ ] Marketplace contextual basado en necesidad, no solo posicionamiento.

### Despues: plataforma regional

- [ ] API partner versionada con sandbox, scopes, webhooks y changelog.
- [ ] Interoperabilidad de identidad animal entre organizaciones.
- [ ] Soporte de paises, moneda, privacidad y regulacion por jurisdiccion.
- [ ] Integraciones de seguros, telemedicina y hardware con contratos claros.
- [ ] Modelos predictivos de riesgo territorial auditables y no discriminatorios.

## 6. Lo que no se debe construir todavia

- [ ] Otra red social generalista sin densidad local.
- [ ] IA decorativa que no reduzca tiempo a recuperacion o mejore cuidado.
- [ ] Checkout multi-proveedor antes de resolver confianza y soporte.
- [ ] Custodia de dinero o payouts sin compliance financiero.
- [ ] Rankings que expongan ubicaciones privadas o incentiven spam.
- [ ] Promesas de certificacion estatal sin convenio y validacion externa.
- [ ] Expansion LATAM antes de probar un canton con metricas fuertes.

## 7. Modelo de crecimiento recomendado

1. Elegir 2-3 cantones de prueba con aliados ancla.
2. Capturar mascotas existentes mediante QR/NFC y clinicas.
3. Crear eventos de activacion: vacunacion, adopcion y registro municipal.
4. Medir activacion, densidad, respuesta y recuperacion semanalmente.
5. Mejorar el producto segun fricciones observadas, no segun volumen de features.
6. Publicar resultados agregados y casos verificables.
7. Repetir en cantones adyacentes cuando el territorio anterior alcance el SLO.

## 8. Guardrails de confianza

- Minimizacion y consentimiento para salud, ubicacion y contacto.
- Finder anonimo por defecto; contacto enmascarado.
- IA como apoyo, nunca como veredicto unico.
- Toda moderacion importante debe ser auditable y apelable.
- Metricas publicas con denominadores, fechas y limites estadisticos.
- No vender datos individuales de mascotas, personas o ubicaciones.
- Feature flags para experimentos de alto riesgo.

## 9. Roadmap de 12 meses

| Tramo        | Resultado esperado                         | Evidencia                                 |
| ------------ | ------------------------------------------ | ----------------------------------------- |
| 0-60 dias    | Activacion y finder sin friccion           | Funnel, E2E, tiempo a reporte             |
| 60-120 dias  | Densidad en cantones ancla                 | Mascotas activas, aliados y SLO           |
| 120-180 dias | Centro de confianza y pasaporte            | Verificaciones, exportes y fraude         |
| 180-270 dias | Red institucional y marketplace contextual | Partners activos, conversion y retencion  |
| 270-365 dias | Expansion regional selectiva               | Repetibilidad por territorio y compliance |

## 10. Decisiones de liderazgo

- El exito se define por animales protegidos y reunidos, no por pantallas
  construidas.
- La red local es el producto; los modulos son puertas de entrada.
- Confianza, privacidad y evidencia son ventajas, no burocracia.
- Cada nueva feature debe mejorar activacion, densidad, velocidad, confianza o
  sostenibilidad; si no, queda fuera del roadmap.
