import { z } from "zod";

export const Vector3Schema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

export type Vector3 = z.infer<typeof Vector3Schema>;

export const ShowActionSchema = z.object({
  actionId: z.string(),
  actionType: z.literal("show"),
  targetAssetName: z.string(),
  startFrame: z.number().int().nonnegative(),
  explanation: z.string(),
  confidence: z.number().min(0).max(1),
});

export const HideActionSchema = z.object({
  actionId: z.string(),
  actionType: z.literal("hide"),
  targetAssetName: z.string(),
  startFrame: z.number().int().nonnegative(),
  explanation: z.string(),
  confidence: z.number().min(0).max(1),
});

export const ChangeColorActionSchema = z.object({
  actionId: z.string(),
  actionType: z.literal("changeColor"),
  targetAssetName: z.string(),
  startFrame: z.number().int().nonnegative(),
  color: z.enum([
    "red",
    "green",
    "blue",
    "yellow",
    "pink",
    "gray",
    "white",
    "black",
  ]),
  explanation: z.string(),
  confidence: z.number().min(0).max(1),
});

export const FollowActionSchema = z.object({
  actionId: z.string(),
  actionType: z.literal("follow"),
  targetAssetName: z.string(),
  startFrame: z.number().int().nonnegative(),
  followTargetType: z.enum([
    "LEFTHAND",
    "RIGHTHAND",
    "LEFTFOCUS",
    "RIGHTFOCUS",
    "GAZEFOCUS",
  ]),
  explanation: z.string(),
  confidence: z.number().min(0).max(1),
});

export const UnfollowActionSchema = z.object({
  actionId: z.string(),
  actionType: z.literal("unfollow"),
  targetAssetName: z.string(),
  startFrame: z.number().int().nonnegative(),
  explanation: z.string(),
  confidence: z.number().min(0).max(1),
});

export const ThrowAssetActionSchema = z.object({
  actionId: z.string(),
  actionType: z.literal("throwAsset"),
  targetAssetName: z.string(),
  startFrame: z.number().int().nonnegative(),
  forceVelocityMode: Vector3Schema,
  explanation: z.string(),
  confidence: z.number().min(0).max(1),
});

export const ActionToRecordSchema = z.discriminatedUnion("actionType", [
  ShowActionSchema,
  HideActionSchema,
  ChangeColorActionSchema,
  FollowActionSchema,
  UnfollowActionSchema,
  ThrowAssetActionSchema,
]);

export type ActionToRecord = z.infer<typeof ActionToRecordSchema>;
export type ShowAction = z.infer<typeof ShowActionSchema>;
export type HideAction = z.infer<typeof HideActionSchema>;
export type ChangeColorAction = z.infer<typeof ChangeColorActionSchema>;
export type FollowAction = z.infer<typeof FollowActionSchema>;
export type UnfollowAction = z.infer<typeof UnfollowActionSchema>;
export type ThrowAssetAction = z.infer<typeof ThrowAssetActionSchema>;
