import { ToolError } from '../../_shared/common.mjs';
import { getOptions, getOpenAIEndpoint, validateOpenAi, formatHttpError } from '../src/options.mjs';

export default {
  name: 'generate_video',
  description: "Creates an asynchronous video generation job with Sora-2. Duration 'seconds' must be '4', '8', or '12' as string.",
  inputSchema: {
    type: 'object',
    properties: {
      prompt: { type: 'string', description: 'Prompt description for video generation.' },
      seconds: { type: 'string', enum: ['4', '8', '12'], description: "Duration in seconds as string: '4', '8', or '12'." },
      size: { type: 'string', description: "Resolution: '1280x720', '720x1280', or '1024x1024'." },
    },
    required: ['prompt'],
    additionalProperties: false,
  },

  async handler(args) {
    if (!args.prompt?.trim()) throw new ToolError('El prompt de generación de video no puede estar vacío.');
    const validSeconds = (args.seconds ?? '8').trim();
    if (!['4', '8', '12'].includes(validSeconds)) throw new ToolError("seconds debe ser '4', '8' o '12' (como string).");

    const options = getOptions();
    validateOpenAi(options);
    const model = options.models.videoGeneration || 'sora-2';
    const payload = { model, prompt: args.prompt.trim(), seconds: validSeconds, size: (args.size || '1280x720').trim(), n: 1 };

    let response;
    try {
      response = await fetch(`${getOpenAIEndpoint(options)}/videos`, {
        method: 'POST',
        headers: { 'api-key': options.apiKey, 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
    } catch (error) {
      throw new ToolError(`Fallo al llamar a Azure OpenAI (videos): ${error.message}`);
    }
    if (!response.ok) throw new ToolError(await formatHttpError(response));

    const json = await response.json();
    return {
      jobId: json.id ?? 'unknown', status: json.status ?? 'queued', model,
      message: `Trabajo de video creado. Usa get_video_status con jobId '${json.id ?? 'unknown'}' para consultar o descargar.`,
    };
  },

  async smoke({ callTool, ...ctx }) {
    const payload = ctx.toolJson(await callTool('generate_video', { prompt: 'x', seconds: '7' }));
    ctx.check('generate_video valida seconds', typeof payload.error === 'string');
  },
};
