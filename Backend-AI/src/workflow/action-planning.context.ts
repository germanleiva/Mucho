import { ACTION_PROMPT_VERSION } from "../prompts/action-planning.prompt.js";
import type { WorkflowPlanningContext } from "./workflow.providers.js";
import type { WorkflowTimelineSequence } from "./workflow.schema.js";

type SequenceOfKind<
  Kind extends WorkflowTimelineSequence["sequenceKind"],
> = Extract<WorkflowTimelineSequence, { sequenceKind: Kind }>;

export type NormalizedActionPlanningContext = {
  promptVersion: typeof ACTION_PROMPT_VERSION;
  workflow: {
    workflowId: string;
    iteration: number;
    maxActionIterations: number;
  };
  recording: WorkflowPlanningContext["request"]["recording"];
  transcript: WorkflowPlanningContext["request"]["transcript"];
  chapters: WorkflowPlanningContext["chapters"];
  scene: WorkflowPlanningContext["request"]["scene"];
  timeline: {
    gestures: SequenceOfKind<"gesture">[];
    voiceCommands: SequenceOfKind<"voice">[];
    collisions: SequenceOfKind<"collision">[];
    existingActions: SequenceOfKind<"action">[];
  };
  forceCandidates: WorkflowPlanningContext["request"]["forceVectorCandidates"];
  actionConfidenceThreshold: number;
};

export function buildActionPlanningContext(
  context: WorkflowPlanningContext
): NormalizedActionPlanningContext {
  const enabledSequences = context.request.timeline.sequences.filter(
    (sequence) => sequence.enabled
  );

  return {
    promptVersion: ACTION_PROMPT_VERSION,
    workflow: {
      workflowId: context.request.workflowId,
      iteration: context.request.iteration,
      maxActionIterations: context.request.options.maxActionIterations,
    },
    recording: context.request.recording,
    transcript: context.request.transcript,
    chapters: context.chapters,
    scene: context.request.scene,
    timeline: {
      gestures: sequencesOfKind(enabledSequences, "gesture"),
      voiceCommands: sequencesOfKind(enabledSequences, "voice"),
      collisions: sequencesOfKind(enabledSequences, "collision"),
      existingActions: sequencesOfKind(enabledSequences, "action"),
    },
    forceCandidates: context.request.forceVectorCandidates,
    actionConfidenceThreshold:
      context.request.options.actionConfidenceThreshold,
  };
}

function sequencesOfKind<
  Kind extends WorkflowTimelineSequence["sequenceKind"],
>(
  sequences: WorkflowTimelineSequence[],
  kind: Kind
): SequenceOfKind<Kind>[] {
  return sequences.filter(
    (sequence): sequence is SequenceOfKind<Kind> =>
      sequence.sequenceKind === kind
  );
}
