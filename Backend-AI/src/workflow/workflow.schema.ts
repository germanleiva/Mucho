import { z } from "zod";

export const WorkflowVector3Schema = z.object({
  x: z.number().finite(),
  y: z.number().finite(),
  z: z.number().finite(),
});

export const WorkflowActionTypeSchema = z.enum([
  "show",
  "hide",
  "changeColor",
  "follow",
  "unfollow",
  "applyForce",
]);

const TimelineSequenceBaseSchema = z.object({
  sequenceId: z.string().min(1),
  enabled: z.boolean().default(true),
  startFrame: z.number().int().nonnegative(),
  endFrame: z.number().int().nonnegative(),
});

export const WorkflowGestureSequenceSchema =
  TimelineSequenceBaseSchema.extend({
    sequenceKind: z.literal("gesture"),
    gestureLabel: z.enum(["Closed", "Pinch", "Open"]),
    hand: z.enum(["left", "right"]),
  });

export const WorkflowCollisionSequenceSchema =
  TimelineSequenceBaseSchema.extend({
    sequenceKind: z.literal("collision"),
    objectAId: z.string().min(1),
    objectBId: z.string().min(1),
  });

export const WorkflowVoiceSequenceSchema = TimelineSequenceBaseSchema.extend({
  sequenceKind: z.literal("voice"),
  command: z.string().min(1),
});

export const WorkflowExistingActionSequenceSchema =
  TimelineSequenceBaseSchema.extend({
    sequenceKind: z.literal("action"),
    actionType: WorkflowActionTypeSchema,
    targetAssetId: z.string().min(1),
    params: z.record(z.unknown()).default({}),
    source: z.enum(["default", "manual", "ai", "unknown"]).default("unknown"),
  });

export const WorkflowTimelineSequenceSchema = z.discriminatedUnion(
  "sequenceKind",
  [
    WorkflowGestureSequenceSchema,
    WorkflowCollisionSequenceSchema,
    WorkflowVoiceSequenceSchema,
    WorkflowExistingActionSequenceSchema,
  ]
);

export const WorkflowAssetSchema = z.object({
  assetId: z.string().min(1),
  assetName: z.string().min(1),
  displayName: z.string().optional(),
  assetKind: z.string().min(1),
  aliases: z.array(z.string()).default([]),
  capabilities: z.array(WorkflowActionTypeSchema).default([]),
});

export const WorkflowContextObjectSchema = z.object({
  objectId: z.string().min(1),
  objectName: z.string().min(1),
  displayName: z.string().optional(),
  objectKind: z.string().min(1),
  aliases: z.array(z.string()).default([]),
});

export const WorkflowTranscriptSchema = z.object({
  text: z.string().min(1),
  language: z.enum(["en", "it", "da", "unknown"]).default("unknown"),
  segments: z
    .array(
      z.object({
        segmentId: z.string().min(1).optional(),
        text: z.string(),
        timeStartMs: z.number().nonnegative().optional(),
        timeEndMs: z.number().nonnegative().optional(),
      })
    )
    .default([]),
});

export const StatePlaceholderSchema = z.object({
  placeholderId: z.string().min(1),
  frameStart: z.number().int().nonnegative(),
  frameEnd: z.number().int().nonnegative(),
});

export const ForceVectorCandidateSchema = z.object({
  candidateId: z.string().min(1),
  assetId: z.string().min(1),
  frame: z.number().int().nonnegative(),
  initialVelocity: WorkflowVector3Schema,
  source: z.enum([
    "hand_velocity",
    "aim",
    "trajectory",
    "manual",
    "other",
  ]),
  aimTargetAssetId: z.string().min(1).optional(),
});

export const WorkflowOptionsSchema = z
  .object({
    actionConfidenceThreshold: z.number().min(0).max(1).default(0.85),
    stateConfidenceThreshold: z.number().min(0).max(1).default(0.85),
    maxActionIterations: z.number().int().positive().default(6),
  })
  .default({});

export const WorkflowIterationRequestSchema = z.object({
  workflowId: z.string().min(1),
  iteration: z.number().int().nonnegative(),
  recording: z.object({
    frameRate: z.number().positive(),
    durationFrames: z.number().int().positive(),
    audioStartFrame: z.number().int().nonnegative().default(0),
  }),
  transcript: WorkflowTranscriptSchema,
  scene: z.object({
    assets: z.array(WorkflowAssetSchema),
    contextObjects: z.array(WorkflowContextObjectSchema).default([]),
  }),
  timeline: z.object({
    sequences: z.array(WorkflowTimelineSequenceSchema),
  }),
  statePlaceholders: z.array(StatePlaceholderSchema).min(1),
  forceVectorCandidates: z.array(ForceVectorCandidateSchema).default([]),
  options: WorkflowOptionsSchema,
});

export const WorkflowChapterSchema = z.object({
  chapterId: z.string(),
  summary: z.string(),
  frameStart: z.number().int().nonnegative(),
  frameEnd: z.number().int().nonnegative(),
  relatedSequenceIds: z.array(z.string()),
  confidence: z.number().min(0).max(1),
});

export const WorkflowActionSchema = z.object({
  actionId: z.string(),
  actionType: WorkflowActionTypeSchema,
  targetAssetId: z.string(),
  startFrame: z.number().int().nonnegative(),
  params: z.record(z.unknown()),
  evidenceIds: z.array(z.string()),
  confidence: z.number().min(0).max(1),
  reason: z.string(),
  requiresResimulation: z.boolean(),
});

export const TimelinePatchSchema = z.object({
  operations: z.array(
    z.object({
      op: z.literal("addAction"),
      action: WorkflowActionSchema,
    })
  ),
});

export const WorkflowUnresolvedIntentSchema = z.object({
  summary: z.string(),
  reason: z.string(),
  evidenceIds: z.array(z.string()).default([]),
});

export const WorkflowWarningSchema = z.object({
  code: z.string(),
  message: z.string(),
});

export const MergedStateSchema = z.object({
  stateId: z.string(),
  name: z.string(),
  frameStart: z.number().int().nonnegative(),
  frameEnd: z.number().int().nonnegative(),
  sourcePlaceholderIds: z.array(z.string()).min(1),
  rationale: z.string(),
  confidence: z.number().min(0).max(1),
});

export const StatePlanSchema = z.object({
  states: z.array(MergedStateSchema).min(1),
  confidence: z.number().min(0).max(1),
});

export const WorkflowIterationResponseSchema = z.object({
  workflowId: z.string(),
  iteration: z.number().int().nonnegative(),
  workflowStatus: z.enum([
    "continue",
    "completed",
    "needs_review",
    "failed",
  ]),
  actionsComplete: z.boolean(),
  nextStep: z.enum([
    "apply_actions",
    "apply_actions_and_resimulate",
    "apply_state_plan",
    "review",
    "none",
  ]),
  chapters: z.array(WorkflowChapterSchema),
  timelinePatch: TimelinePatchSchema,
  statePlan: StatePlanSchema.nullable(),
  warnings: z.array(WorkflowWarningSchema),
  unresolvedIntents: z.array(WorkflowUnresolvedIntentSchema),
});

export type WorkflowIterationRequest = z.infer<
  typeof WorkflowIterationRequestSchema
>;
export type WorkflowIterationResponse = z.infer<
  typeof WorkflowIterationResponseSchema
>;
export type WorkflowTimelineSequence = z.infer<
  typeof WorkflowTimelineSequenceSchema
>;
export type WorkflowAction = z.infer<typeof WorkflowActionSchema>;
export type WorkflowChapter = z.infer<typeof WorkflowChapterSchema>;
export type StatePlan = z.infer<typeof StatePlanSchema>;
