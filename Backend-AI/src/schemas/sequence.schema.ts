import { z } from "zod";

export const BaseSequenceSchema = z.object({
  sequenceId: z.string(),
  sequenceKind: z.enum(["gesture", "collision", "existingAction"]),
  startFrame: z.number().int().nonnegative(),
  length: z.number().int().nonnegative(),
});

export const GestureSequenceSchema = BaseSequenceSchema.extend({
  sequenceKind: z.literal("gesture"),
  rawGesture: z.enum([
    "Gesture.LEFTHANDGRAB",
    "Gesture.LEFTHANDPINCH",
    "Gesture.LEFTHANDOPEN",
    "Gesture.RIGHTHANDGRAB",
    "Gesture.RIGHTHANDPINCH",
    "Gesture.RIGHTHANDOPEN",
  ]),
  gestureLabel: z.enum(["Closed", "Pinch", "Open"]),
  hand: z.enum(["left", "right"]),
});

export const CollisionSequenceSchema = BaseSequenceSchema.extend({
  sequenceKind: z.literal("collision"),
  objectAName: z.string(),
  objectBName: z.string(),
});

export const ActionTypeSchema = z.enum([
  "show",
  "hide",
  "changeColor",
  "follow",
  "unfollow",
  "throwAsset",
]);

export type ActionType = z.infer<typeof ActionTypeSchema>;

export const ExistingActionSequenceSchema = BaseSequenceSchema.extend({
  sequenceKind: z.literal("existingAction"),
  actionType: ActionTypeSchema,
  targetAssetName: z.string(),
  params: z.record(z.unknown()).default({}),
  source: z.enum(["default", "manual", "ai", "unknown"]).default("unknown"),
});

export const SequenceSchema = z.discriminatedUnion("sequenceKind", [
  GestureSequenceSchema,
  CollisionSequenceSchema,
  ExistingActionSequenceSchema,
]);

export type Sequence = z.infer<typeof SequenceSchema>;
export type GestureSequence = z.infer<typeof GestureSequenceSchema>;
export type CollisionSequence = z.infer<typeof CollisionSequenceSchema>;
export type ExistingActionSequence = z.infer<typeof ExistingActionSequenceSchema>;
