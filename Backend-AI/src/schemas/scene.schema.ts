import { z } from "zod";

export const AssetSchema = z.object({
  assetName: z.string().min(1),
  displayName: z.string().optional(),
  assetKind: z.enum([
    "sphere",
    "cube",
    "basketball",
    "book",
    "lamp",
    "text",
    "unknown",
  ]),
  aliases: z.array(z.string()).optional().default([]),
});

export const ContextObjectSchema = z.object({
  objectName: z.string().min(1),
  displayName: z.string().optional(),
  objectKind: z.enum([
    "leftHand",
    "rightHand",
    "leftFocus",
    "rightFocus",
    "gazeFocus",
    "camera",
    "other",
  ]),
  aliases: z.array(z.string()).optional().default([]),
});

export const SceneSchema = z.object({
  assets: z.array(AssetSchema),
  contextObjects: z.array(ContextObjectSchema),
});

export type Asset = z.infer<typeof AssetSchema>;
export type ContextObject = z.infer<typeof ContextObjectSchema>;
export type Scene = z.infer<typeof SceneSchema>;
