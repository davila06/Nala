# PawTrack CR — QR: Guía Completa

> Cubre todo lo relacionado con QR en la plataforma: generación, contenido, flujo de escaneo público, integración clínica, telemetría, retención y convivencia con los CollarTags.  
> Última actualización: 2026-09-01

---

## 1. Qué codifica el QR

Cada mascota registrada tiene **un único QR permanente** que codifica su URL de perfil público:

```
https://pawtrack.cr/p/{petId}
```

El `petId` es un `Guid v7` generado al crear la mascota. Esta URL nunca cambia mientras la cuenta esté activa.

**Lo que NO codifica el QR:**

- Datos privados del dueño
- Teléfono, dirección, email
- Estado de pérdida (el perfil es dinámico — muestra el estado actual en tiempo real)

---

## 2. Generación del QR (backend)

### Librería y configuración

```csharp
// backend/src/PawTrack.Infrastructure/Pets/QrCodeService.cs
public sealed class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }
}
```

| Parámetro           | Valor              | Razón                                                           |
| ------------------- | ------------------ | --------------------------------------------------------------- |
| Librería            | `QRCoder 1.6.0`    | Pura C#, sin dependencias nativas                               |
| Nivel de corrección | `ECCLevel.M` (15%) | Balance entre densidad y legibilidad para superficies de collar |
| Píxeles por módulo  | 10                 | ~330×330 px de salida — legible a 5 cm en impresión de 600 dpi  |
| Formato de salida   | PNG, `image/png`   | Compatible con todo hardware de impresión y navegadores         |

### Endpoint

```
GET /api/pets/{id}/qr
Authorization: Bearer {jwt}   ← solo el dueño
Rate limit: 30/min (CPU-intensivo)
```

El endpoint verifica `OwnerId == UserId` antes de generar — nadie puede descargar el QR de una mascota ajena.

---

## 3. Dónde aparece el QR en la app

### 3.1 Tab QR en PetDetailPage

Componente `QRFlipCard` — tarjeta 3D con:

- **Frente:** foto del perfil o avatar por especie
- **Reverso:** QR descargable + botón de pedido de collar físico
- Flip por toque, click o swipe horizontal
- Descarga como `qr-{nombre}.png`
- Lazy-load: el PNG del QR solo se solicita al girar la tarjeta por primera vez

### 3.2 Foto de avatar para WhatsApp

`WhatsAppAvatarComposer` compone una imagen con fondo de perfil y el QR en overlay — apta para ser la foto de perfil del dueño en WhatsApp para máxima visibilidad al ser contactado.

### 3.3 Volante de mascota perdida

`LostReportConfirmationPage` y `useGenerateFlyer` pre-cargan el QR como `data:image/png;base64,...` para insertarlo en el `SearchFlyerTemplate` (HTML→canvas→PDF). El QR aparece impreso en el volante para que cualquier persona que lo encuentre pueda escanear sin tener la app.

### 3.4 Imagen para redes sociales

`SocialShareImageTemplate` incluye el QR en la franja inferior de la imagen de pérdida compartida en WhatsApp/Facebook.

### 3.5 Tarjeta de identidad PDF

`IPetIdCardService` genera una tarjeta A6 en PDF que incluye el QR codificando la `PublicProfileUrl`. Entregable físico imprimible.

### 3.6 Certificados veterinarios y pasaportes

`QuestPdfCertificateService` embebe el QR en certificados PDF de vacunación y pasaportes para la misma URL pública.

### 3.7 Pantalla de activación de CollarTag

`ActivateCollarTagPage` usa el mismo `BarcodeDetector` de la cámara para escanear el QR grabado en el collar y extraer el serial `PT-XXXX-NNNNNNN`.

---

## 4. Flujo cuando alguien escanea el QR

```
Persona escanea QR con la cámara del celular
        ↓
Navegador abre https://pawtrack.cr/p/{petId}
        ↓
Frontend renderiza PublicPetProfilePage (React, sin login)
        ↓
GET /api/public/pets/{id}  →  PublicController
        ↓ (en paralelo, best-effort)
RecordPublicQrScanCommand  →  graba QrScanEvent en DB
        ↓
Si mascota está Lost y es el primer escaneo del día
  → notificación push/in-app al dueño: "Escaneo QR de {nombre} desde {ciudad}"
        ↓
Si hay reporte activo + (escaneo cerca de casa OR primer escaneo tras silencio)
  → notificación ResolveCheck: "¿Encontraste a {nombre}?"
```

### Qué muestra el perfil público

- Foto, nombre, especie, raza
- Banner de alerta roja si el estado es `Lost`
- Mensaje personalizado del dueño (seteado al reportar pérdida)
- Botón para reportar avistamiento
- Botón de contacto anónimo al dueño (sin exponer teléfono/email)
- Clínicas veterinarias asociadas si hay alguna como ClinicPartner

---

## 5. Telemetría de escaneo (QrScanEvent)

### Campos almacenados

```csharp
public sealed class QrScanEvent
{
    Guid PetId
    string? ScannedByUserId   // Guid si el escaneador está logueado
    string? IpAddress         // SHA-256 del IP real — nunca texto plano
    string? CountryCode       // ISO 3166-1 alfa-2
    string? CityName          // Ciudad aproximada
    string? UserAgent
    DateTimeOffset ScannedAt
}
```

**Privacidad por diseño:**

- IP almacenada como SHA-256 irreversible — no puede reconstruirse el IP real
- No se almacena la ubicación GPS del escaneador (solo ciudad por header)
- `ScannedByUserId` es opcional — funciona anónimo

### Retención

`QrScanRetentionJob` corre diariamente a las 02:00 hora CR (UTC-6):

- Elimina registros con `ScannedAt < now - RetentionDays`
- Valor por defecto: **90 días**
- Configurable en `appsettings.json` bajo `QrScanRetention:RetentionDays`

### Exposición al dueño

El dueño ve en `PetDetailPage → tab QR`:

- Contador de escaneos hoy
- Lista de escaneos recientes (ciudad, fecha/hora)
- Botón "Exportar cadena de custodia QR" → PDF firmado con jsPDF

---

## 6. Escaneo por clínicas veterinarias

Las clínicas con plan `ClinicStandard` o superior pueden escanear el QR para identificar mascotas en consulta. Hay dos vías:

### Vía A — Dashboard de clínica (manual)

`ScanInput.tsx` en `ClinicDashboardPage`:

- Usa `BarcodeDetector` nativo (sin librería extra)
- Fallback: campo de texto manual
- Heurística: URLs → `ScanInputType.Qr`, texto corto → `ScanInputType.RfidChip`

Llama a `PerformClinicScanCommand`:

```
PerformClinicScanCommand(ClinicId, Input, InputType)
  ↓
Extrae petId de /p/{guid} con regex
  ↓
Busca la mascota por petId (QR) o microchipId (RFID)
  ↓
Registra ClinicScan (audit)
  ↓
Notifica al dueño
```

### Vía B — API machine-to-machine (ClinicPartner)

```
GET /api/v1/pets/lookup?qr={url}
GET /api/v1/pets/lookup?chip={microchip}
Authorization: X-PawTrack-Key: {clinicApiKey}
```

El mismo `PerformClinicScanCommand` — sin diferencia de lógica.

### Acceso al expediente

Después de un escaneo, la clínica puede acceder al historial médico de esa mascota. Dos modos:

- **Opción A:** `petId` de un escaneo previo → `GetPetMedicalHistoryForClinicQuery`
- **Opción B:** QR escaneado en el mismo momento de la consulta (inline) → `AddClinicMedicalRecordCommand`

---

## 7. Productos físicos con QR

Los bundles del catálogo incluyen QR físico en distintos formatos:

| Producto               | `BundleProductType` | Precio  | Descripción                                                 |
| ---------------------- | ------------------- | ------- | ----------------------------------------------------------- |
| Placa QR de aluminio   | `QrPlate = 1`       | ₡4,500  | 3×5 cm, grabado láser, resistente al agua                   |
| Tag de silicona con QR | `SiliconeTag = 2`   | ₡5,500  | Flexible, colores, impresión UV                             |
| Combo NFC + QR         | `NfcQrCombo = 3`    | ₡12,000 | Chip NTAG213 + placa QR — toca con Android, escanea con iOS |
| Pack emergencia        | `EmergencyPack = 4` | ₡7,000  | Placa QR + tarjeta billetera de emergencia                  |
| Bundle GPS + Plus      | `CollarGpsPlus = 0` | ₡49,900 | Tractive DOG/CAT + 12 meses Plus                            |
| CollarTag GPS + Plus   | `CollarTagGps = 5`  | ₡39,900 | Collar GPS PawTrack de marca propia + 12 meses Plus         |

El QR físico de todos estos productos codifica la misma URL `/p/{petId}` — no hay distinción de tipo en el backend.

---

## 8. Convivencia QR ↔ CollarTag

El CollarTag tiene **dos códigos físicos distintos** con propósitos diferentes:

```
COLLAR FÍSICO (CollarTag)
│
├── QR de identidad (igual que en placas independientes)
│   └── Codifica: https://pawtrack.cr/p/{petId}
│   └── Uso: cualquier persona que encuentre la mascota escanea → perfil público
│   └── Grabado láser en el enclosure del collar
│
└── QR / serial de activación (específico del CollarTag)
    └── Codifica: PT-[4hex]-[7dig]  ──O──  URL con el serial embebido
    └── Uso: solo en el flujo de activación del dueño
    └── Impreso en papel dentro de la caja (o legible directamente del enclosure)
    └── ActivateCollarTagPage lo escanea con ScanInput.tsx → extractSerial()
    └── Solo útil UNA VEZ (al vincular el collar a la mascota)
```

### ¿Colisionan?

No. Son formatos completamente distintos:

| Característica    | QR de identidad                          | QR/serial de activación                           |
| ----------------- | ---------------------------------------- | ------------------------------------------------- |
| Contenido         | URL `/p/{guid}`                          | `PT-XXXX-NNNNNNN` o URL con serial                |
| Quién lo usa      | Cualquier persona                        | Solo el dueño durante la activación               |
| Frecuencia de uso | Cada vez que alguien lo escanea          | Una sola vez                                      |
| Registro en DB    | `QrScanEvent`                            | `CollarTag.Activate()` + `CollarDeviceCredential` |
| Codificado por    | `IQrCodeService.GeneratePng(/p/{petId})` | Generado en fábrica / por el admin                |
| Acceso API        | `GET /api/public/pets/{id}`              | `GET /api/collars/tag/{serial}`                   |

### `extractSerial()` en el frontend

Cuando el dueño escanea con la cámara en `ActivateCollarTagPage`, la función `extractSerial` distingue ambos tipos:

```typescript
function extractSerial(raw: string): string {
  // Si el QR es una URL que contiene el serial (ej. pawtrack.cr/activate?serial=PT-A3F9-0001234)
  // o directamente el serial raw:
  const match = raw.match(/PT-[0-9A-Fa-f]{4}-\d{7}/i);
  return match ? match[0].toUpperCase() : raw.trim().toUpperCase();
}
```

Si el dueño escanea por error el QR de identidad (que contiene `/p/{petId}`), la regex no encuentra el patrón `PT-XXXX-NNNNNNN` y se le presenta el texto original — que el backend rechaza con 404 — sin efectos secundarios.

### Flujo de activación con QR

```
Dueño abre ActivateCollarTagPage
        ↓
ScanInput.tsx activa BarcodeDetector (cámara)
        ↓
Escanea el QR del CollarTag
        ↓
extractSerial(raw) → "PT-A3F9-0001234"
        ↓
GET /api/collars/tag/PT-A3F9-0001234
→ { available: true, status: "Unactivated" }
        ↓
[Dueño elige mascota]
        ↓
POST /api/collars/tag/PT-A3F9-0001234/activate  { petId }
        ↓
Backend crea Collar (Provider.Own) + CollarTag.Activate() + CollarDeviceCredential
        ↓
Retorna raw key UNA SOLA VEZ → dueño la copia/guarda
        ↓
A partir de aquí el collar reporta GPS vía POST /api/collars/ingest (X-Collar-Key)
```

---

## 9. Widget B2B (clínicas y embebibles)

El widget embebible en el sitio web de la clínica tiene un campo de búsqueda:

```html
<input placeholder="N° microchip o URL del QR…" />
```

El widget acepta:

- URL completa `https://pawtrack.cr/p/{petId}`
- Solo el ID `{petId}`
- Número de microchip RFID

Llama internamente a `POST /api/clinics/{id}/scan` → mismo `PerformClinicScanCommand`.

---

## 10. Limitaciones y decisiones de diseño

| Decisión                                                    | Razón                                                                                                                                                             |
| ----------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| El QR nunca cambia aunque la mascota se transfiera de dueño | La URL del perfil es del `petId`, no del `ownerId`. El nuevo dueño actualiza su propio perfil detrás de la misma URL.                                             |
| `ECCLevel.M` en lugar de `H`                                | Nivel `H` (30% corrección) produce QRs más densos y difíciles de leer a pequeña escala en collares de mascota. `M` (15%) es suficiente para impresión de calidad. |
| IP hasheada con SHA-256                                     | El GDPR y la regulación costarricense (Ley 8968) consideran IPs como datos personales. El hash hace el dato técnicamente inútil para re-identificación.           |
| Retención de 90 días configurable                           | Balance entre utilidad analítica para el dueño y minimización de datos personales del escaneador.                                                                 |
| QR de activación de CollarTag ≠ QR de perfil                | Separar los dos usos evita que un escaneo accidental del QR de identidad desencadene flujos de activación de hardware.                                            |

---

## 11. Referencias de código

| Archivo                                                                        | Descripción                                                   |
| ------------------------------------------------------------------------------ | ------------------------------------------------------------- |
| `backend/src/PawTrack.Domain/Pets/QrScanEvent.cs`                              | Entidad de auditoría de escaneos públicos                     |
| `backend/src/PawTrack.Application/Common/Interfaces/IQrCodeService.cs`         | Contrato de generación de QR                                  |
| `backend/src/PawTrack.Infrastructure/Pets/QrCodeService.cs`                    | Implementación con QRCoder (ECCLevel.M, 10px/módulo)          |
| `backend/src/PawTrack.Application/Pets/Commands/RecordPublicQrScan/`           | Comando de telemetría + lógica de notificaciones ResolveCheck |
| `backend/src/PawTrack.API/Controllers/PublicController.cs`                     | `GET /api/public/pets/{id}` — dispara el escaneo              |
| `backend/src/PawTrack.API/Controllers/PetsController.cs`                       | `GET /api/pets/{id}/qr` — descarga del PNG                    |
| `backend/src/PawTrack.API/Controllers/PetLookupController.cs`                  | `GET /api/v1/pets/lookup` — escaneo M2M ClinicPartner         |
| `backend/src/PawTrack.Infrastructure/Notifications/Jobs/QrScanRetentionJob.cs` | Purga diaria a 02:00 CR                                       |
| `backend/src/PawTrack.Application/Common/Settings/QrScanRetentionSettings.cs`  | Config de retención (default 90 días)                         |
| `backend/src/PawTrack.Application/Common/Settings/ResolveCheckSettings.cs`     | Umbrales para notificaciones por escaneo                      |
| `frontend/src/features/pets/components/QRFlipCard.tsx`                         | Tarjeta 3D flip con QR descargable                            |
| `frontend/src/features/pets/components/QRCodeDisplay.tsx`                      | Componente simple de display del QR                           |
| `frontend/src/features/clinics/components/ScanInput.tsx`                       | Escáner con BarcodeDetector + fallback manual                 |
| `frontend/src/features/pets/pages/ActivateCollarTagPage.tsx`                   | Flujo de activación con `extractSerial()`                     |
| `frontend/src/features/lost-pets/hooks/useGenerateFlyer.ts`                    | Pre-carga QR como data URL para el volante                    |
