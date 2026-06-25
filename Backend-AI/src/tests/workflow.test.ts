import { describe, expect, it } from "vitest";
import { iterateWorkflow } from "../workflow/iterate-workflow.js";
import {
  MockStateMergeProvider,
  StaticActionPlanningProvider,
  StaticStateMergeProvider,
} from "../workflow/mock-workflow.providers.js";
import {
  WorkflowIterationRequestSchema,
  type WorkflowIterationRequest,
} from "../workflow/workflow.schema.js";
import { WorkflowSemanticError } from "../workflow/workflow.errors.js";

function createRequest(
  overrides: Partial<WorkflowIterationRequest> = {}
): WorkflowIterationRequest {
  const base = WorkflowIterationRequestSchema.parse({
    workflowId: "workflow-test",
    iteration: 0,
    recording: {
      frameRate: 30,
      durationFrames: 100,
      audioStartFrame: 0,
    },
    transcript: {
      text: "Pinch the ball so it follows my hand.",
      language: "en",
      segments: [],
    },
    scene: {
      assets: [
        {
          assetId: "ball",
          assetName: "Ball(Clone)",
          displayName: "Ball",
          assetKind: "sphere",
          capabilities: [
            "show",
            "hide",
            "changeColor",
            "follow",
            "unfollow",
            "applyForce",
          ],
        },
        {
          assetId: "lamp",
          assetName: "Lamp(Clone)",
          displayName: "Lamp",
          assetKind: "lamp",
          capabilities: ["show", "hide", "changeColor"],
        },
        {
          assetId: "text",
          assetName: "HitText(Clone)",
          displayName: "Hit text",
          assetKind: "text",
          capabilities: ["show", "hide", "changeColor"],
        },
      ],
      contextObjects: [
        {
          objectId: "right-hand",
          objectName: "RIGHTHAND",
          displayName: "Right Hand",
          objectKind: "rightHand",
        },
      ],
    },
    timeline: {
      sequences: [
        {
          sequenceId: "pinch",
          sequenceKind: "gesture",
          enabled: true,
          startFrame: 20,
          endFrame: 40,
          gestureLabel: "Pinch",
          hand: "right",
        },
      ],
    },
    statePlaceholders: [
      { placeholderId: "p0", frameStart: 0, frameEnd: 20 },
      { placeholderId: "p1", frameStart: 20, frameEnd: 40 },
      { placeholderId: "p2", frameStart: 40, frameEnd: 100 },
    ],
    forceVectorCandidates: [],
    options: {},
  });

  return WorkflowIterationRequestSchema.parse({
    ...base,
    ...overrides,
  });
}

const completeStateProvider = new StaticStateMergeProvider({
  groups: [
    {
      placeholderIds: ["p0"],
      name: "idle",
      rationale: "Before interaction.",
      confidence: 0.95,
    },
    {
      placeholderIds: ["p1", "p2"],
      name: "drag",
      rationale: "The ball follows the hand.",
      confidence: 0.93,
    },
  ],
});

describe("iterative timeline workflow", () => {
  it("proposes an action, then returns the final state plan from an updated snapshot", async () => {
    const firstRequest = createRequest();
    const first = await iterateWorkflow(firstRequest, {
      actionProvider: new StaticActionPlanningProvider({
        actions: [
          {
            actionType: "follow",
            targetAssetId: "ball",
            followTargetId: "right-hand",
            anchor: { kind: "sequence_start", sequenceId: "pinch" },
            evidenceIds: ["pinch"],
            confidence: 0.95,
            reason: "The ball should follow after the pinch.",
          },
        ],
        unresolvedIntents: [],
      }),
      stateProvider: completeStateProvider,
    });

    expect(first.workflowStatus).toBe("continue");
    expect(first.nextStep).toBe("apply_actions");
    expect(first.timelinePatch.operations[0].action).toEqual(
      expect.objectContaining({
        actionType: "follow",
        targetAssetId: "ball",
        startFrame: 20,
        requiresResimulation: false,
      })
    );

    const secondRequest = createRequest({
      iteration: 1,
      timeline: {
        sequences: [
          ...firstRequest.timeline.sequences,
          {
            sequenceId: "ai-follow",
            sequenceKind: "action",
            enabled: true,
            startFrame: 20,
            endFrame: 20,
            actionType: "follow",
            targetAssetId: "ball",
            params: { followTargetId: "right-hand" },
            source: "ai",
          },
        ],
      },
    });
    const second = await iterateWorkflow(secondRequest, {
      actionProvider: new StaticActionPlanningProvider({
        actions: [],
        unresolvedIntents: [],
      }),
      stateProvider: completeStateProvider,
    });

    expect(second.workflowStatus).toBe("completed");
    expect(second.actionsComplete).toBe(true);
    expect(second.nextStep).toBe("apply_state_plan");
    expect(second.statePlan?.states.map((state) => state.name)).toEqual([
      "idle",
      "drag",
    ]);
  });

  it("marks applyForce for resimulation and can use a resulting collision next", async () => {
    const request = createForceRequest();
    const forceResponse = await iterateWorkflow(request, {
      actionProvider: new StaticActionPlanningProvider({
        actions: [
          {
            actionType: "applyForce",
            targetAssetId: "ball",
            forceCandidateId: "force-1",
            scale: 1.5,
            evidenceIds: ["open", "force-1"],
            confidence: 0.92,
            reason: "Release the ball toward the lamp.",
          },
        ],
        unresolvedIntents: [],
      }),
      stateProvider: completeStateProvider,
    });

    expect(forceResponse.nextStep).toBe("apply_actions_and_resimulate");
    expect(forceResponse.timelinePatch.operations[0].action.params).toEqual({
      forceCandidateId: "force-1",
      scale: 1.5,
      initialVelocity: { x: 0, y: 0, z: 6 },
    });

    const updated = createForceRequest({
      iteration: 1,
      timeline: {
        sequences: [
          ...request.timeline.sequences,
          {
            sequenceId: "ai-force",
            sequenceKind: "action",
            enabled: true,
            startFrame: 40,
            endFrame: 40,
            actionType: "applyForce",
            targetAssetId: "ball",
            params: forceResponse.timelinePatch.operations[0].action.params,
            source: "ai",
          },
          {
            sequenceId: "ball-lamp-hit",
            sequenceKind: "collision",
            enabled: true,
            startFrame: 80,
            endFrame: 90,
            objectAId: "ball",
            objectBId: "lamp",
          },
        ],
      },
      statePlaceholders: [
        { placeholderId: "p0", frameStart: 0, frameEnd: 20 },
        { placeholderId: "p1", frameStart: 20, frameEnd: 40 },
        { placeholderId: "p2", frameStart: 40, frameEnd: 50 },
        { placeholderId: "p3", frameStart: 50, frameEnd: 80 },
        { placeholderId: "p4", frameStart: 80, frameEnd: 90 },
        { placeholderId: "p5", frameStart: 90, frameEnd: 120 },
      ],
    });
    const collisionResponse = await iterateWorkflow(updated, {
      actionProvider: new StaticActionPlanningProvider({
        actions: [
          {
            actionType: "show",
            targetAssetId: "text",
            anchor: {
              kind: "sequence_start",
              sequenceId: "ball-lamp-hit",
            },
            evidenceIds: ["ball-lamp-hit"],
            confidence: 0.91,
            reason: "Show feedback when the ball hits the lamp.",
          },
        ],
        unresolvedIntents: [],
      }),
      stateProvider: completeStateProvider,
    });

    expect(collisionResponse.nextStep).toBe("apply_actions");
    expect(collisionResponse.timelinePatch.operations[0].action.startFrame).toBe(
      80
    );
  });

  it("merges the example into idle, drag, release, and hit states", async () => {
    const request = createStateExampleRequest();
    const response = await iterateWorkflow(request, {
      actionProvider: new StaticActionPlanningProvider({
        actions: [],
        unresolvedIntents: [],
      }),
      stateProvider: new MockStateMergeProvider(),
    });

    expect(response.workflowStatus).toBe("completed");
    expect(response.statePlan?.states.map((state) => state.name)).toEqual([
      "idle",
      "drag",
      "release",
      "hit",
    ]);
    expect(response.statePlan?.states.map((state) => [
      state.frameStart,
      state.frameEnd,
    ])).toEqual([
      [0, 20],
      [20, 50],
      [50, 80],
      [80, 100],
    ]);
  });

  it("does not return an action already present in the timeline", async () => {
    const request = createRequest({
      timeline: {
        sequences: [
          {
            sequenceId: "pinch",
            sequenceKind: "gesture",
            enabled: true,
            startFrame: 20,
            endFrame: 40,
            gestureLabel: "Pinch",
            hand: "right",
          },
          {
            sequenceId: "existing-follow",
            sequenceKind: "action",
            enabled: true,
            startFrame: 20,
            endFrame: 20,
            actionType: "follow",
            targetAssetId: "ball",
            params: { followTargetId: "right-hand" },
            source: "ai",
          },
        ],
      },
    });
    const response = await iterateWorkflow(request, {
      actionProvider: new StaticActionPlanningProvider({
        actions: [
          {
            actionType: "follow",
            targetAssetId: "ball",
            followTargetId: "right-hand",
            anchor: { kind: "sequence_start", sequenceId: "pinch" },
            evidenceIds: ["pinch"],
            confidence: 0.95,
            reason: "Already applied.",
          },
        ],
        unresolvedIntents: [],
      }),
      stateProvider: completeStateProvider,
    });

    expect(response.timelinePatch.operations).toEqual([]);
    expect(response.workflowStatus).toBe("completed");
    expect(response.warnings).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ code: "DUPLICATE_ACTION" }),
      ])
    );
  });

  it("rejects incomplete placeholder boundaries", async () => {
    const request = createRequest({
      statePlaceholders: [
        { placeholderId: "p0", frameStart: 0, frameEnd: 40 },
        { placeholderId: "p1", frameStart: 40, frameEnd: 100 },
      ],
    });

    await expect(
      iterateWorkflow(request, {
        actionProvider: new StaticActionPlanningProvider({
          actions: [],
          unresolvedIntents: [],
        }),
        stateProvider: completeStateProvider,
      })
    ).rejects.toBeInstanceOf(WorkflowSemanticError);
  });

  it("requires review when state groups skip or reorder placeholders", async () => {
    const response = await iterateWorkflow(createRequest(), {
      actionProvider: new StaticActionPlanningProvider({
        actions: [],
        unresolvedIntents: [],
      }),
      stateProvider: new StaticStateMergeProvider({
        groups: [
          {
            placeholderIds: ["p1", "p0"],
            name: "bad order",
            rationale: "Invalid on purpose.",
            confidence: 0.95,
          },
        ],
      }),
    });

    expect(response.workflowStatus).toBe("needs_review");
    expect(response.nextStep).toBe("review");
    expect(response.warnings[0].code).toBe("INVALID_STATE_PLAN");
  });

  it("pauses on low-confidence actions", async () => {
    const response = await iterateWorkflow(createRequest(), {
      actionProvider: new StaticActionPlanningProvider({
        actions: [
          {
            actionType: "follow",
            targetAssetId: "ball",
            followTargetId: "right-hand",
            anchor: { kind: "sequence_start", sequenceId: "pinch" },
            evidenceIds: ["pinch"],
            confidence: 0.4,
            reason: "The target is uncertain.",
          },
        ],
        unresolvedIntents: [],
      }),
      stateProvider: completeStateProvider,
    });

    expect(response.workflowStatus).toBe("needs_review");
    expect(response.actionsComplete).toBe(false);
    expect(response.timelinePatch.operations).toEqual([]);
  });

  it("pauses on a low-confidence state plan", async () => {
    const response = await iterateWorkflow(createRequest(), {
      actionProvider: new StaticActionPlanningProvider({
        actions: [],
        unresolvedIntents: [],
      }),
      stateProvider: new StaticStateMergeProvider({
        groups: [
          {
            placeholderIds: ["p0", "p1", "p2"],
            name: "interaction",
            rationale: "The phases may be equivalent.",
            confidence: 0.5,
          },
        ],
      }),
    });

    expect(response.workflowStatus).toBe("needs_review");
    expect(response.actionsComplete).toBe(true);
    expect(response.statePlan).not.toBeNull();
  });

  it("prevents another patch after the maximum action iteration", async () => {
    const response = await iterateWorkflow(createRequest({ iteration: 6 }), {
      actionProvider: new StaticActionPlanningProvider({
        actions: [
          {
            actionType: "follow",
            targetAssetId: "ball",
            followTargetId: "right-hand",
            anchor: { kind: "sequence_start", sequenceId: "pinch" },
            evidenceIds: ["pinch"],
            confidence: 0.95,
            reason: "Still trying to add the action.",
          },
        ],
        unresolvedIntents: [],
      }),
      stateProvider: completeStateProvider,
    });

    expect(response.workflowStatus).toBe("needs_review");
    expect(response.timelinePatch.operations).toEqual([]);
    expect(response.unresolvedIntents[0].summary).toContain("Maximum");
  });
});

function createForceRequest(
  overrides: Partial<WorkflowIterationRequest> = {}
): WorkflowIterationRequest {
  return createRequest({
    recording: {
      frameRate: 30,
      durationFrames: 120,
      audioStartFrame: 0,
    },
    transcript: {
      text: "Pinch the ball, then open my hand and throw it at the lamp.",
      language: "en",
      segments: [],
    },
    timeline: {
      sequences: [
        {
          sequenceId: "pinch",
          sequenceKind: "gesture",
          enabled: true,
          startFrame: 20,
          endFrame: 40,
          gestureLabel: "Pinch",
          hand: "right",
        },
        {
          sequenceId: "open",
          sequenceKind: "gesture",
          enabled: true,
          startFrame: 40,
          endFrame: 50,
          gestureLabel: "Open",
          hand: "right",
        },
      ],
    },
    statePlaceholders: [
      { placeholderId: "p0", frameStart: 0, frameEnd: 20 },
      { placeholderId: "p1", frameStart: 20, frameEnd: 40 },
      { placeholderId: "p2", frameStart: 40, frameEnd: 50 },
      { placeholderId: "p3", frameStart: 50, frameEnd: 120 },
    ],
    forceVectorCandidates: [
      {
        candidateId: "force-1",
        assetId: "ball",
        frame: 40,
        initialVelocity: { x: 0, y: 0, z: 4 },
        source: "hand_velocity",
        aimTargetAssetId: "lamp",
      },
    ],
    ...overrides,
  });
}

function createStateExampleRequest(): WorkflowIterationRequest {
  return createRequest({
    transcript: {
      text: "Pinch the ball so it follows my hand. Open my hand to release it. When it hits the lamp, finish.",
      language: "en",
      segments: [],
    },
    timeline: {
      sequences: [
        {
          sequenceId: "follow",
          sequenceKind: "action",
          enabled: true,
          startFrame: 20,
          endFrame: 20,
          actionType: "follow",
          targetAssetId: "ball",
          params: { followTargetId: "right-hand" },
          source: "ai",
        },
        {
          sequenceId: "unfollow",
          sequenceKind: "action",
          enabled: true,
          startFrame: 50,
          endFrame: 50,
          actionType: "unfollow",
          targetAssetId: "ball",
          params: {},
          source: "ai",
        },
        {
          sequenceId: "force",
          sequenceKind: "action",
          enabled: true,
          startFrame: 50,
          endFrame: 50,
          actionType: "applyForce",
          targetAssetId: "ball",
          params: {
            forceCandidateId: "force-1",
            scale: 1,
            initialVelocity: { x: 0, y: 0, z: 4 },
          },
          source: "ai",
        },
        {
          sequenceId: "hit",
          sequenceKind: "collision",
          enabled: true,
          startFrame: 80,
          endFrame: 90,
          objectAId: "ball",
          objectBId: "lamp",
        },
      ],
    },
    statePlaceholders: [
      { placeholderId: "p0", frameStart: 0, frameEnd: 20 },
      { placeholderId: "p1", frameStart: 20, frameEnd: 50 },
      { placeholderId: "p2", frameStart: 50, frameEnd: 80 },
      { placeholderId: "p3", frameStart: 80, frameEnd: 90 },
      { placeholderId: "p4", frameStart: 90, frameEnd: 100 },
    ],
    forceVectorCandidates: [
      {
        candidateId: "force-1",
        assetId: "ball",
        frame: 50,
        initialVelocity: { x: 0, y: 0, z: 4 },
        source: "hand_velocity",
        aimTargetAssetId: "lamp",
      },
    ],
  });
}
