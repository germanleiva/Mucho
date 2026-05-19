import { v4 as uuidv4 } from "uuid";
import type { AnalyzeRecordingRequest } from "../schemas/request.schema.js";
import type { AnalyzeRecordingResponse } from "../schemas/response.schema.js";
import { initializeState } from "./state.js";
import { validateRequest } from "./nodes/validateRequest.node.js";
import { obtainTranscript } from "./nodes/obtainTranscript.node.js";
import { extractChapters } from "./nodes/extractChapters.node.js";
import { generateActionCandidates } from "./nodes/generateActionCandidates.node.js";
import { validateAndFilterActions } from "./nodes/validateAndFilterActions.node.js";
import { buildResponse } from "./nodes/buildResponse.node.js";

export async function analyzeRecording(
  request: AnalyzeRecordingRequest
): Promise<AnalyzeRecordingResponse> {
  const analysisId = uuidv4();
  let state = initializeState(request, analysisId);

  try {
    state = validateRequest(state);
    state = obtainTranscript(state);

    if (state.errors.length > 0) {
      state.status = "failed";
      return buildResponse(state);
    }

    state = extractChapters(state);
    state = generateActionCandidates(state);
    state = validateAndFilterActions(state);

    return buildResponse(state);
  } catch (error) {
    state.errors.push(
      `Pipeline error: ${error instanceof Error ? error.message : String(error)}`
    );
    state.status = "failed";
    return buildResponse(state);
  }
}
