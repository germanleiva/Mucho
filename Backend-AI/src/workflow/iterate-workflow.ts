import { v4 as uuidv4 } from "uuid";
import { extractTranscriptChapterFragments } from "../graph/nodes/extractChapters.node.js";
import {
  ActionPlanningOutputSchema,
  StateMergeOutputSchema,
  type ActionPlanningProvider,
  type StateMergeProvider,
} from "./workflow.providers.js";
import {
  type StatePlan,
  type WorkflowAction,
  type WorkflowChapter,
  type WorkflowIterationRequest,
  type WorkflowIterationResponse,
  type WorkflowTimelineSequence,
} from "./workflow.schema.js";
import { validateWorkflowSemantics } from "./workflow.validation.js";

export type WorkflowProviders = {
  actionProvider: ActionPlanningProvider;
  stateProvider: StateMergeProvider;
};

export async function iterateWorkflow(
  request: WorkflowIterationRequest,
  providers: WorkflowProviders
): Promise<WorkflowIterationResponse> {
  validateWorkflowSemantics(request);
  const chapters = buildWorkflowChapters(request);
  const context = { request, chapters };
  const warnings: WorkflowIterationResponse["warnings"] = [];
  const unresolvedIntents: WorkflowIterationResponse["unresolvedIntents"] = [];
  const actionOutputResult = ActionPlanningOutputSchema.safeParse(
    await providers.actionProvider.planActions(context)
  );

  if (!actionOutputResult.success) {
    warnings.push({
      code: "INVALID_ACTION_PLAN",
      message: "The action model returned an invalid structured result.",
    });
    unresolvedIntents.push({
      summary: "Action plan requires review",
      reason: actionOutputResult.error.message,
      evidenceIds: [],
    });
    return responseBase(request, chapters, {
      workflowStatus: "needs_review",
      actionsComplete: false,
      nextStep: "review",
      warnings,
      unresolvedIntents,
    });
  }

  const providerOutput = actionOutputResult.data;
  unresolvedIntents.push(...providerOutput.unresolvedIntents);
  const actions = validateActions(
    request,
    chapters,
    providerOutput.actions,
    warnings,
    unresolvedIntents
  );

  if (actions.length > 0) {
    if (request.iteration >= request.options.maxActionIterations) {
      unresolvedIntents.push({
        summary: "Maximum action iterations reached",
        reason: `The workflow reached its limit of ${request.options.maxActionIterations} action iterations.`,
        evidenceIds: [],
      });
      return responseBase(request, chapters, {
        workflowStatus: "needs_review",
        actionsComplete: false,
        nextStep: "review",
        warnings,
        unresolvedIntents,
      });
    }

    const requiresResimulation = actions.some(
      (action) => action.requiresResimulation
    );
    return responseBase(request, chapters, {
      workflowStatus: "continue",
      actionsComplete: false,
      nextStep: requiresResimulation
        ? "apply_actions_and_resimulate"
        : "apply_actions",
      actions,
      warnings,
      unresolvedIntents,
    });
  }

  if (unresolvedIntents.length > 0) {
    return responseBase(request, chapters, {
      workflowStatus: "needs_review",
      actionsComplete: false,
      nextStep: "review",
      warnings,
      unresolvedIntents,
    });
  }

  const stateOutputResult = StateMergeOutputSchema.safeParse(
    await providers.stateProvider.mergeStates(context)
  );
  if (!stateOutputResult.success) {
    warnings.push({
      code: "INVALID_STATE_PLAN",
      message: "The state model returned an invalid structured result.",
    });
    unresolvedIntents.push({
      summary: "State plan requires review",
      reason: stateOutputResult.error.message,
      evidenceIds: request.statePlaceholders.map(
        (placeholder) => placeholder.placeholderId
      ),
    });
    return responseBase(request, chapters, {
      workflowStatus: "needs_review",
      actionsComplete: true,
      nextStep: "review",
      warnings,
      unresolvedIntents,
    });
  }

  const stateOutput = stateOutputResult.data;
  const stateResult = buildStatePlan(request, stateOutput.groups);

  if (!stateResult.plan) {
    warnings.push({
      code: "INVALID_STATE_PLAN",
      message: stateResult.error,
    });
    unresolvedIntents.push({
      summary: "State plan requires review",
      reason: stateResult.error,
      evidenceIds: request.statePlaceholders.map(
        (placeholder) => placeholder.placeholderId
      ),
    });
    return responseBase(request, chapters, {
      workflowStatus: "needs_review",
      actionsComplete: true,
      nextStep: "review",
      warnings,
      unresolvedIntents,
    });
  }

  if (
    stateResult.plan.confidence < request.options.stateConfidenceThreshold
  ) {
    unresolvedIntents.push({
      summary: "Low-confidence state merge",
      reason: `State confidence ${stateResult.plan.confidence.toFixed(2)} is below the required ${request.options.stateConfidenceThreshold.toFixed(2)}.`,
      evidenceIds: request.statePlaceholders.map(
        (placeholder) => placeholder.placeholderId
      ),
    });
    return responseBase(request, chapters, {
      workflowStatus: "needs_review",
      actionsComplete: true,
      nextStep: "review",
      statePlan: stateResult.plan,
      warnings,
      unresolvedIntents,
    });
  }

  return responseBase(request, chapters, {
    workflowStatus: "completed",
    actionsComplete: true,
    nextStep: "apply_state_plan",
    statePlan: stateResult.plan,
    warnings,
    unresolvedIntents,
  });
}

function buildWorkflowChapters(
  request: WorkflowIterationRequest
): WorkflowChapter[] {
  const fragments = extractTranscriptChapterFragments(
    request.transcript,
    request.recording.durationFrames,
    request.recording.frameRate,
    request.recording.audioStartFrame
  );

  return fragments.map((fragment, index) => ({
    chapterId: `chapter-${index + 1}`,
    summary: fragment.text,
    frameStart: fragment.frameStart,
    frameEnd: fragment.frameEnd,
    relatedSequenceIds: request.timeline.sequences
      .filter(
        (sequence) =>
          sequence.enabled &&
          rangesOverlap(
            fragment.frameStart,
            fragment.frameEnd,
            sequence.startFrame,
            sequence.endFrame
          )
      )
      .map((sequence) => sequence.sequenceId),
    confidence: request.transcript.segments.length > 0 ? 0.9 : 0.8,
  }));
}

function validateActions(
  request: WorkflowIterationRequest,
  chapters: WorkflowChapter[],
  suggestions: Awaited<
    ReturnType<ActionPlanningProvider["planActions"]>
  >["actions"],
  warnings: WorkflowIterationResponse["warnings"],
  unresolvedIntents: WorkflowIterationResponse["unresolvedIntents"]
): WorkflowAction[] {
  const assetById = new Map(
    request.scene.assets.map((asset) => [asset.assetId, asset])
  );
  const contextIds = new Set(
    request.scene.contextObjects.map((object) => object.objectId)
  );
  const sequenceById = new Map(
    request.timeline.sequences.map((sequence) => [
      sequence.sequenceId,
      sequence,
    ])
  );
  const candidateById = new Map(
    request.forceVectorCandidates.map((candidate) => [
      candidate.candidateId,
      candidate,
    ])
  );
  const evidenceIds = new Set([
    ...sequenceById.keys(),
    ...candidateById.keys(),
    ...chapters.map((chapter) => chapter.chapterId),
  ]);
  const existingKeys = new Set(
    request.timeline.sequences
      .filter(
        (
          sequence
        ): sequence is Extract<
          WorkflowTimelineSequence,
          { sequenceKind: "action" }
        > => sequence.sequenceKind === "action" && sequence.enabled
      )
      .map(existingActionKey)
  );
  const outputKeys = new Set<string>();
  const actions: WorkflowAction[] = [];

  for (const suggestion of suggestions) {
    const problems: string[] = [];
    const asset = assetById.get(suggestion.targetAssetId);
    if (!asset) {
      problems.push(`Unknown target asset ${suggestion.targetAssetId}.`);
    } else if (!asset.capabilities.includes(suggestion.actionType)) {
      problems.push(
        `Asset ${suggestion.targetAssetId} does not support ${suggestion.actionType}.`
      );
    }

    const unknownEvidence = suggestion.evidenceIds.filter(
      (id) => !evidenceIds.has(id)
    );
    if (unknownEvidence.length > 0) {
      problems.push(`Unknown evidence IDs: ${unknownEvidence.join(", ")}.`);
    }

    let startFrame = 0;
    let params: Record<string, unknown> = {};

    if (suggestion.actionType === "applyForce") {
      const candidate = candidateById.get(suggestion.forceCandidateId);
      if (!candidate) {
        problems.push(
          `Unknown force candidate ${suggestion.forceCandidateId}.`
        );
      } else {
        startFrame = candidate.frame;
        if (candidate.assetId !== suggestion.targetAssetId) {
          problems.push(
            `Force candidate ${candidate.candidateId} belongs to ${candidate.assetId}, not ${suggestion.targetAssetId}.`
          );
        }
        params = {
          forceCandidateId: candidate.candidateId,
          scale: suggestion.scale,
          initialVelocity: scaleVector(
            candidate.initialVelocity,
            suggestion.scale
          ),
        };
      }
    } else {
      const anchorResult = resolveAnchor(
        suggestion.anchor,
        sequenceById,
        request.recording.durationFrames
      );
      if (anchorResult.error) {
        problems.push(anchorResult.error);
      } else {
        startFrame = anchorResult.frame;
      }

      if (suggestion.actionType === "changeColor") {
        params = { color: suggestion.color };
      } else if (suggestion.actionType === "follow") {
        if (!contextIds.has(suggestion.followTargetId)) {
          problems.push(
            `Unknown follow target ${suggestion.followTargetId}.`
          );
        }
        params = { followTargetId: suggestion.followTargetId };
      }
    }

    if (startFrame >= request.recording.durationFrames) {
      problems.push(`Action frame ${startFrame} is outside the recording.`);
    }

    if (problems.length > 0) {
      addRejectedSuggestion(
        suggestion.actionType,
        suggestion.targetAssetId,
        problems.join(" "),
        suggestion.evidenceIds,
        warnings,
        unresolvedIntents
      );
      continue;
    }

    if (
      suggestion.confidence < request.options.actionConfidenceThreshold
    ) {
      unresolvedIntents.push({
        summary: `Low-confidence ${suggestion.actionType} action`,
        reason: `Confidence ${suggestion.confidence.toFixed(2)} is below the required ${request.options.actionConfidenceThreshold.toFixed(2)}. ${suggestion.reason}`,
        evidenceIds: suggestion.evidenceIds,
      });
      continue;
    }

    const action: WorkflowAction = {
      actionId: uuidv4(),
      actionType: suggestion.actionType,
      targetAssetId: suggestion.targetAssetId,
      startFrame,
      params,
      evidenceIds: suggestion.evidenceIds,
      confidence: suggestion.confidence,
      reason: suggestion.reason,
      requiresResimulation: suggestion.actionType === "applyForce",
    };
    const key = proposedActionKey(action);

    if (existingKeys.has(key) || outputKeys.has(key)) {
      warnings.push({
        code: "DUPLICATE_ACTION",
        message: `Action ${action.actionType} on ${action.targetAssetId} at frame ${action.startFrame} already exists.`,
      });
      continue;
    }

    outputKeys.add(key);
    actions.push(action);
  }

  return actions;
}

function buildStatePlan(
  request: WorkflowIterationRequest,
  groups: Array<{
    placeholderIds: string[];
    name: string;
    rationale: string;
    confidence: number;
  }>
): { plan: StatePlan | null; error: string } {
  const expectedIds = request.statePlaceholders.map(
    (placeholder) => placeholder.placeholderId
  );
  const actualIds = groups.flatMap((group) => group.placeholderIds);

  if (
    actualIds.length !== expectedIds.length ||
    actualIds.some((id, index) => id !== expectedIds[index])
  ) {
    return {
      plan: null,
      error:
        "State groups must include every placeholder exactly once in timeline order.",
    };
  }

  const placeholderById = new Map(
    request.statePlaceholders.map((placeholder) => [
      placeholder.placeholderId,
      placeholder,
    ])
  );
  const states: StatePlan["states"] = [];

  for (const [index, group] of groups.entries()) {
    const placeholders = group.placeholderIds.map((id) =>
      placeholderById.get(id)
    );
    if (placeholders.some((placeholder) => !placeholder)) {
      return {
        plan: null,
        error: "State groups reference an unknown placeholder.",
      };
    }

    const resolved = placeholders as NonNullable<
      (typeof placeholders)[number]
    >[];
    for (let i = 1; i < resolved.length; i++) {
      if (resolved[i - 1].frameEnd !== resolved[i].frameStart) {
        return {
          plan: null,
          error: "State groups may merge only adjacent placeholders.",
        };
      }
    }

    states.push({
      stateId: `${request.workflowId}-state-${index + 1}`,
      name: group.name.trim(),
      frameStart: resolved[0].frameStart,
      frameEnd: resolved[resolved.length - 1].frameEnd,
      sourcePlaceholderIds: group.placeholderIds,
      rationale: group.rationale,
      confidence: group.confidence,
    });
  }

  if (states.length === 0) {
    return { plan: null, error: "State plan contains no states." };
  }

  return {
    plan: {
      states,
      confidence: Math.min(...states.map((state) => state.confidence)),
    },
    error: "",
  };
}

function responseBase(
  request: WorkflowIterationRequest,
  chapters: WorkflowChapter[],
  values: {
    workflowStatus: WorkflowIterationResponse["workflowStatus"];
    actionsComplete: boolean;
    nextStep: WorkflowIterationResponse["nextStep"];
    actions?: WorkflowAction[];
    statePlan?: StatePlan | null;
    warnings: WorkflowIterationResponse["warnings"];
    unresolvedIntents: WorkflowIterationResponse["unresolvedIntents"];
  }
): WorkflowIterationResponse {
  return {
    workflowId: request.workflowId,
    iteration: request.iteration,
    workflowStatus: values.workflowStatus,
    actionsComplete: values.actionsComplete,
    nextStep: values.nextStep,
    chapters,
    timelinePatch: {
      operations: (values.actions ?? []).map((action) => ({
        op: "addAction",
        action,
      })),
    },
    statePlan: values.statePlan ?? null,
    warnings: values.warnings,
    unresolvedIntents: values.unresolvedIntents,
  };
}

function resolveAnchor(
  anchor:
    | { kind: "recording_start" }
    | { kind: "sequence_start"; sequenceId: string }
    | { kind: "sequence_end"; sequenceId: string },
  sequenceById: Map<string, WorkflowTimelineSequence>,
  durationFrames: number
): { frame: number; error?: undefined } | { frame: number; error: string } {
  if (anchor.kind === "recording_start") {
    return { frame: 0 };
  }

  const sequence = sequenceById.get(anchor.sequenceId);
  if (!sequence || !sequence.enabled) {
    return {
      frame: 0,
      error: `Anchor references unknown or disabled sequence ${anchor.sequenceId}.`,
    };
  }

  const frame =
    anchor.kind === "sequence_start"
      ? sequence.startFrame
      : sequence.endFrame;
  if (frame >= durationFrames) {
    return {
      frame,
      error: `Anchor ${anchor.kind} for ${anchor.sequenceId} is outside the recording.`,
    };
  }
  return { frame };
}

function existingActionKey(
  sequence: Extract<
    WorkflowTimelineSequence,
    { sequenceKind: "action" }
  >
): string {
  return actionKey(
    sequence.actionType,
    sequence.targetAssetId,
    sequence.startFrame,
    sequence.params
  );
}

function proposedActionKey(action: WorkflowAction): string {
  return actionKey(
    action.actionType,
    action.targetAssetId,
    action.startFrame,
    action.params
  );
}

function actionKey(
  actionType: WorkflowAction["actionType"],
  targetAssetId: string,
  startFrame: number,
  params: Record<string, unknown>
): string {
  let relevantParams = "";
  if (actionType === "changeColor") {
    relevantParams = `color:${String(params.color)}`;
  } else if (actionType === "follow") {
    relevantParams = `followTargetId:${String(params.followTargetId)}`;
  }

  return [actionType, targetAssetId, startFrame, relevantParams].join("|");
}

function addRejectedSuggestion(
  actionType: string,
  targetAssetId: string,
  reason: string,
  evidenceIds: string[],
  warnings: WorkflowIterationResponse["warnings"],
  unresolvedIntents: WorkflowIterationResponse["unresolvedIntents"]
): void {
  warnings.push({
    code: "INVALID_ACTION_PROPOSAL",
    message: `${actionType} on ${targetAssetId}: ${reason}`,
  });
  unresolvedIntents.push({
    summary: `Invalid ${actionType} proposal`,
    reason,
    evidenceIds,
  });
}

function scaleVector(
  vector: { x: number; y: number; z: number },
  scale: number
): { x: number; y: number; z: number } {
  return {
    x: vector.x * scale,
    y: vector.y * scale,
    z: vector.z * scale,
  };
}

function rangesOverlap(
  leftStart: number,
  leftEnd: number,
  rightStart: number,
  rightEnd: number
): boolean {
  if (rightStart === rightEnd) {
    return rightStart >= leftStart && rightStart < leftEnd;
  }
  return rightStart < leftEnd && rightEnd > leftStart;
}
