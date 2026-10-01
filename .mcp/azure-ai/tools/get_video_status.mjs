import { existsSync, mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { ToolError } from '../../_shared/common.mjs';
import { getOptions, getOpenAIEndpoint, validateOpenAi, formatHttpError, defaultMultimediaDir } from '../src/options.mjs';

export default {
  name: 'get_video_status',
  description: "Checks status of a Sora-2 video job. When status is 'completed' and download=true, downloads video to target output path.",
  inputSchema: {
    type: 'object',
    properties: {
      jobId: { type: 'string', description: 'The video job ID returned by generate_video.' },
      download: { type: 'boolean', description: 'Whether to download the video content if completed. Default true.' },
      outputPath: { type: 'string', description: 'Target file path for video download (.mp4).' },
    },
    required: ['jobId'],
    additionalProperties: false,
  },

  async handler(args) {
    if (!args.jobId?.trim()) throw new ToolError('jobId es requerido.');
    const options = getOptions();
    validateOpenAi(options);
    const endpoint = getOpenAIEndpoint(options);
    const safeId = encodeURIComponent(args.jobId.trim());

    let statusResponse;
    try {
      statusResponse = await fetch(`${endpoint}/videos/${safeId}`, { headers: { 'api-key': options.apiKey } });
    } catch (error) {
      throw new ToolError(`Fallo al consultar estado del video: ${error.message}`);
    }
    if (!statusResponse.ok) throw new ToolError(await formatHttpError(statusResponse));

    const json = await statusResponse.json();
    const status = json.status ?? 'unknown';
    const prompt = json.prompt ?? null;
    const failureReason = json.failure_reason ?? null;
    const download = args.download ?? true;

    let downloadedPath = null;
    let bytesWritten = 0;

    if (status.toLowerCase() === 'completed' && download) {
      let resolved = args.outputPath;
      if (!resolved?.trim()) {
        const stamp = new Date().toISOString().replace(/[-:]/g, '').replace('T', '_').slice(0, 15);
        const fileName = `video_${stamp}_${safeId.slice(0, 8)}.mp4`;
        resolved = path.join(defaultMultimediaDir(), fileName);
      }
      const dir = path.dirname(path.resolve(resolved));
      if (!existsSync(dir)) mkdirSync(dir, { recursive: true });

      let downloadResponse;
      try {
        downloadResponse = await fetch(`${endpoint}/videos/${safeId}/content`, { headers: { 'api-key': options.apiKey } });
      } catch (error) {
        throw new ToolError(`Fallo al descargar video completado: ${error.message}`);
      }
      if (!downloadResponse.ok) throw new ToolError(`El video está completado pero la descarga falló: ${await formatHttpError(downloadResponse)}`);

      const bytes = Buffer.from(await downloadResponse.arrayBuffer());
      writeFileSync(resolved, bytes);
      downloadedPath = path.resolve(resolved);
      bytesWritten = bytes.length;
    }

    const messages = {
      completed: downloadedPath ? `Video completado y descargado en ${downloadedPath} (${bytesWritten} bytes)` : 'Video completado y listo para descarga.',
      running: 'Video en generación (running)...',
      queued: 'Video en cola (queued)...',
      failed: `Video fallido: ${failureReason ?? 'sin detalle'}`,
    };

    return {
      jobId: args.jobId, status, prompt, failureReason, downloadedPath, bytesWritten,
      message: messages[status.toLowerCase()] ?? `Estado actual: ${status}`,
    };
  },

  async smoke({ callTool, ...ctx }) {
    const payload = ctx.toolJson(await callTool('get_video_status', { jobId: '' }));
    ctx.check('get_video_status valida jobId', typeof payload.error === 'string');
  },
};
