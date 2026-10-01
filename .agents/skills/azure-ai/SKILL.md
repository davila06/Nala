---
name: azure-ai
description: >
  Capacidades de Azure AI Foundry y Azure Speech: generacion de imagenes (gpt-image-2),
  generacion de video (sora-2), estado y descarga de video, y transcripcion de audio con
  diarizacion automatica via Azure Speech Fast Transcription. Integrado mediante el MCP local
  azureAi (.mcp/azure-ai-dotnet).
  Triggers: azure ai foundry, gpt-image-2, sora-2, video sora, transcribir audio, transcripcion,
  diarizacion, fast transcription, azure speech, generate_image, generate_video, get_video_status,
  transcribe_audio, audio a texto.
---

# Azure AI — Generación de Imagen, Video y Transcripción

Servicios de Azure AI Foundry y Azure Speech unificados en el servidor MCP local **azureAi** (`.mcp/azure-ai-dotnet`).

## Configuración y Credenciales

Las credenciales unificadas se configuran en `.local-secrets/azure-ai.json` (plantilla en [.local-secrets/azure-ai.example.json](.local-secrets/azure-ai.example.json)):

```json
{
  "apiKey": "<API_KEY_AZURE_AI_SERVICES>",
  "resourceName": "evistaprofoundry",
  "azureOpenAIEndpoint": "https://<recurso>.openai.azure.com/openai/v1",
  "speechEndpoint": "https://<recurso>.cognitiveservices.azure.com",
  "projectEndpoint": "https://<recurso>.services.ai.azure.com/api/projects/<proyecto>",
  "models": {
    "imageGeneration": "gpt-image-2",
    "videoGeneration": "sora-2"
  }
}
```

Los artefactos multimedia generados por defecto se guardan en `.mcp/azure-ai-dotnet/multimedia/` (ignorado por Git).

---

## Tools MCP disponibles (`azureAi`)

El servidor MCP está registrado en `.vscode/mcp.json` bajo el nombre `azureAi`.

### 1. `generate_image`
Genera imágenes usando el despliegue `gpt-image-2`.
- **Parámetros:**
  - `prompt` (string, obligatorio): Descripción de la imagen.
  - `size` (string): `"1024x1024"` (default), `"1792x1024"`, `"1024x1792"`.
  - `quality` (string): `"medium"` (default recomendado), `"low"`, `"high"`, `"auto"`.
  - `outputFormat` (string): `"png"` (default) o `"jpeg"`.
  - `outputPath` (string, opcional): Ruta donde guardar el archivo. Por defecto guarda en `.mcp/azure-ai-dotnet/multimedia/img_{timestamp}_{guid}.png`.
- **Reglas críticas:**
  - `quality` usa `low`/`medium`/`high`/`auto`, **no** `standard`/`hd`.
  - En este workspace preferir `quality: medium` para balance de costo y latencia.

### 2. `generate_video`
Inicia un trabajo asíncrono de generación de video con `sora-2`.
- **Parámetros:**
  - `prompt` (string, obligatorio): Descripción del video.
  - `seconds` (string, default `"8"`): Duración en segundos. **CRÍTICO: debe ser string**, `"4"`, `"8"`, `"12"`.
  - `size` (string, default `"1280x720"`): `"1280x720"`, `"720x1280"`, `"1024x1024"`.
- **Salida:** Retorna `jobId` y `status: "queued"`.

### 3. `get_video_status`
Consulta el estado de un trabajo de video Sora-2 y descarga el archivo una vez finalizado.
- **Parámetros:**
  - `jobId` (string, obligatorio): Identificador del trabajo devuelto por `generate_video`.
  - `download` (boolean, default `true`): Si está completado, descarga automáticamente el contenido binario.
  - `outputPath` (string, opcional): Ruta `.mp4` destino. Por defecto en `.mcp/azure-ai-dotnet/multimedia/video_{timestamp}_{jobId}.mp4`.
- **Ruta de descarga oficial:** `GET /openai/v1/videos/{id}/content`.

### 4. `transcribe_audio`
Transcribe grabaciones de audio (`m4a`, `mp3`, `wav`, `flac`, etc.) con diarización automática mediante Azure Speech Fast Transcription API.
- **Parámetros:**
  - `audioPath` (string, obligatorio): Ruta del archivo de audio.
  - `language` (string, default `"es-CR"`): Código BCP-47 del idioma.
  - `maxSpeakers` (integer, default `35`): Número máximo de hablantes a identificar.
  - `outputPath` (string, opcional): Ruta del `.md` generado. Si se omite, se genera junto al audio como `{nombre}_transcript.md`.
- **Pipeline:** Convierte automáticamente a WAV 16kHz mono mediante `ffmpeg` y estructura el transcript con timestamps y etiquetas de hablante (`Speaker A`, `Speaker B`, etc.).
- **Requisito:** `ffmpeg` disponible en el sistema.

---

## Referencia Técnica

Para especificaciones detalladas de endpoints REST y parámetros avanzados, consultar [reference/](reference/):
- [reference/01-endpoints-auth.md](reference/01-endpoints-auth.md) — URLs y auth v1.
- [reference/02-llm-gpt54.md](reference/02-llm-gpt54.md) — Chat Completions reasoning.
- [reference/03-image-gpt-image-2.md](reference/03-image-gpt-image-2.md) — gpt-image-2.
- [reference/04-video-sora2.md](reference/04-video-sora2.md) — sora-2.
- [reference/05-realtime-websocket.md](reference/05-realtime-websocket.md) — WebSocket de voz.
- [reference/06-translate-websocket.md](reference/06-translate-websocket.md) — WebSocket de traducción.
