// Port de Mcp.Shared.AzureAiOptions / AzureClientFactory: config + endpoints + error HTTP formateado.
import path from 'node:path';
import { ToolError } from '../../_shared/common.mjs';
import { findSecretFilePath, loadSecret } from './secrets.mjs';

const DEFAULT_MODELS = {
  llm: 'gpt-5.4',
  imageGeneration: 'gpt-image-2',
  videoGeneration: 'sora-2',
  realtime: 'gpt-realtime',
  realtimeTranslation: 'gpt-realtime-translate',
  llmEmbedding: 'text-embedding-3-large',
  llmVision: 'gpt-5.6-luna',
};

let cached;

export function getOptions() {
  if (cached) return cached;
  const raw = loadSecret('azure-ai.json');
  cached = {
    apiKey: raw.apiKey,
    resourceName: raw.resourceName,
    azureOpenAIEndpoint: raw.azureOpenAIEndpoint,
    speechEndpoint: raw.speechEndpoint,
    projectEndpoint: raw.projectEndpoint,
    models: { ...DEFAULT_MODELS, ...(raw.models || {}) },
  };
  return cached;
}

export function getOpenAIEndpoint(options) {
  if (options.azureOpenAIEndpoint?.trim()) return options.azureOpenAIEndpoint.replace(/\/+$/, '');
  if (options.resourceName?.trim()) return `https://${options.resourceName.trim()}.openai.azure.com/openai/v1`;
  throw new ToolError("Falta 'azureOpenAIEndpoint' o 'resourceName' en la configuración de Azure AI.");
}

export function getSpeechEndpoint(options) {
  if (options.speechEndpoint?.trim()) return options.speechEndpoint.replace(/\/+$/, '');
  if (options.resourceName?.trim()) return `https://${options.resourceName.trim()}.cognitiveservices.azure.com`;
  throw new ToolError("Falta 'speechEndpoint' o 'resourceName' en la configuración de Azure AI.");
}

export function validateOpenAi(options) {
  if (!options.apiKey?.trim()) throw new ToolError("Falta 'apiKey' en la configuración de Azure AI.");
  getOpenAIEndpoint(options);
}

export function validateSpeech(options) {
  if (!options.apiKey?.trim()) throw new ToolError("Falta 'apiKey' en la configuración de Azure AI.");
  getSpeechEndpoint(options);
}

export async function formatHttpError(response) {
  const status = response.status;
  const reason = response.statusText || 'Error';
  let content;
  try {
    content = await response.text();
    if (content.length > 2000) content = content.slice(0, 2000) + '... [truncado]';
  } catch {
    content = '(sin cuerpo de respuesta)';
  }
  return `Error HTTP ${status} (${reason}): ${content}`;
}

// Directorio de salida por defecto para multimedia generado (junto al secreto, no en el repo si vive fuera).
export function defaultMultimediaDir() {
  const secretPath = findSecretFilePath('azure-ai.json');
  const base = secretPath ? path.dirname(path.dirname(secretPath)) : process.cwd();
  return path.join(base, '.mcp', 'azure-ai', 'multimedia');
}
