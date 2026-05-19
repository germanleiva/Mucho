import type { ActionToRecord } from "../../schemas/action.schema.js";
import type { ExistingActionSequence } from "../../schemas/sequence.schema.js";
import type { AnalyzeRecordingState } from "../state.js";

export function validateAndFilterActions(
  state: AnalyzeRecordingState
): AnalyzeRecordingState {
  const { request, actionCandidates } = state;
  const confidenceThreshold = request.options.confidenceThreshold ?? 0.88;
  const passedThreshold = actionCandidates.filter(
    (action) => action.confidence >= confidenceThreshold
  );
  const deduplicated: ActionToRecord[] = [];
  const seenDuplicateWarnings = new Set<string>();
  const seenCandidateKeys = new Set<string>();
  const existingActions = request.sequences.filter(
    (seq): seq is ExistingActionSequence => seq.sequenceKind === "existingAction"
  );

  for (const candidate of passedThreshold) {
    const candidateKey = actionKey(candidate);
    const isRepeatedCandidate = seenCandidateKeys.has(candidateKey);
    const isDuplicate = existingActions.some((seq) =>
      existingActionMatchesCandidate(seq, candidate)
    );

    if (isDuplicate || isRepeatedCandidate) {
      if (!seenDuplicateWarnings.has(candidateKey)) {
        seenDuplicateWarnings.add(candidateKey);
        state.warnings.push({
          code: "DUPLICATE_ACTION",
          message: `Action ${candidate.actionType} on ${candidate.targetAssetName} at frame ${candidate.startFrame} already exists. Skipping.`,
        });
      }
    } else {
      seenCandidateKeys.add(candidateKey);
      deduplicated.push(candidate);
    }
  }

  state.actionsToRecord = ensureThrowUnfollows(deduplicated);
  return state;
}

function ensureThrowUnfollows(actions: ActionToRecord[]): ActionToRecord[] {
  const withUnfollows: ActionToRecord[] = [];

  for (const action of actions) {
    if (action.actionType === "throwAsset") {
      const hasUnfollow = actions.some(
        (candidate) =>
          candidate.actionType === "unfollow" &&
          candidate.targetAssetName === action.targetAssetName &&
          candidate.startFrame === action.startFrame
      );

      if (!hasUnfollow) {
        withUnfollows.push({
          actionId: action.actionId,
          actionType: "unfollow",
          targetAssetName: action.targetAssetName,
          startFrame: action.startFrame,
          explanation: "Throwing an asset requires it to stop following first.",
          confidence: action.confidence,
        });
      }
    }

    withUnfollows.push(action);
  }

  return withUnfollows;
}

function existingActionMatchesCandidate(
  existing: ExistingActionSequence,
  candidate: ActionToRecord
): boolean {
  return (
    existing.actionType === candidate.actionType &&
    existing.targetAssetName === candidate.targetAssetName &&
    existing.startFrame === candidate.startFrame &&
    existingParamsKey(existing, candidate.actionType) ===
      actionParamsKey(candidate)
  );
}

function actionKey(action: ActionToRecord): string {
  return [
    action.actionType,
    action.targetAssetName,
    action.startFrame,
    actionParamsKey(action),
  ].join("|");
}

function actionParamsKey(action: ActionToRecord): string {
  switch (action.actionType) {
    case "changeColor":
      return `color:${action.color}`;
    case "follow":
      return `followTargetType:${action.followTargetType}`;
    case "throwAsset":
      return `forceVelocityMode:${JSON.stringify(action.forceVelocityMode)}`;
    default:
      return "";
  }
}

function existingParamsKey(
  existing: ExistingActionSequence,
  actionType: ActionToRecord["actionType"]
): string {
  const params = existing.params ?? {};

  switch (actionType) {
    case "changeColor":
      return `color:${String(params.color)}`;
    case "follow":
      return `followTargetType:${String(params.followTargetType)}`;
    case "throwAsset":
      return `forceVelocityMode:${JSON.stringify(params.forceVelocityMode)}`;
    default:
      return "";
  }
}
