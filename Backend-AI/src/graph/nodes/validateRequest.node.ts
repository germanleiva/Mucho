import type { AnalyzeRecordingState } from "../state.js";

export function validateRequest(
  state: AnalyzeRecordingState
): AnalyzeRecordingState {
  const { request } = state;

  for (const seq of request.sequences) {
    if (seq.length === 0) {
      state.warnings.push({
        code: "ZERO_LENGTH_SEQUENCE",
        message: `Sequence ${seq.sequenceId} has length 0. Mucho actions and triggers are expected to have meaningful length.`,
      });
    }
  }

  const assetNames = new Set(request.scene.assets.map((asset) => asset.assetName));

  for (const seq of request.sequences) {
    if (seq.sequenceKind === "existingAction" && !assetNames.has(seq.targetAssetName)) {
      state.warnings.push({
        code: "INVALID_ACTION_TARGET",
        message: `Existing action targets unknown asset "${seq.targetAssetName}". Expected one of: ${Array.from(assetNames).join(", ")}`,
      });
    }
  }

  for (const seq of request.sequences) {
    if (seq.startFrame + seq.length > request.recording.durationFrames) {
      state.warnings.push({
        code: "FRAME_OUT_OF_BOUNDS",
        message: `Sequence ${seq.sequenceId} extends beyond recording duration (${seq.startFrame + seq.length} > ${request.recording.durationFrames})`,
      });
    }
  }

  for (const obs of request.contextObservations) {
    if (obs.startFrame + obs.length > request.recording.durationFrames) {
      state.warnings.push({
        code: "FRAME_OUT_OF_BOUNDS",
        message: "Observation extends beyond recording duration",
      });
    }
  }

  return state;
}
