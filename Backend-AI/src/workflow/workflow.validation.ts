import type { WorkflowIterationRequest } from "./workflow.schema.js";
import { WorkflowSemanticError } from "./workflow.errors.js";

export function validateWorkflowSemantics(
  request: WorkflowIterationRequest
): void {
  const errors: string[] = [];
  const duration = request.recording.durationFrames;
  const assetIds = new Set(request.scene.assets.map((asset) => asset.assetId));
  const contextIds = new Set(
    request.scene.contextObjects.map((object) => object.objectId)
  );

  if (request.recording.audioStartFrame > duration) {
    errors.push("Audio start frame is outside the recording.");
  }

  addDuplicateErrors(
    request.scene.assets.map((asset) => asset.assetId),
    "asset ID",
    errors
  );
  addDuplicateErrors(
    request.scene.contextObjects.map((object) => object.objectId),
    "context object ID",
    errors
  );
  addDuplicateErrors(
    request.timeline.sequences.map((sequence) => sequence.sequenceId),
    "sequence ID",
    errors
  );
  addDuplicateErrors(
    request.statePlaceholders.map(
      (placeholder) => placeholder.placeholderId
    ),
    "placeholder ID",
    errors
  );
  addDuplicateErrors(
    request.forceVectorCandidates.map((candidate) => candidate.candidateId),
    "force candidate ID",
    errors
  );

  for (const id of assetIds) {
    if (contextIds.has(id)) {
      errors.push(`Scene ID ${id} is used by both an asset and context object.`);
    }
  }

  for (const sequence of request.timeline.sequences) {
    if (sequence.endFrame < sequence.startFrame) {
      errors.push(
        `Sequence ${sequence.sequenceId} ends before it starts.`
      );
    }
    if (sequence.endFrame > duration) {
      errors.push(
        `Sequence ${sequence.sequenceId} ends outside the recording.`
      );
    }

    if (sequence.sequenceKind === "collision") {
      if (!assetIds.has(sequence.objectAId) && !contextIds.has(sequence.objectAId)) {
        errors.push(
          `Collision ${sequence.sequenceId} references unknown object ${sequence.objectAId}.`
        );
      }
      if (!assetIds.has(sequence.objectBId) && !contextIds.has(sequence.objectBId)) {
        errors.push(
          `Collision ${sequence.sequenceId} references unknown object ${sequence.objectBId}.`
        );
      }
    }

    if (
      sequence.sequenceKind === "action" &&
      !assetIds.has(sequence.targetAssetId)
    ) {
      errors.push(
        `Action ${sequence.sequenceId} references unknown asset ${sequence.targetAssetId}.`
      );
    }
  }

  for (const candidate of request.forceVectorCandidates) {
    if (!assetIds.has(candidate.assetId)) {
      errors.push(
        `Force candidate ${candidate.candidateId} references unknown asset ${candidate.assetId}.`
      );
    }
    if (candidate.frame >= duration) {
      errors.push(
        `Force candidate ${candidate.candidateId} is outside the recording.`
      );
    }
    if (
      candidate.aimTargetAssetId &&
      !assetIds.has(candidate.aimTargetAssetId)
    ) {
      errors.push(
        `Force candidate ${candidate.candidateId} references unknown aim target ${candidate.aimTargetAssetId}.`
      );
    }
  }

  validatePlaceholders(request, errors);

  if (errors.length > 0) {
    throw new WorkflowSemanticError(
      "Workflow request failed semantic validation.",
      errors
    );
  }
}

function validatePlaceholders(
  request: WorkflowIterationRequest,
  errors: string[]
): void {
  const duration = request.recording.durationFrames;
  const placeholders = request.statePlaceholders;
  const expectedBoundaries = new Set<number>([0, duration]);

  for (const sequence of request.timeline.sequences) {
    if (!sequence.enabled) {
      continue;
    }
    expectedBoundaries.add(sequence.startFrame);
    expectedBoundaries.add(sequence.endFrame);
  }

  const actualBoundaries = new Set<number>();
  let expectedStart = 0;

  for (const placeholder of placeholders) {
    actualBoundaries.add(placeholder.frameStart);
    actualBoundaries.add(placeholder.frameEnd);

    if (placeholder.frameStart !== expectedStart) {
      errors.push(
        `Placeholder ${placeholder.placeholderId} must start at ${expectedStart}, not ${placeholder.frameStart}.`
      );
    }
    if (placeholder.frameEnd <= placeholder.frameStart) {
      errors.push(
        `Placeholder ${placeholder.placeholderId} must have a positive half-open range.`
      );
    }
    if (placeholder.frameEnd > duration) {
      errors.push(
        `Placeholder ${placeholder.placeholderId} ends outside the recording.`
      );
    }

    expectedStart = placeholder.frameEnd;
  }

  if (expectedStart !== duration) {
    errors.push(
      `State placeholders must cover the recording through frame ${duration}.`
    );
  }

  const missing = [...expectedBoundaries]
    .filter((boundary) => !actualBoundaries.has(boundary))
    .sort((a, b) => a - b);
  const unexpected = [...actualBoundaries]
    .filter((boundary) => !expectedBoundaries.has(boundary))
    .sort((a, b) => a - b);

  if (missing.length > 0) {
    errors.push(`State placeholders are missing boundaries: ${missing.join(", ")}.`);
  }
  if (unexpected.length > 0) {
    errors.push(
      `State placeholders contain unexpected boundaries: ${unexpected.join(", ")}.`
    );
  }
}

function addDuplicateErrors(
  values: string[],
  label: string,
  errors: string[]
): void {
  const seen = new Set<string>();
  const duplicates = new Set<string>();

  for (const value of values) {
    if (seen.has(value)) {
      duplicates.add(value);
    }
    seen.add(value);
  }

  for (const duplicate of duplicates) {
    errors.push(`Duplicate ${label}: ${duplicate}.`);
  }
}
