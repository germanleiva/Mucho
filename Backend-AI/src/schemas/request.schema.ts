import { z } from "zod";
import { SequenceSchema } from "./sequence.schema.js";
import { SceneSchema } from "./scene.schema.js";
import { ContextObservationSchema } from "./observation.schema.js";

export const TranscriptSchema = z.object({
  text: z.string(),
  language: z.enum(["en", "it", "da", "unknown"]).optional(),
  segments: z
    .array(
      z.object({
        text: z.string(),
        timeStartMs: z.number().nonnegative().optional(),
        timeEndMs: z.number().nonnegative().optional(),
      })
    )
    .default([]),
});

export const RecordingMetadataSchema = z.object({
  frameRate: z.number().positive(),
  durationFrames: z.number().int().positive(),
  audioStartFrame: z.number().int().nonnegative().default(0),
  recordingMode: z.literal("voice_during_miming"),
});

export const AnalysisOptionsSchema = z
  .object({
    confidenceThreshold: z.number().min(0).max(1).default(0.88),
    includeDebugChapters: z.boolean().default(true),
    allowDefaultTextHide: z.boolean().default(true),
  })
  .default({});

export const AnalyzeRecordingRequestSchema = z.object({
  projectId: z.string().optional(),
  recordingId: z.string(),
  languageHint: z.enum(["en", "it", "da", "auto"]).default("auto"),
  recording: RecordingMetadataSchema,
  transcript: TranscriptSchema.optional(),
  useMockTranscript: z.boolean().default(false),
  scene: SceneSchema,
  sequences: z.array(SequenceSchema),
  contextObservations: z.array(ContextObservationSchema).default([]),
  options: AnalysisOptionsSchema,
});

export type AnalyzeRecordingRequest = z.infer<
  typeof AnalyzeRecordingRequestSchema
>;
export type Transcript = z.infer<typeof TranscriptSchema>;
export type RecordingMetadata = z.infer<typeof RecordingMetadataSchema>;
export type AnalysisOptions = z.infer<typeof AnalysisOptionsSchema>;
