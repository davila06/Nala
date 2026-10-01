import { writeFileSync, mkdirSync, existsSync } from 'node:fs';
import path from 'node:path';
import { randomUUID } from 'node:crypto';
import { ToolError } from '../../_shared/common.mjs';
import { getOptions, getOpenAIEndpoint, validateOpenAi, formatHttpError, defaultMultimediaDir } from '../src/options.mjs';

export default {
  name: 'generate_image',
  description: 'Generates an image via Azure AI Foundry using deployment gpt-image-2 and saves it to a local output path (or default assets path).',
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Prompt description for the image generation.' },
      size: { type: 'string', description: 'Image dimensions: 1024x1024 (default), 1792x1024, or 1024x1792.' },
      quality: { type: 'string', description: 'Quality: medium (recommended default in workspace), low, high, auto.' },
      outputFormat: { type: 'string', enum: ['png', 'jpeg'], description: 'Format: png (default) or jpeg.' },
      outputPath: { type: 'string', description: 'Optional target output file path. Defaults to assets/img/{guid}.{ext}' },
    },
    required: ['prompt'],
    additionalProperties: false,
  },

  async handler(args) {
    if (!args.prompt?.trim()) throw new ToolError('El prompt de generación no puede estar vacío.');
    const options = getOptions();
    validateOpenAi(options);
    const model = options.models.imageGeneration || 'gpt-image-2';
    const outputFormat = (args.outputFormat || 'png').trim();
    const payload = {
      model,
      prompt: args.prompt.trim(),
      n: 1,
      size: (args.size || '1024x1024').trim(),
      quality: (args.quality || 'medium').trim(),
      output_format: outputFormat,
    };

    let response;
    try {
      response = await fetch(`${getOpenAIEndpoint(options)}/images/generations`, {
        method: 'POST',
        headers: { 'api-key': options.apiKey, 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(payload),
      });
    } catch (error) {
      throw new ToolError(`Fallo de conexión con Azure OpenAI (images/generations): ${error.message}`);
    }
    if (!response.ok) throw new ToolError(await formatHttpError(response));

    const json = await response.json();
    const first = json.data?.[0];
    if (!first) throw new ToolError("La respuesta de generación no contiene ningún item en 'data'.");

    if (first.b64_json) {
      const ext = outputFormat === 'jpeg' ? 'jpg' : 'png';
      let resolvedPath = args.outputPath;
      if (!resolvedPath?.trim()) {
        const stamp = new Date().toISOString().replace(/[-:]/g, '').replace('T', '_').slice(0, 15);
        const fileName = `img_${stamp}_${randomUUID().replace(/-/g, '').slice(0, 6)}.${ext}`;
        resolvedPath = path.join(defaultMultimediaDir(), fileName);
      }
      const dir = path.dirname(path.resolve(resolvedPath));
      if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
      const bytes = Buffer.from(first.b64_json, 'base64');
      writeFileSync(resolvedPath, bytes);
      return {
        success: true, model, savedPath: path.resolve(resolvedPath), bytesWritten: bytes.length,
        revisedPrompt: first.revised_prompt ?? null, url: null, message: `Imagen generada y guardada en ${resolvedPath}`,
      };
    }

    if (first.url) {
      return { success: true, model, savedPath: null, bytesWritten: 0, revisedPrompt: first.revised_prompt ?? null, url: first.url, message: 'Imagen generada disponible en URL' };
    }

    throw new ToolError("La respuesta no contiene 'b64_json' ni 'url'.");
  },

  async smoke({ callTool, ...ctx }) {
    const payload = ctx.toolJson(await callTool('generate_image', { prompt: '' }));
    ctx.check('generate_image valida prompt vacío', typeof payload.error === 'string');
  },
};
