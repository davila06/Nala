# gpt-image-2 — Generación de Imágenes

> **Fuentes oficiales**:
> - https://learn.microsoft.com/azure/ai-services/openai/how-to/images
> - https://learn.microsoft.com/azure/ai-services/openai/reference#image-generation
> - https://platform.openai.com/docs/api-reference/images/create

---

## Información del modelo

| Campo | Valor |
|-------|-------|
| Nombre de despliegue | `gpt-image-2` |
| Familia | GPT Image |
| Capacidades | Generación de imágenes desde texto, edición de imágenes, variaciones |
| Formatos de salida | `b64_json` (por defecto), `url` (opcional, con expiración) |

---

## Endpoint

```
POST https://demo-itqs-resource.openai.azure.com/openai/v1/images/generations
```

---

## Request body

```json
{
  "model": "gpt-image-2",
  "prompt": "Un diagrama de arquitectura de Azure con servicios conectados...",
  "n": 1,
  "size": "1024x1024",
  "quality": "medium",
  "output_format": "png"
}
```

### Parámetros

| Parámetro | Tipo | Obligatorio | Valores | Default |
|-----------|------|-------------|---------|---------|
| `model` | string | ✅ | `"gpt-image-2"` | — |
| `prompt` | string | ✅ | Descripción de la imagen | — |
| `n` | integer | ❌ | `1`–`10` | `1` |
| `size` | string | ❌ | `"1024x1024"`, `"1792x1024"`, `"1024x1792"` | `"1024x1024"` |
| `quality` | string | ❌ | `"low"`, `"medium"`, `"high"`, `"auto"` | `"auto"` |
| `output_format` | string | ❌ | `"png"`, `"jpeg"` | `"png"` |

> **⚠️ CRÍTICO**: El campo `quality` acepta `low`/`medium`/`high`/`auto` — **no** `standard`/`hd` (esos son de DALL-E 3).  
> Fuente verificada en producción: valor `"medium"` funciona correctamente.
>
> **⚠️ CRÍTICO**: El parámetro de formato de salida es `output_format` (acepta `'png'` o `'jpeg'`), **no** `response_format`.  
> Usar `response_format` causa HTTP 400. La respuesta siempre retorna `data[].b64_json` independientemente del valor de `output_format`.  
> Verificado en producción ITQS: mayo 2026 (documentado) y julio 2026 (reconfirmado con slides CAP-04).

---

## Response body

```json
{
  "created": 1748000000,
  "data": [
    {
      "b64_json": "iVBORw0KGgoAAAANSUhEUgAA...",
      "revised_prompt": "Un diagrama de arquitectura..."
    }
  ]
}
```

> **Nota**: La respuesta siempre incluye `b64_json` (sin importar el valor de `output_format`).  
> El campo `revised_prompt` contiene el prompt modificado por el modelo para mayor calidad.

---

## Decodificar y guardar la imagen en PowerShell

```powershell
$creds   = Get-Content (Join-Path $PSScriptRoot '..\..\..\..\credentials\ai-foundry.json') -Raw | ConvertFrom-Json
$url     = "$($creds.azureOpenAIEndpoint)/images/generations"
$headers = @{ 'api-key' = $creds.apiKey; 'Content-Type' = 'application/json' }

$body = @{
    model         = $creds.models.imageGeneration   # 'gpt-image-2'
    prompt        = 'Un diagrama de arquitectura Azure con servicios cloud'
    n             = 1
    size          = '1024x1024'
    quality       = 'medium'
    output_format = 'png'            # CORRECTO: 'png' o 'jpeg' — NO usar response_format
} | ConvertTo-Json -Depth 5

$response  = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body
$imageData = $response.data[0]

# Verificar si es b64_json (puede retornar url en otros casos)
if ($imageData.PSObject.Properties['b64_json']) {
    $bytes    = [Convert]::FromBase64String($imageData.b64_json)
    $outPath  = Join-Path $PSScriptRoot 'assets\demo-image-generated.png'
    [IO.File]::WriteAllBytes($outPath, $bytes)
    Write-Host "Imagen guardada: $outPath"
} elseif ($imageData.PSObject.Properties['url']) {
    Invoke-WebRequest -Uri $imageData.url -OutFile (Join-Path $PSScriptRoot 'assets\demo-image-generated.png')
}
```

> **Patrón de verificación**: usar `$obj.PSObject.Properties['campo']` en lugar de `$obj.campo -ne $null` para evitar falsos negativos en PowerShell cuando el campo no existe vs. cuando es null.

---

## Prompt Tips para mejores resultados

- Incluir estilo: `"estilo minimalista"`, `"flat design"`, `"isometric illustration"`
- Especificar color: `"paleta azul y blanco corporativo"`
- Especificar propósito: `"para presentación ejecutiva"`, `"para documentación técnica"`
- Evitar texto en la imagen (gpt-image-2 puede generarlo pero con errores tipográficos)

---

## Errores comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `400 - invalid quality value` | Usar `"standard"` o `"hd"` | Cambiar a `"low"`, `"medium"`, `"high"` o `"auto"` |
| `400 - Bad Request` (genérico) | Usar `response_format` en lugar de `output_format` | **Reemplazar** `response_format = 'b64_json'` por `output_format = 'png'` |
| `429 - Too Many Requests` | Throttling por requests seguidos | Esperar ≥15s entre requests; procesar en lotes pequeños |
| `400 - content_policy_violation` | Prompt con contenido sensible | Reformular sin referencias a personas reales, violencia, etc. |
| `$null` en `b64_json` | Campo no existe en respuesta | Verificar con `PSObject.Properties['b64_json']` antes de acceder |
| Imagen en blanco | Prompt demasiado vago | Agregar más detalle descriptivo al prompt |

---

## Edición de Imágenes (Image Edit API)

> **Fuente oficial**: https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/dall-e#call-the-image-edit-api

`gpt-image-2` soporta edición con **inpainting y variaciones** — con capacidad mejorada sobre versiones anteriores.

### Endpoint de edición

```
POST https://{resource}.openai.azure.com/openai/v1/images/edits?api-version=preview
```

> **⚠️ CRÍTICO**: El endpoint de edición usa el mismo patrón `/openai/v1/` que generación,
> pero con `?api-version=preview` — **verificado en producción** para el recurso `demo-itqs-resource`.

Para este proyecto:
```
POST https://demo-itqs-resource.openai.azure.com/openai/v1/images/edits?api-version=preview
```

Constructor: `"$($creds.azureOpenAIEndpoint)/images/edits?api-version=preview"` (sin modificar el endpoint base).

### Headers

```
Content-Type: multipart/form-data
api-key: <apiKey>
```

### Form data (multipart/form-data)

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|-------------|-------------|
| `image[]` | file (PNG/JPG) | ✅ | Imagen a editar, < 50 MB |
| `prompt` | string | ✅ | Descripción del resultado deseado |
| `model` | string | ✅ | `"gpt-image-2"` |
| `mask` | file (PNG) | ❌ | PNG con áreas transparentes (alpha=0) = zonas a editar (inpainting) |
| `size` | string | ❌ | Resoluciones arbitrarias (múltiplos de 16px, max 3840px) |
| `quality` | string | ❌ | `"low"`, `"medium"`, `"high"` — default `"high"` |
| `n` | integer | ❌ | `1`–`10` imágenes — default `1` |
| `input_fidelity` | string | ❌ | `"high"` / `"low"` — qué tanto preservar rasgos/estilo del original |
| `stream` | boolean | ❌ | `true` para streaming de imágenes parciales |
| `partial_images` | integer | ❌ | `0`–`3` imágenes parciales durante streaming |

> **`input_fidelity`**: `"high"` preserva caras y estilo del original más fielmente; `"low"` permite mayor libertad creativa.

### Response body

```json
{
  "created": 1748000000,
  "data": [
    {
      "b64_json": "iVBORw0KGgoAAAANSUhEUgAA..."
    }
  ]
}
```

> La edición siempre devuelve `b64_json` — no hay opción de URL en el endpoint de edición.

### Ejemplo PowerShell 7 (multipart/form-data)

```powershell
$creds    = Get-Content (Join-Path $PSScriptRoot '..\..\..\..\credentials\ai-foundry.json') -Raw | ConvertFrom-Json
$model   = $creds.models.imageGeneration   # 'gpt-image-2'
$editUrl = "$($creds.azureOpenAIEndpoint)/images/edits?api-version=preview"
$headers  = @{ 'api-key' = $creds.apiKey }

$imagePath = 'C:\ruta\imagen-original.png'
$imageStream = [System.IO.File]::OpenRead($imagePath)

# Usar HttpClient para controlar MIME type (FileInfo con -Form envía octet-stream
# pero la API requiere image/png o image/jpeg)
$httpClient = [System.Net.Http.HttpClient]::new()
$httpClient.DefaultRequestHeaders.Add('api-key', $apiKey)

$multipart  = [System.Net.Http.MultipartFormDataContent]::new()
$imgBytes   = [System.IO.File]::ReadAllBytes($imagePath)
$imgContent = [System.Net.Http.ByteArrayContent]::new($imgBytes)
$imgContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new('image/png')
$multipart.Add($imgContent, 'image[]', 'image.png')
$multipart.Add([System.Net.Http.StringContent]::new('Add a glowing Azure logo in the sky'), 'prompt')
$multipart.Add([System.Net.Http.StringContent]::new($model),  'model')
$multipart.Add([System.Net.Http.StringContent]::new('high'),  'quality')
$multipart.Add([System.Net.Http.StringContent]::new('high'),  'input_fidelity')
$multipart.Add([System.Net.Http.StringContent]::new('1'),     'n')

$resp      = $httpClient.PostAsync($editUrl, $multipart).GetAwaiter().GetResult()
$jsonStr   = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
$httpClient.Dispose()
$response  = $jsonStr | ConvertFrom-Json

$bytes   = [Convert]::FromBase64String($response.data[0].b64_json)
$outPath = Join-Path $PSScriptRoot 'assets\demo-image-edited.png'
[IO.File]::WriteAllBytes($outPath, $bytes)
Write-Host "Imagen editada guardada: $outPath"
```

> **⚠️ MIME type**: `[System.IO.FileInfo]` en `-Form` de `Invoke-RestMethod` envía `application/octet-stream`.
> Usar `HttpClient` + `ByteArrayContent` con `ContentType = image/png` para que la API lo acepte.

### Errores específicos de edición

| Error | Causa | Solución |
|-------|-------|----------|
| `415 - Unsupported Media Type` | Enviar JSON en vez de multipart | Usar `-Form` (PS) o `FormData` (JS) |
| `400 - image too large` | Imagen > 50 MB | Comprimir o reducir resolución |
| `400 - invalid image format` | Formato distinto a PNG/JPG | Convertir a PNG o JPG |
| `404 - deployment not found` | URL de deployment incorrecta | Verificar nombre del deployment en Azure Foundry |
