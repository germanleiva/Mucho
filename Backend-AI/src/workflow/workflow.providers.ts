import { z } from "zod";
import type {
  WorkflowChapter,
  WorkflowIterationRequest,
} from "./workflow.schema.js";

const EvidenceSchema = z.object({
  evidenceIds: z.array(z.string()),
  confidence: z.number().min(0).max(1),
  reason: z.string().min(1),
});

const AnchoredActionBaseSchema = EvidenceSchema.extend({
  targetAssetId: z.string().min(1),
  anchor: z.discriminatedUnion("kind", [
    z.object({ kind: z.literal("recording_start") }),
    z.object({
      kind: z.literal("sequence_start"),
      sequenceId: z.string().min(1),
    }),
    z.object({
      kind: z.literal("sequence_end"),
      sequenceId: z.string().min(1),
    }),
  ]),
});

export const ActionPlanningOutputSchema = z.object({
  actions: z.array(
    z.discriminatedUnion("actionType", [
      AnchoredActionBaseSchema.extend({
        actionType: z.literal("show"),
      }),
      AnchoredActionBaseSchema.extend({
        actionType: z.literal("hide"),
      }),
      AnchoredActionBaseSchema.extend({
        actionType: z.literal("unfollow"),
      }),
      AnchoredActionBaseSchema.extend({
        actionType: z.literal("changeColor"),
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
      }),
      AnchoredActionBaseSchema.extend({
        actionType: z.literal("follow"),
        followTargetId: z.string().min(1),
      }),
      EvidenceSchema.extend({
        actionType: z.literal("applyForce"),
        targetAssetId: z.string().min(1),
        forceCandidateId: z.string().min(1),
        scale: z.number().min(0.25).max(2),
      }),
    ])
  ),
  unresolvedIntents: z.array(
    z.object({
      summary: z.string().min(1),
      reason: z.string().min(1),
      evidenceIds: z.array(z.string()),
    })
  ),
});

export const StateMergeOutputSchema = z.object({
  groups: z.array(
    z.object({
      placeholderIds: z.array(z.string().min(1)).min(1),
      name: z.string().trim().min(1).max(40),
      rationale: z.string().min(1),
      confidence: z.number().min(0).max(1),
    })
  ),
});

export type ActionPlanningOutput = z.infer<
  typeof ActionPlanningOutputSchema
>;
export type StateMergeOutput = z.infer<typeof StateMergeOutputSchema>;

export type WorkflowPlanningContext = {
  request: WorkflowIterationRequest;
  chapters: WorkflowChapter[];
};

export interface ActionPlanningProvider {
  planActions(
    context: WorkflowPlanningContext
  ): Promise<ActionPlanningOutput>;
}

export interface StateMergeProvider {
  mergeStates(context: WorkflowPlanningContext): Promise<StateMergeOutput>;
}
