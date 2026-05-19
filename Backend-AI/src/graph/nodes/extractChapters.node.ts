import { v4 as uuidv4 } from "uuid";
import type { AnalyzeRecordingState } from "../state.js";

export function extractChapters(
  state: AnalyzeRecordingState
): AnalyzeRecordingState {
  const { request, transcript } = state;

  if (!transcript) {
    state.warnings.push({
      code: "NO_TRANSCRIPT",
      message: "Cannot extract chapters without transcript",
    });
    return state;
  }

  const chapters: typeof state.chapters = [];
  const duration = request.recording.durationFrames;
  const sentences = splitTranscriptFragments(transcript.text);
  const framesPerSentence =
    sentences.length > 0 ? Math.floor(duration / sentences.length) : duration;

  let framePosition = 0;

  for (const sentence of sentences) {
    const frameStart = framePosition;
    const frameEnd = Math.min(frameStart + framesPerSentence, duration);

    chapters.push({
      chapterId: uuidv4(),
      summary: sentence.trim(),
      frameStart,
      frameEnd,
      relatedSequenceIds: [],
      relatedObservationIndexes: [],
      confidence: 0.85,
    });

    framePosition = frameEnd;
  }

  for (const chapter of chapters) {
    for (const seq of request.sequences) {
      if (
        seq.startFrame < chapter.frameEnd &&
        seq.startFrame + seq.length > chapter.frameStart
      ) {
        chapter.relatedSequenceIds.push(seq.sequenceId);
      }
    }

    for (let obsIdx = 0; obsIdx < request.contextObservations.length; obsIdx++) {
      const obs = request.contextObservations[obsIdx];
      if (
        obs.startFrame < chapter.frameEnd &&
        obs.startFrame + obs.length > chapter.frameStart
      ) {
        chapter.relatedObservationIndexes.push(obsIdx);
      }
    }
  }

  state.chapters = chapters;
  return state;
}

function splitTranscriptFragments(text: string): string[] {
  return text
    .split(/[.!?]+|\r?\n+/)
    .map((fragment) => fragment.trim())
    .filter((fragment) => fragment.length > 0);
}
