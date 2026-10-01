# azureAi (Node.js / stdio)

Servidor MCP local para Azure AI Foundry: generación de imágenes (`gpt-image-2`), video (`sora-2`) y transcripción con diarización (Azure Speech Fast Transcription). Sigue el patrón `mcp-vscode`.

Ejecutar localmente:

```powershell
node .mcp/azure-ai/server.mjs
node .mcp/azure-ai/tests/smoke.mjs
```

Requiere `.local-secrets/azure-ai.json` (o `credentials/azure-ai.json`) con `apiKey`, `resourceName`/`azureOpenAIEndpoint`/`speechEndpoint` y overrides opcionales de `models`. `transcribe_audio` requiere `ffmpeg` en el PATH.

## Arquitectura

- `src/secrets.mjs`: localiza el archivo de secretos subiendo por el árbol de directorios.
- `src/options.mjs`: endpoints, validación y formateo de errores HTTP (equivalente a `Mcp.Shared.AzureAiOptions`/`AzureClientFactory`).

No hay host, solución ni proyecto .NET en este MCP.
