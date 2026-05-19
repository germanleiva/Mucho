import { describe, expect, it } from "vitest";
import { analyzeRecording } from "../graph/analyzeRecording.graph.js";
import type { AnalyzeRecordingRequest } from "../schemas/request.schema.js";

function createBaseRequest(
  transcript: string,
  assets: AnalyzeRecordingRequest["scene"]["assets"],
  sequences: AnalyzeRecordingRequest["sequences"] = []
): AnalyzeRecordingRequest {
  return {
    recordingId: "test-recording",
    languageHint: "en",
    recording: {
      frameRate: 60,
      durationFrames: 420,
      audioStartFrame: 0,
      recordingMode: "voice_during_miming",
    },
    transcript: {
      text: transcript,
      language: "en",
      segments: [],
    },
    scene: {
      assets,
      contextObjects: [
        {
          objectName: "RIGHTHAND",
          displayName: "Right Hand",
          objectKind: "rightHand",
        },
      ],
    },
    sequences,
    contextObservations: [],
    options: {
      confidenceThreshold: 0.85,
      includeDebugChapters: true,
      allowDefaultTextHide: true,
    },
  };
}

function createProductionLikeRequest(): AnalyzeRecordingRequest {
  return createBaseRequest(
    "I grab the basketball with my right hand, then I throw it. When it hits the cube, show the hit text in green.",
    [
      {
        assetName: "Basketball(Clone)",
        displayName: "Basketball",
        assetKind: "basketball",
      },
      {
        assetName: "Cube(Clone)",
        displayName: "Cube",
        assetKind: "cube",
      },
      {
        assetName: "HitText(Clone)",
        displayName: "Hit Text",
        assetKind: "text",
      },
    ],
    [
      {
        sequenceId: "gesture-pinch",
        sequenceKind: "gesture",
        startFrame: 40,
        length: 55,
        rawGesture: "Gesture.RIGHTHANDPINCH",
        gestureLabel: "Pinch",
        hand: "right",
      },
      {
        sequenceId: "gesture-open",
        sequenceKind: "gesture",
        startFrame: 96,
        length: 34,
        rawGesture: "Gesture.RIGHTHANDOPEN",
        gestureLabel: "Open",
        hand: "right",
      },
      {
        sequenceId: "collision-ball-cube",
        sequenceKind: "collision",
        startFrame: 210,
        length: 8,
        objectAName: "Basketball(Clone)",
        objectBName: "Cube(Clone)",
      },
      {
        sequenceId: "existing-show-ball",
        sequenceKind: "existingAction",
        startFrame: 0,
        length: 420,
        actionType: "show",
        targetAssetName: "Basketball(Clone)",
        params: {},
        source: "default",
      },
      {
        sequenceId: "existing-show-cube",
        sequenceKind: "existingAction",
        startFrame: 0,
        length: 420,
        actionType: "show",
        targetAssetName: "Cube(Clone)",
        params: {},
        source: "default",
      },
      {
        sequenceId: "existing-show-text",
        sequenceKind: "existingAction",
        startFrame: 0,
        length: 420,
        actionType: "show",
        targetAssetName: "HitText(Clone)",
        params: {},
        source: "default",
      },
    ]
  );
}

describe("Mucho AI Backend - Action Generation", () => {
  it("generates follow, unfollow, and throw only for the active basketball", async () => {
    const response = await analyzeRecording(createProductionLikeRequest());

    expect(response.status).toBe("completed");
    expect(response.actionsToRecord).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          actionType: "follow",
          targetAssetName: "Basketball(Clone)",
          startFrame: 40,
          followTargetType: "RIGHTHAND",
        }),
        expect.objectContaining({
          actionType: "unfollow",
          targetAssetName: "Basketball(Clone)",
          startFrame: 96,
        }),
        expect.objectContaining({
          actionType: "throwAsset",
          targetAssetName: "Basketball(Clone)",
          startFrame: 96,
          forceVelocityMode: { x: 0, y: 0, z: 1 },
        }),
      ])
    );

    expect(
      response.actionsToRecord.filter((a) => a.targetAssetName === "Cube(Clone)")
    ).toEqual([]);
  });

  it("shows and colors hit text at the collision frame despite default show at frame 0", async () => {
    const response = await analyzeRecording(createProductionLikeRequest());

    expect(response.actionsToRecord).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          actionType: "show",
          targetAssetName: "HitText(Clone)",
          startFrame: 210,
        }),
        expect.objectContaining({
          actionType: "changeColor",
          targetAssetName: "HitText(Clone)",
          startFrame: 210,
          color: "green",
        }),
        expect.objectContaining({
          actionType: "hide",
          targetAssetName: "HitText(Clone)",
          startFrame: 0,
        }),
      ])
    );
  });

  it("normalizes grey to gray in generated color actions", async () => {
    const request = createBaseRequest(
      "Change the sphere to grey",
      [
        {
          assetName: "Sphere(Clone)",
          displayName: "Sphere",
          assetKind: "sphere",
        },
      ],
      [
        {
          sequenceId: "existing-show",
          sequenceKind: "existingAction",
          startFrame: 0,
          length: 200,
          actionType: "show",
          targetAssetName: "Sphere(Clone)",
        },
      ]
    );
    request.recording.durationFrames = 200;
    request.recording.frameRate = 30;

    const response = await analyzeRecording(request);
    const colorActions = response.actionsToRecord.filter(
      (a) => a.actionType === "changeColor"
    );

    expect(colorActions.length).toBe(1);
    const colorAction = colorActions[0];
    expect(colorAction).toEqual(
      expect.objectContaining({
        actionType: "changeColor",
        targetAssetName: "Sphere(Clone)",
        color: "gray",
        startFrame: 0,
      })
    );
  });

  it("does not auto-generate an ambiguous throw-it action without active asset", async () => {
    const request = createBaseRequest("Throw it", [
      {
        assetName: "Ball(Clone)",
        displayName: "Ball",
        assetKind: "sphere",
      },
    ]);

    const response = await analyzeRecording(request);

    expect(
      response.actionsToRecord.some((a) => a.actionType === "throwAsset")
    ).toBe(false);
    expect(response.unresolvedIntents.length).toBeGreaterThan(0);
    expect(response.unresolvedIntents[0].reason).toContain("throw intent");
  });

  it("filters exact duplicate existing actions but allows same action at a new frame", async () => {
    const request = createBaseRequest(
      "Show the ball. When the ball hits the cube, show the text",
      [
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
        {
          assetName: "Text(Clone)",
          displayName: "Text",
          assetKind: "text",
        },
      ],
      [
        {
          sequenceId: "collision",
          sequenceKind: "collision",
          startFrame: 200,
          length: 10,
          objectAName: "Ball(Clone)",
          objectBName: "Cube(Clone)",
        },
        {
          sequenceId: "existing-show-ball",
          sequenceKind: "existingAction",
          startFrame: 0,
          length: 420,
          actionType: "show",
          targetAssetName: "Ball(Clone)",
          source: "default",
        },
        {
          sequenceId: "existing-show-text",
          sequenceKind: "existingAction",
          startFrame: 0,
          length: 420,
          actionType: "show",
          targetAssetName: "Text(Clone)",
          source: "default",
        },
      ]
    );

    const response = await analyzeRecording(request);

    expect(
      response.actionsToRecord.some(
        (a) => a.actionType === "show" && a.targetAssetName === "Ball(Clone)"
      )
    ).toBe(false);
    expect(
      response.actionsToRecord.some(
        (a) =>
          a.actionType === "show" &&
          a.targetAssetName === "Text(Clone)" &&
          a.startFrame === 200
      )
    ).toBe(true);
  });
});
