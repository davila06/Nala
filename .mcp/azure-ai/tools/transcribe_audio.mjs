import { existsSync, mkdirSync, readFileSync, writeFileSync, statSync } from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { ToolError } from '../../_shared/common.mjs';
import { getOptions, getSpeechEndpoint, validateSpeech, formatHttpError, defaultMultimediaDir } from '../src/options.mjs';

function runFfmpeg(inputPath, outputPath) {
  return new Promise((resolve, reject) => {
    let child;
    try {
      child = spawn('ffmpeg', ['-y', '-i', inputPath, '-ar', '16000', '-ac', '1', '-c:a', 'pcm_s16le', outputPath], { stdio: ['ignore', 'ignore', 'pipe'] });
    } catch (error) {
      reject(new ToolError(`Error ejecutando ffmpeg. Asegúrate de tener ffmpeg en el PATH: ${error.message}`));
      return;
    }
    let stderr = '';
    child.stderr.on('data', (d) => { stderr += d.toString(); });
    child.on('error', (error) => reject(new ToolError(`Error ejecutando ffmpeg. Asegúrate de tener ffmpeg en el PATH: ${error.message}`)));
    child.on('close', (code) => {
      if (code !== 0) reject(new ToolError(`ffmpeg falló al convertir audio (código ${code}): ${stderr}`));
      else resolve();
    });
  });
}

async function ensureWav16kMono(audioPath) {
  const workDir = path.join(defaultMultimediaDir());
  if (!existsSync(workDir)) mkdirSync(workDir, { recursive: true });
  const targetWav = path.join(workDir, `${path.basename(audioPath, path.extname(audioPath))}_16k.wav`);
  await runFfmpeg(audioPath, targetWav);
  return targetWav;
}

function formatTimestamp(offsetMs) {
  const totalSeconds = Math.floor(offsetMs / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  const pad = (n) => String(n).padStart(2, '0');
  return hours > 0 ? `${pad(hours)}:${pad(minutes)}:${pad(seconds)}` : `00:${pad(minutes)}:${pad(seconds)}`;
}

export default {
  name: 'transcribe_audio',
  description: 'Transcribes audio files (m4a, mp3, wav, flac, etc.) using Azure Speech Fast Transcription API with speaker diarization. Converts to WAV 16kHz mono via ffmpeg if needed, generates Markdown formatted transcript with timestamps and speaker labels.',
  inputSchema: {
    type: 'object',
    properties: {
      audioPath: { type: 'string', description: 'Absolute or relative path to the audio file (m4a, mp3, wav, etc.).' },
      language: { type: 'string', description: "BCP-47 language locale (default: 'es-CR')." },
      maxSpeakers: { type: 'number', description: 'Max number of speakers for diarization (default 35).' },
      outputPath: { type: 'string', description: 'Optional target Markdown output file path.' },
    },
    required: ['audioPath'],
    additionalProperties: false,
  },

  async handler(args) {
    if (!args.audioPath?.trim()) throw new ToolError('audioPath es requerido.');
    const fullAudioPath = path.resolve(args.audioPath);
    if (!existsSync(fullAudioPath)) throw new ToolError(`Archivo de audio no encontrado: ${fullAudioPath}`);

    const options = getOptions();
    validateSpeech(options);

    const wavPath = await ensureWav16kMono(fullAudioPath);
    const definition = {
      locales: [(args.language || 'es-CR').trim()],
      diarization: { enabled: true, maxSpeakers: Math.min(Math.max(args.maxSpeakers ?? 35, 1), 35) },
      properties: { wordLevelTimestampsEnabled: false, punctuationMode: 'DictatedAndAutomatic', profanityFilterMode: 'None' },
    };

    const form = new FormData();
    form.append('definition', new Blob([JSON.stringify(definition)], { type: 'application/json' }));
    form.append('audio', new Blob([readFileSync(wavPath)], { type: 'audio/wav' }), path.basename(wavPath));

    let response;
    try {
      response = await fetch(`${getSpeechEndpoint(options)}/speechtotext/transcriptions:transcribe?api-version=2024-11-15`, {
        method: 'POST',
        headers: { 'Ocp-Apim-Subscription-Key': options.apiKey },
        body: form,
      });
    } catch (error) {
      throw new ToolError(`Fallo al enviar audio a Azure Speech Fast Transcription: ${error.message}`);
    }
    if (!response.ok) throw new ToolError(await formatHttpError(response));

    const json = await response.json();
    const phrases = json.phrases || [];
    if (!phrases.length) {
      return { success: false, transcriptPath: null, speakerCount: 0, phrasesCount: 0, rawText: null, message: 'La API respondió sin frases detectadas en el audio.' };
    }

    const utterances = phrases
      .map((p) => ({ speaker: p.speaker ?? 0, offsetMs: p.offsetMilliseconds ?? 0, text: (p.text || '').trim() }))
      .filter((u) => u.text)
      .sort((a, b) => a.offsetMs - b.offsetMs);

    const uniqueSpeakers = [...new Set(utterances.map((u) => u.speaker))].sort((a, b) => a - b);
    const speakerLabels = new Map();
    uniqueSpeakers.forEach((speaker, index) => speakerLabels.set(speaker, `Speaker ${String.fromCharCode(65 + index)}`));

    let targetMd = args.outputPath;
    if (!targetMd?.trim()) {
      const baseDir = path.dirname(fullAudioPath);
      const baseName = path.basename(fullAudioPath, path.extname(fullAudioPath));
      targetMd = path.join(baseDir, `${baseName}_transcript.md`);
    }

    const lines = [
      `# Transcripción — ${new Date().toLocaleString()}`,
      '',
      `**Fuente:** ${path.basename(fullAudioPath)}`,
      `**Hablantes detectados:** ${uniqueSpeakers.length}`,
      '',
      '---',
      '',
    ];
    for (const u of utterances) {
      lines.push(`**${speakerLabels.get(u.speaker) || `Speaker ${u.speaker}`}** [${formatTimestamp(u.offsetMs)}]`, u.text, '');
    }
    const markdown = lines.join('\n');

    const outDir = path.dirname(path.resolve(targetMd));
    if (!existsSync(outDir)) mkdirSync(outDir, { recursive: true });
    writeFileSync(targetMd, markdown, 'utf8');

    return {
      success: true, transcriptPath: path.resolve(targetMd), speakerCount: uniqueSpeakers.length, phrasesCount: utterances.length,
      rawText: markdown.length > 1500 ? markdown.slice(0, 1500) + '... [truncado]' : markdown,
      message: `Transcripción generada exitosamente en ${targetMd}`,
    };
  },

  async smoke({ callTool, ...ctx }) {
    const payload = ctx.toolJson(await callTool('transcribe_audio', { audioPath: '/tmp/no-existe.wav' }));
    ctx.check('transcribe_audio valida archivo inexistente', typeof payload.error === 'string');
  },
};
