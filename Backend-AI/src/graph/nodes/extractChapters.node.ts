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
  const fragments = extractTranscriptChapterFragments(
    transcript,
    duration,
    request.recording.frameRate,
    request.recording.audioStartFrame
  );

  for (const fragment of fragments) {
    const { text, frameStart, frameEnd } = fragment;

    chapters.push({
      chapterId: uuidv4(),
      summary: text,
      frameStart,
      frameEnd,
      relatedSequenceIds: [],
      relatedObservationIndexes: [],
      confidence: 0.85,
    });
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

export type ChapterFragment = {
  text: string;
  frameStart: number;
  frameEnd: number;
};

type TimedFragment = {
  text: string;
  timeStartMs: number;
  timeEndMs: number;
};

const PAUSE_BOUNDARY_MS = 800;
const TOPIC_WORD_PATTERN = /[\p{L}\p{N}]+/gu;
const TOPIC_STOP_WORDS = new Set([
  "a",
  "an",
  "and",
  "at",
  "da",
  "de",
  "den",
  "det",
  "di",
  "e",
  "en",
  "et",
  "for",
  "fra",
  "gli",
  "i",
  "il",
  "in",
  "la",
  "le",
  "lo",
  "of",
  "og",
  "on",
  "per",
  "the",
  "til",
  "to",
  "un",
  "una",
]);

export function extractTranscriptChapterFragments(
  transcript: NonNullable<AnalyzeRecordingState["transcript"]>,
  durationFrames: number,
  frameRate: number,
  audioStartFrame: number
): ChapterFragment[] {
  const segments = (transcript.segments ?? [])
    .filter(
      (
        segment
      ): segment is {
        text: string;
        timeStartMs: number;
        timeEndMs: number;
      } =>
        segment.text.trim().length > 0 &&
        segment.timeStartMs !== undefined &&
        segment.timeEndMs !== undefined &&
        segment.timeEndMs >= segment.timeStartMs
    )
    .map((segment) => ({
      text: segment.text.trim(),
      timeStartMs: segment.timeStartMs,
      timeEndMs: segment.timeEndMs,
    }))
    .sort((a, b) => a.timeStartMs - b.timeStartMs);

  if (segments.length > 0) {
    return timedFragmentsToChapters(
      groupTimedFragments(segments),
      durationFrames,
      frameRate,
      audioStartFrame
    );
  }

  return distributeFragmentsByWordCount(
    splitTranscriptFragments(transcript.text),
    durationFrames
  );
}

function groupTimedFragments(segments: TimedFragment[]): TimedFragment[] {
  const groups: TimedFragment[] = [];

  for (const segment of segments) {
    const current = groups[groups.length - 1];
    if (!current) {
      groups.push({ ...segment });
      continue;
    }

    const pauseMs = segment.timeStartMs - current.timeEndMs;
    if (
      pauseMs >= PAUSE_BOUNDARY_MS ||
      hasTopicShift(current.text, segment.text)
    ) {
      groups.push({ ...segment });
      continue;
    }

    current.text = `${current.text} ${segment.text}`;
    current.timeEndMs = Math.max(current.timeEndMs, segment.timeEndMs);
  }

  return groups;
}

function hasTopicShift(currentText: string, nextText: string): boolean {
  const currentWords = getTopicWords(currentText);
  const nextWords = getTopicWords(nextText);

  if (currentWords.size < 2 || nextWords.size < 2) {
    return false;
  }

  for (const word of nextWords) {
    if (currentWords.has(word)) {
      return false;
    }
  }

  return true;
}

function getTopicWords(text: string): Set<string> {
  const words = text.toLocaleLowerCase().match(TOPIC_WORD_PATTERN) ?? [];
  return new Set(
    words.filter((word) => word.length > 1 && !TOPIC_STOP_WORDS.has(word))
  );
}

function timedFragmentsToChapters(
  fragments: TimedFragment[],
  durationFrames: number,
  frameRate: number,
  audioStartFrame: number
): ChapterFragment[] {
  let frameStart = 0;

  return fragments.map((fragment, index) => {
    const next = fragments[index + 1];
    const boundaryMs = next
      ? (fragment.timeEndMs + next.timeStartMs) / 2
      : Number.POSITIVE_INFINITY;
    const frameEnd = next
      ? Math.max(
          frameStart,
          Math.min(
            durationFrames,
            audioStartFrame + Math.round((boundaryMs * frameRate) / 1000)
          )
        )
      : durationFrames;
    const chapter = {
      text: fragment.text,
      frameStart,
      frameEnd,
    };
    frameStart = frameEnd;
    return chapter;
  });
}

function distributeFragmentsByWordCount(
  fragments: string[],
  durationFrames: number
): ChapterFragment[] {
  const weights = fragments.map(
    (fragment) => fragment.match(TOPIC_WORD_PATTERN)?.length ?? 1
  );
  const totalWeight = weights.reduce((total, weight) => total + weight, 0);
  let accumulatedWeight = 0;
  let frameStart = 0;

  return fragments.map((text, index) => {
    accumulatedWeight += weights[index];
    const frameEnd =
      index === fragments.length - 1
        ? durationFrames
        : Math.round((durationFrames * accumulatedWeight) / totalWeight);
    const chapter = { text, frameStart, frameEnd };
    frameStart = frameEnd;
    return chapter;
  });
}
