import { z } from "zod";
import { ActionToRecordSchema } from "./action.schema.js";

export const DebugChapterSchema = z.object({
  chapterId: z.string(),
  summary: z.string(),
  frameStart: z.number().int().nonnegative(),
  frameEnd: z.number().int().nonnegative(),
  relatedSequenceIds: z.array(z.string()).default([]),
  relatedObservationIndexes: z.array(z.number()).default([]),
  confidence: z.number().min(0).max(1),
});

export const UnresolvedIntentSchema = z.object({
  intentId: z.string(),
  summary: z.string(),
  reason: z.string(),
  relatedSequenceIds: z.array(z.string()).default([]),
});

export const WarningSchema = z.object({
  code: z.string(),
  message: z.string(),
});

export const AnalyzeRecordingResponseSchema = z.object({
  analysisId: z.string(),
  recordingId: z.string(),
  status: z.enum(["completed", "partial", "failed"]),
  transcript: z.object({
    text: z.string(),
    language: z.string().optional(),
    source: z.enum(["mock", "provided", "transcribed"]),
  }),
  chapters: z.array(DebugChapterSchema),
  actionsToRecord: z.array(ActionToRecordSchema),
  unresolvedIntents: z.array(UnresolvedIntentSchema),
  warnings: z.array(WarningSchema),
});

export type AnalyzeRecordingResponse = z.infer<
  typeof AnalyzeRecordingResponseSchema
>;
export type DebugChapter = z.infer<typeof DebugChapterSchema>;
export type UnresolvedIntent = z.infer<typeof UnresolvedIntentSchema>;
export type Warning = z.infer<typeof WarningSchema>;
