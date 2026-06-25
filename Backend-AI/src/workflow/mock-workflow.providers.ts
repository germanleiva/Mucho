import type {
  ActionPlanningOutput,
  ActionPlanningProvider,
  StateMergeOutput,
  StateMergeProvider,
  WorkflowPlanningContext,
} from "./workflow.providers.js";
import type { WorkflowTimelineSequence } from "./workflow.schema.js";

export class MockActionPlanningProvider implements ActionPlanningProvider {
  async planActions(
    context: WorkflowPlanningContext
  ): Promise<ActionPlanningOutput> {
    return buildMockActionPlan(context);
  }
}

export class MockStateMergeProvider implements StateMergeProvider {
  async mergeStates(
    context: WorkflowPlanningContext
  ): Promise<StateMergeOutput> {
    return buildMockStatePlan(context);
  }
}

export class StaticActionPlanningProvider implements ActionPlanningProvider {
  constructor(private readonly output: ActionPlanningOutput) {}

  async planActions(): Promise<ActionPlanningOutput> {
    return this.output;
  }
}

export class StaticStateMergeProvider implements StateMergeProvider {
  constructor(private readonly output: StateMergeOutput) {}

  async mergeStates(): Promise<StateMergeOutput> {
    return this.output;
  }
}

function buildMockActionPlan(
  context: WorkflowPlanningContext
): ActionPlanningOutput {
  const { request } = context;
  const text = normalize(request.transcript.text);
  const actions: ActionPlanningOutput["actions"] = [];
  const unresolvedIntents: ActionPlanningOutput["unresolvedIntents"] = [];
  const mentionedAssets = request.scene.assets.filter((asset) =>
    assetPhrases(asset).some((phrase) => text.includes(phrase))
  );
  const primaryAsset = mentionedAssets[0] ?? request.scene.assets[0];
  const pinch = findSequence(
    request.timeline.sequences,
    (sequence) =>
      sequence.sequenceKind === "gesture" &&
      (sequence.gestureLabel === "Pinch" ||
        sequence.gestureLabel === "Closed")
  );
  const open = findSequence(
    request.timeline.sequences,
    (sequence) =>
      sequence.sequenceKind === "gesture" && sequence.gestureLabel === "Open"
  );
  const collision = findSequence(
    request.timeline.sequences,
    (sequence) => sequence.sequenceKind === "collision"
  );
  const rightHand =
    request.scene.contextObjects.find((object) =>
      /right.*hand|righthand/.test(normalize(`${object.objectKind} ${object.objectName}`))
    ) ?? request.scene.contextObjects[0];

  if (
    primaryAsset &&
    pinch &&
    rightHand &&
    /\b(pinch|grab|hold|follow|drag)\b/.test(text)
  ) {
    actions.push({
      actionType: "follow",
      targetAssetId: primaryAsset.assetId,
      followTargetId: rightHand.objectId,
      anchor: { kind: "sequence_start", sequenceId: pinch.sequenceId },
      evidenceIds: [pinch.sequenceId],
      confidence: 0.92,
      reason: "The transcript asks the held asset to follow the hand.",
    });
  }

  if (
    primaryAsset &&
    open &&
    /\b(open|release|throw|launch|jump|go from my hand)\b/.test(text)
  ) {
    actions.push({
      actionType: "unfollow",
      targetAssetId: primaryAsset.assetId,
      anchor: { kind: "sequence_start", sequenceId: open.sequenceId },
      evidenceIds: [open.sequenceId],
      confidence: 0.91,
      reason: "The asset must stop following when the hand opens.",
    });

    const forceCandidate = request.forceVectorCandidates.find(
      (candidate) =>
        candidate.assetId === primaryAsset.assetId &&
        candidate.frame >= open.startFrame
    );
    if (forceCandidate) {
      actions.push({
        actionType: "applyForce",
        targetAssetId: primaryAsset.assetId,
        forceCandidateId: forceCandidate.candidateId,
        scale: 1,
        evidenceIds: [open.sequenceId, forceCandidate.candidateId],
        confidence: 0.9,
        reason: "The release intent selects Unity's force candidate.",
      });
    } else {
      unresolvedIntents.push({
        summary: "Release force is missing",
        reason:
          "The transcript implies a force action, but Unity supplied no matching force candidate.",
        evidenceIds: [open.sequenceId],
      });
    }
  }

  const showMatch = /\b(show|display|appear)\b/.test(text);
  if (showMatch && collision?.sequenceKind === "collision") {
    const collisionParticipants = new Set([
      collision.objectAId,
      collision.objectBId,
    ]);
    const showTarget =
      [...mentionedAssets]
        .reverse()
        .find((asset) => !collisionParticipants.has(asset.assetId)) ??
      mentionedAssets[mentionedAssets.length - 1];

    if (showTarget) {
      actions.push({
        actionType: "show",
        targetAssetId: showTarget.assetId,
        anchor: {
          kind: "sequence_start",
          sequenceId: collision.sequenceId,
        },
        evidenceIds: [collision.sequenceId],
        confidence: 0.9,
        reason: "The transcript requests this asset at the collision.",
      });
    }
  }

  return { actions, unresolvedIntents };
}

function buildMockStatePlan(
  context: WorkflowPlanningContext
): StateMergeOutput {
  const { request } = context;
  const sequences = request.timeline.sequences.filter(
    (sequence) => sequence.enabled
  );
  const followFrame = firstActionFrame(sequences, "follow");
  const releaseFrame = Math.min(
    firstActionFrame(sequences, "unfollow"),
    firstActionFrame(sequences, "applyForce")
  );
  const hitFrame =
    findSequence(
      sequences,
      (sequence) => sequence.sequenceKind === "collision"
    )?.startFrame ?? Number.POSITIVE_INFINITY;

  const labelled = request.statePlaceholders.map((placeholder) => ({
    placeholderId: placeholder.placeholderId,
    name:
      placeholder.frameStart >= hitFrame
        ? "hit"
        : placeholder.frameStart >= releaseFrame
          ? "release"
          : placeholder.frameStart >= followFrame
            ? "drag"
            : "idle",
  }));
  const groups: StateMergeOutput["groups"] = [];

  for (const item of labelled) {
    const current = groups[groups.length - 1];
    if (current?.name === item.name) {
      current.placeholderIds.push(item.placeholderId);
    } else {
      groups.push({
        placeholderIds: [item.placeholderId],
        name: item.name,
        rationale: `Adjacent timeline intervals represent the ${item.name} phase.`,
        confidence: 0.92,
      });
    }
  }

  return { groups };
}

function findSequence(
  sequences: WorkflowTimelineSequence[],
  predicate: (sequence: WorkflowTimelineSequence) => boolean
): WorkflowTimelineSequence | undefined {
  return sequences
    .filter((sequence) => sequence.enabled && predicate(sequence))
    .sort((a, b) => a.startFrame - b.startFrame)[0];
}

function firstActionFrame(
  sequences: WorkflowTimelineSequence[],
  actionType: "follow" | "unfollow" | "applyForce"
): number {
  return (
    findSequence(
      sequences,
      (sequence) =>
        sequence.sequenceKind === "action" &&
        sequence.actionType === actionType
    )?.startFrame ?? Number.POSITIVE_INFINITY
  );
}

function assetPhrases(asset: {
  assetName: string;
  displayName?: string;
  aliases: string[];
}): string[] {
  return [asset.assetName, asset.displayName ?? "", ...asset.aliases]
    .map(normalize)
    .filter(Boolean);
}

function normalize(value: string): string {
  return value
    .toLocaleLowerCase()
    .replace(/\(clone\)/g, "")
    .replace(/[^\p{L}\p{N}]+/gu, " ")
    .trim();
}
