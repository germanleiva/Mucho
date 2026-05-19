import type {
  AnalyzeRecordingRequest,
  Transcript,
} from "../schemas/request.schema.js";
import type {
  DebugChapter,
  UnresolvedIntent,
  Warning,
} from "../schemas/response.schema.js";
import type { ActionToRecord } from "../schemas/action.schema.js";

export interface AnalyzeRecordingState {
  request: AnalyzeRecordingRequest;
  transcript: Transcript | null;
  chapters: DebugChapter[];
  actionCandidates: ActionToRecord[];
  actionsToRecord: ActionToRecord[];
  unresolvedIntents: UnresolvedIntent[];
  warnings: Warning[];
  errors: string[];
  analysisId: string;
  status: "completed" | "partial" | "failed";
}

export function initializeState(
  request: AnalyzeRecordingRequest,
  analysisId: string
): AnalyzeRecordingState {
  return {
    request,
    transcript: null,
    chapters: [],
    actionCandidates: [],
    actionsToRecord: [],
    unresolvedIntents: [],
    warnings: [],
    errors: [],
    analysisId,
    status: "completed",
  };
}
