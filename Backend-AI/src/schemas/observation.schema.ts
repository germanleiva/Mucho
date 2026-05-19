import { z } from "zod";

export const ObjectPointedObservationSchema = z.object({
  observationType: z.literal("objectPointed"),
  objectName: z.string(),
  source: z.enum(["LEFTFOCUS", "RIGHTFOCUS"]),
  startFrame: z.number().int().nonnegative(),
  length: z.number().int().positive(),
});

export const ObjectFocusedObservationSchema = z.object({
  observationType: z.literal("objectFocused"),
  objectName: z.string(),
  source: z.literal("GAZEFOCUS"),
  startFrame: z.number().int().nonnegative(),
  length: z.number().int().positive(),
});

export const ObjectInCameraObservationSchema = z.object({
  observationType: z.literal("objectInCamera"),
  objectName: z.string(),
  startFrame: z.number().int().nonnegative(),
  length: z.number().int().positive(),
});

export const ContextObservationSchema = z.discriminatedUnion(
  "observationType",
  [
    ObjectPointedObservationSchema,
    ObjectFocusedObservationSchema,
    ObjectInCameraObservationSchema,
  ]
);

export type ContextObservation = z.infer<typeof ContextObservationSchema>;
export type ObjectPointedObservation = z.infer<
  typeof ObjectPointedObservationSchema
>;
export type ObjectFocusedObservation = z.infer<
  typeof ObjectFocusedObservationSchema
>;
export type ObjectInCameraObservation = z.infer<
  typeof ObjectInCameraObservationSchema
>;
