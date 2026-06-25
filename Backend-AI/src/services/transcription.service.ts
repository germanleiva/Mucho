import OpenAI, { toFile } from "openai";
import type { Transcript } from "../schemas/request.schema.js";

type TranscribeAudioInput = {
  bytes: Buffer;
  filename: string;
  mimeType?: string;
  language?: string;
};

let client: OpenAI | undefined;

export async function transcribeAudio(
  input: TranscribeAudioInput
): Promise<Transcript> {
  const model =
    process.env.OPENAI_TRANSCRIPTION_MODEL || "gpt-4o-mini-transcribe";
  const file = await toFile(input.bytes, input.filename, {
    type: input.mimeType,
  });
  const openai = getOpenAIClient();

  if (model === "whisper-1") {
    const result = await openai.audio.transcriptions.create({
      file,
      model,
      response_format: "verbose_json",
      timestamp_granularities: ["segment"],
      language: input.language,
    });

    return {
      text: result.text,
      language: normalizeLanguage(result.language),
      segments: (result.segments ?? []).map((segment) => ({
        text: segment.text.trim(),
        timeStartMs: Math.round(segment.start * 1000),
        timeEndMs: Math.round(segment.end * 1000),
      })),
    };
  }

  const result = await openai.audio.transcriptions.create({
    file,
    model,
    response_format: "json",
    language: input.language,
  });

  return {
    text: result.text,
    language: normalizeLanguage(input.language),
    segments: [],
  };
}

function getOpenAIClient(): OpenAI {
  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey) {
    throw new Error("OPENAI_API_KEY is not configured.");
  }

  client ??= new OpenAI({ apiKey });
  return client;
}

function normalizeLanguage(language?: string): Transcript["language"] {
  const normalized = language?.toLowerCase();
  return normalized === "en" || normalized === "it" || normalized === "da"
    ? normalized
    : "unknown";
}
