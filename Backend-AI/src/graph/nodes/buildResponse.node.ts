import type { AnalyzeRecordingState } from "../state.js";
import type { AnalyzeRecordingResponse } from "../../schemas/response.schema.js";

export function buildResponse(
  state: AnalyzeRecordingState
): AnalyzeRecordingResponse {
  const {
    transcript,
    chapters,
    actionsToRecord,
    unresolvedIntents,
    warnings,
    analysisId,
    status,
    request,
  } = state;

  return {
    analysisId,
    recordingId: request.recordingId,
    status,
    transcript: {
      text: transcript?.text || "",
      language: transcript?.language || "unknown",
      source: request.transcript
        ? "provided"
        : request.useMockTranscript
          ? "mock"
          : "transcribed",
    },
    chapters,
    actionsToRecord,
    unresolvedIntents,
    warnings,
  };
}
