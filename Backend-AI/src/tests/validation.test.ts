import { describe, expect, it } from "vitest";
import { analyzeRecording } from "../graph/analyzeRecording.graph.js";
import type { AnalyzeRecordingRequest } from "../schemas/request.schema.js";

function minimalRequest(
  transcriptText = "Show ball"
): AnalyzeRecordingRequest {
  return {
    recordingId: "validation-test",
    recording: {
      frameRate: 30,
      durationFrames: 120,
      recordingMode: "voice_during_miming",
    },
    transcript: {
      text: transcriptText,
      language: "en",
    },
    scene: {
      assets: [
        {
          assetName: "Ball(Clone)",
          displayName: "Ball",
          assetKind: "sphere",
        },
        {
          assetName: "Cube(Clone)",
          displayName: "Cube",
          assetKind: "cube",
        },
      ],
      contextObjects: [],
    },
    sequences: [],
    contextObservations: [],
    options: {},
  };
}

describe("Mucho AI Backend - Validation and Chapters", () => {
  it("accepts a valid request", async () => {
    const response = await analyzeRecording(minimalRequest());

    expect(response.status).toBe("completed");
    expect(response.recordingId).toBe("validation-test");
    expect(response.chapters.length).toBeGreaterThan(0);
  });

  it("warns on zero-length sequences", async () => {
    const request = minimalRequest();
    request.sequences = [
      {
        sequenceId: "bad-seq",
        sequenceKind: "existingAction",
        startFrame: 0,
        length: 0,
        actionType: "show",
        targetAssetName: "Ball(Clone)",
      },
    ];

    const response = await analyzeRecording(request);

    expect(response.warnings).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          code: "ZERO_LENGTH_SEQUENCE",
          message:
            "Sequence bad-seq has length 0. Mucho actions and triggers are expected to have meaningful length.",
        }),
      ])
    );
  });

  it("uses provided transcript over mock", async () => {
    const request = minimalRequest("Custom transcript text");
    request.useMockTranscript = true;

    const response = await analyzeRecording(request);

    expect(response.transcript.text).toBe("Custom transcript text");
    expect(response.transcript.source).toBe("provided");
  });

  it("fails if no transcript and mock transcript is disabled", async () => {
    const request = minimalRequest();
    delete request.transcript;
    request.useMockTranscript = false;

    const response = await analyzeRecording(request);

    expect(response.status).toBe("failed");
  });

  it.each([
    ["Show ball. Hide cube", ["Show ball", "Hide cube"]],
    ["Show ball. Hide cube.", ["Show ball", "Hide cube"]],
    ["Show ball\nHide cube", ["Show ball", "Hide cube"]],
  ])("preserves chapter fragments for %s", async (transcriptText, expected) => {
    const response = await analyzeRecording(minimalRequest(transcriptText));

    expect(response.chapters.map((chapter) => chapter.summary)).toEqual(expected);
  });
});
