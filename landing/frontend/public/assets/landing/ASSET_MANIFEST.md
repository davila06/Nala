# Manifest de assets del landing

**Paquete recibido:** 2026-10-01. **Formato preferido:** AVIF, con WebP como fallback.

| Grupo         | Archivos                                                               |  Dimensión declarada | Uso                    | Alt text base                                         | Estado                                               |
| ------------- | ---------------------------------------------------------------------- | -------------------: | ---------------------- | ----------------------------------------------------- | ---------------------------------------------------- |
| Hero          | `desktop/landing-hero-01-desktop.*`, `mobile/landing-hero-01-mobile.*` | 1600×900 / 1080×1350 | Hero principal         | Familia reunida con su perro en un parque urbano      | Integrado; derechos/origen por confirmar             |
| QR            | `desktop/qr-scan-01-desktop.*`, `mobile/qr-scan-01-mobile.*`           | 1600×900 / 1080×1350 | Identidad QR           | Persona escaneando el identificador de una mascota    | Disponible; no implica placa física                  |
| Comunidad     | `community-01-*`                                                       |       Desktop/mobile | Recuperación y aliados | Personas coordinando el cuidado de una mascota        | Conceptual; no prueba alianzas                       |
| Veterinaria   | `veterinary-01-*`                                                      |       Desktop/mobile | Salud                  | Veterinaria atendiendo una mascota junto a su tutor   | Capacidad de código; operación externa no verificada |
| Refugio       | `shelter-01-*`                                                         |       Desktop/mobile | Adopción               | Familia conociendo una mascota durante una adopción   | Conceptual; no prueba ONG afiliada                   |
| Municipalidad | `municipality-01-*`                                                    |       Desktop/mobile | Institucional          | Familias en una jornada comunitaria de identificación | Conceptual; no prueba convenio                       |
| Producto      | `product-identity-01-*`                                                |       Desktop/mobile | Tag de identidad       | Render conceptual de un tag de identidad              | Conceptual; no vender como hardware                  |
| Futuro IoT    | `future-iot-01-*`                                                      |       Desktop/mobile | Visión futura          | Concepto visual de collar inteligente                 | FUTURE_VISION                                        |

Cada grupo contiene variantes AVIF/WebP y una fuente PNG en `source/`. El origen, herramienta, prompt, fecha y derechos deben confirmarse antes de producción. No se incluyen QR que enlacen a perfiles reales ni claims de disponibilidad derivados solo de una imagen.

## Assets generados para el landing

Generados el **2026-10-01** con Azure AI Image Generation (`gpt-image-2.5-flare`). Las fuentes PNG se conservan en `source/`; `npm run images:optimize` genera AVIF y WebP en `desktop/` (1600×900) y `mobile/` (1080×1350). Las escenas son ilustrativas: no acreditan prestadores afiliados, disponibilidad veterinaria, venta de placas ni un QR funcional. Revisar los términos de Azure AI para publicación comercial antes de producción.

| Asset         | Fuente                                             | Uso       | Brief visual                                                                               |
| ------------- | -------------------------------------------------- | --------- | ------------------------------------------------------------------------------------------ |
| Hero familiar | `source/nala-hero-family-reunion-20261001.png`     | Inicio    | Familia con su perro en un parque urbano costarricense; sin texto ni interfaz.             |
| Directorio    | `source/nala-services-care-directory-20261001.png` | Servicios | Perro acompañado por una veterinaria y su tutora; sin marca ni indicación de afiliación.   |
| Identidad     | `source/nala-qr-scan-identity-20261001.png`        | Página QR | Persona con teléfono junto a una placa lisa; no contiene QR legible ni interfaz.           |
| Dogtag demo   | `source/nala-qr-scan-identity-tagged-20261001.png` | Recorrido | QR legible superpuesto a la placa; solo codifica `NALA DEMO`, sin URL ni datos personales. |

### Categorías de servicios

Generadas el **2026-10-01** con Azure AI Image Generation (`gpt-image-2.5-flare`). `npm run images:optimize` crea variantes AVIF/WebP de 1600×900 en escritorio y 1080×608 en móvil, adecuadas para tarjetas panorámicas. Son escenas conceptuales; no representan prestadores, alianzas, certificaciones ni disponibilidad real. Revisar los términos de Azure AI para publicación comercial antes de producción.

| Categoría        | Fuente PNG                                   |
| ---------------- | -------------------------------------------- |
| Veterinarias     | `source/service-veterinary-20261001.png`     |
| Grooming         | `source/service-grooming-20261001.png`       |
| Entrenamiento    | `source/service-training-20261001.png`       |
| Hospedaje        | `source/service-boarding-20261001.png`       |
| Paseos           | `source/service-walking-20261001.png`        |
| Cuidado temporal | `source/service-temporary-care-20261001.png` |

### Reporte de mascota perdida

Generadas el **2026-10-02** con Azure AI Image Generation (`gpt-image-2.5-flare`). Las escenas son conceptuales y no muestran direcciones, coordenadas, contactos legibles ni casos reales. `npm run images:optimize` produce AVIF/WebP de 1600×900 en escritorio y 1080×608 en móvil.

| Paso                      | Fuente PNG                                      | Brief visual                                                                |
| ------------------------- | ----------------------------------------------- | --------------------------------------------------------------------------- |
| Preparar una descripción  | `source/lost-pet-preparation-20261002.png`      | Tutora revisa una foto reciente y prepara información en casa.              |
| Compartir zona aproximada | `source/lost-pet-approximate-area-20261002.png` | Persona consulta mapa sin nombres de calles ni coordenadas legibles.        |
| Revisar contacto          | `source/lost-pet-contact-review-20261002.png`   | Persona revisa perfil en teléfono con la pantalla intencionalmente borrosa. |

### Capacidades de refugios

Generadas el **2026-10-02** con Azure AI Image Generation (`gpt-image-2.5-flare`). Son escenas ilustrativas y no prueban afiliación, adopciones completadas, convenios, SLA ni cobertura operativa. `npm run images:optimize` produce AVIF/WebP de 1600×900 en escritorio y 1080×608 en móvil.

| Capacidad                | Fuente PNG                                 | Brief visual                                                                        |
| ------------------------ | ------------------------------------------ | ----------------------------------------------------------------------------------- |
| Perfiles aliados         | `source/shelter-profiles-20261002.png`     | Voluntaria revisa el perfil de un perro; no identifica a una organización real.     |
| Publicaciones y adopción | `source/shelter-adoptions-20261002.png`    | Primer encuentro entre persona, perro y voluntaria; no representa adopción cerrada. |
| Convenios y cobertura    | `source/shelter-partnerships-20261002.png` | Conversación exploratoria sin documentos firmados ni señales de acuerdo formal.     |
