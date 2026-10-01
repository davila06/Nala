#!/usr/bin/env node
// MCP local para Azure AI Foundry (imágenes, video, transcripción). Cada tool vive en tools/{name}.mjs.
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { McpServer } from '../_shared/protocol.mjs';
import { loadTools } from '../_shared/registry.mjs';
import { ToolError, textResult, errorResult, createLogger } from '../_shared/common.mjs';

const SERVER_NAME = 'azureAi';
const SERVER_VERSION = '2.0.0';
const SUPPORTED_PROTOCOL_VERSIONS = ['2025-06-18', '2025-03-26', '2024-11-05'];
const DEFAULT_PROTOCOL_VERSION = SUPPORTED_PROTOCOL_VERSIONS[0];

const here = path.dirname(fileURLToPath(import.meta.url));
const log = createLogger(SERVER_NAME);
const tools = await loadTools(path.join(here, 'tools'));
const byName = new Map(tools.map((t) => [t.name, t]));

async function callTool(name, args) {
  const tool = byName.get(name);
  if (!tool) return errorResult(`tool desconocida: ${name}`);
  try {
    return textResult(await tool.handler(args ?? {}));
  } catch (error) {
    if (error instanceof ToolError) return errorResult(error.message);
    log(`error inesperado en ${name}: ${error.stack || error.message}`);
    return errorResult(`error interno: ${error.message}`);
  }
}

new McpServer({
  tools: tools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema })),
  callTool,
  serverInfo: { name: SERVER_NAME, version: SERVER_VERSION },
  protocolVersions: { supported: SUPPORTED_PROTOCOL_VERSIONS, default: DEFAULT_PROTOCOL_VERSION },
  log,
}).start();
