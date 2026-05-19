import type { AnalyzeRecordingState } from "../state.js";

const MOCK_TRANSCRIPTS: Record<string, string> = {
  default:
    "I pinch the ball and it follows my right hand. When I open my hand, throw the ball. When it hits the cube, show the hit text.",
  simple: "Show the sphere when I grab it.",
  multipart:
    "The ball should follow my hand. When I release, throw it. If it hits the cube, make the text appear.",
};

export function obtainTranscript(
  state: AnalyzeRecordingState
): AnalyzeRecordingState {
  const { request } = state;

  if (request.transcript && request.transcript.text) {
    state.transcript = request.transcript;
    return state;
  }

  if (request.useMockTranscript) {
    state.transcript = {
      text: MOCK_TRANSCRIPTS.default,
      language: "en",
      segments: [],
    };
    return state;
  }

  state.errors.push(
    "No transcript provided and useMockTranscript is false. Please provide a transcript or set useMockTranscript to true."
  );
  state.status = "failed";
  return state;
}
