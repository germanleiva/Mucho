import { describe, expect, it } from "vitest";
import { zodTextFormat } from "openai/helpers/zod";
import { buildActionPlanningContext } from "../workflow/action-planning.context.js";
import { iterateWorkflow } from "../workflow/iterate-workflow.js";
import {
  StaticActionPlanningProvider,
  StaticStateMergeProvider,
} from "../workflow/mock-workflow.providers.js";
import {
  ACTION_PROMPT_FEW_SHOT_SCENARIOS,
  ACTION_PROMPT_VERSION,
  ACTION_SYSTEM_PROMPT,
} from "../prompts/action-planning.prompt.js";
import { ActionPlanningOutputSchema } from "../workflow/workflow.providers.js";
import {
  WorkflowIterationRequestSchema,
  type WorkflowIterationRequest,
} from "../workflow/workflow.schema.js";

describe("action planning prompt v1", () => {
  it("normalizes enabled action evidence without state placeholders", () => {
    const request = createRequest({
      timeline: {
        sequences: [
          {
            sequenceId: "pinch",
            sequenceKind: "gesture",
            enabled: true,
            startFrame: 10,
            endFrame: 20,
            gestureLabel: "Pinch",
            hand: "right",
          },
          {
            sequenceId: "say-red",
            sequenceKind: "voice",
            enabled: true,
            startFrame: 30,
            endFrame: 35,
            command: "red",
          },
          {
            sequenceId: "collision",
            sequenceKind: "collision",
            enabled: true,
            startFrame: 50,
            endFrame: 55,
            objectAId: "ball",
            objectBId: "lamp",
          },
          {
            sequenceId: "existing-follow",
            sequenceKind: "action",
            enabled: true,
            startFrame: 10,
            endFrame: 10,
            actionType: "follow",
            targetAssetId: "ball",
            params: { followTargetId: "right-hand" },
            source: "ai",
          },
          {
            sequenceId: "disabled-open",
            sequenceKind: "gesture",
            enabled: false,
            startFrame: 70,
            endFrame: 80,
            gestureLabel: "Open",
            hand: "right",
          },
        ],
      },
    });

    const normalized = buildActionPlanningContext({
      request,
      chapters: [
        {
          chapterId: "chapter-1",
          summary: "Pinch the ball.",
          frameStart: 0,
          frameEnd: 50,
          relatedSequenceIds: ["pinch"],
          confidence: 0.9,
        },
      ],
    });

    expect(normalized.promptVersion).toBe(ACTION_PROMPT_VERSION);
    expect(normalized).not.toHaveProperty("statePlaceholders");
    expect(normalized.timeline.gestures.map((item) => item.sequenceId)).toEqual([
      "pinch",
    ]);
    expect(
      normalized.timeline.voiceCommands.map((item) => item.sequenceId)
    ).toEqual(["say-red"]);
    expect(
      normalized.timeline.collisions.map((item) => item.sequenceId)
    ).toEqual(["collision"]);
    expect(
      normalized.timeline.existingActions.map((item) => item.sequenceId)
    ).toEqual(["existing-follow"]);
    expect(JSON.stringify(normalized)).not.toContain("disabled-open");
  });

  it("keeps all few-shot outputs structurally valid and grounded", () => {
    const examples = ACTION_PROMPT_FEW_SHOT_SCENARIOS.flatMap(
      (scenario) => scenario.examples
    );

    for (const example of examples) {
      expect(
        ActionPlanningOutputSchema.safeParse(example.output).success,
        example.name
      ).toBe(true);
    }

    const voiceExample = examples.find(
      (example) => example.name === "gesture and voice anchors"
    );
    const beforeCollision = examples.find(
      (example) => example.name === "before collision evidence"
    );
    const afterCollision = examples.find(
      (example) => example.name === "after collision evidence"
    );

    expect(voiceExample?.expectedAnchors).toContain(
      "sequence_start:say-red"
    );
    expect(beforeCollision?.expectedActionTypes).toEqual([
      "follow",
      "unfollow",
      "applyForce",
    ]);
    expect(beforeCollision?.output.actions).not.toEqual(
      expect.arrayContaining([
        expect.objectContaining({ actionType: "show" }),
      ])
    );
    expect(afterCollision?.expectedActionTypes).toEqual([
      "hide",
      "show",
      "changeColor",
    ]);
    expect(afterCollision?.output.actions).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          actionType: "hide",
          anchor: { kind: "recording_start" },
        }),
        expect.objectContaining({
          actionType: "show",
          anchor: {
            kind: "sequence_start",
            sequenceId: "ball-hits-lamp",
          },
        }),
      ])
    );
    expect(ACTION_SYSTEM_PROMPT).toContain(
      '"When I say X" requires a matching voice sequence.'
    );
    expect(ACTION_SYSTEM_PROMPT).toContain(
      "Do not propose collision-dependent actions until that collision exists."
    );
  });

  it("generates an OpenAI-compatible response schema", () => {
    const format = zodTextFormat(
      ActionPlanningOutputSchema,
      "mucho_action_plan_test"
    );

    expect(findRefSiblings(format)).toEqual([]);
    expect(JSON.stringify(format)).not.toContain('"default"');
  });

  it("rejects proposals for assets with unknown capabilities", async () => {
    const response = await iterateWorkflow(
      createRequest({
        scene: {
          assets: [
            {
              assetId: "ball",
              assetName: "Ball",
              assetKind: "sphere",
              aliases: [],
              capabilities: [],
            },
          ],
          contextObjects: [
            {
              objectId: "right-hand",
              objectName: "RIGHTHAND",
              objectKind: "rightHand",
              aliases: [],
            },
          ],
        },
      }),
      {
        actionProvider: new StaticActionPlanningProvider({
          actions: [
            {
              actionType: "follow",
              targetAssetId: "ball",
              followTargetId: "right-hand",
              anchor: { kind: "sequence_start", sequenceId: "pinch" },
              evidenceIds: ["pinch"],
              confidence: 0.98,
              reason: "The pinch requests follow.",
            },
          ],
          unresolvedIntents: [],
        }),
        stateProvider: new StaticStateMergeProvider({ groups: [] }),
      }
    );

    expect(response.workflowStatus).toBe("needs_review");
    expect(response.timelinePatch.operations).toEqual([]);
    expect(response.warnings).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          code: "INVALID_ACTION_PROPOSAL",
          message: expect.stringContaining("does not support follow"),
        }),
      ])
    );
  });
});

function findRefSiblings(
  value: unknown,
  path = "$"
): Array<{ path: string; siblings: string[] }> {
  if (Array.isArray(value)) {
    return value.flatMap((item, index) =>
      findRefSiblings(item, `${path}[${index}]`)
    );
  }
  if (!value || typeof value !== "object") {
    return [];
  }

  const record = value as Record<string, unknown>;
  const siblings =
    "$ref" in record
      ? Object.keys(record).filter((key) => key !== "$ref")
      : [];

  return [
    ...(siblings.length > 0 ? [{ path, siblings }] : []),
    ...Object.entries(record).flatMap(([key, item]) =>
      findRefSiblings(item, `${path}.${key}`)
    ),
  ];
}

function createRequest(
  overrides: Partial<WorkflowIterationRequest> = {}
): WorkflowIterationRequest {
  const base = WorkflowIterationRequestSchema.parse({
    workflowId: "prompt-test",
    iteration: 0,
    recording: {
      frameRate: 30,
      durationFrames: 100,
      audioStartFrame: 0,
    },
    transcript: {
      text: "When I pinch the ball, make it follow my hand.",
      language: "en",
      segments: [],
    },
    scene: {
      assets: [
        {
          assetId: "ball",
          assetName: "Ball",
          assetKind: "sphere",
          aliases: [],
          capabilities: ["follow"],
        },
        {
          assetId: "lamp",
          assetName: "Lamp",
          assetKind: "lamp",
          aliases: [],
          capabilities: [],
        },
      ],
      contextObjects: [
        {
          objectId: "right-hand",
          objectName: "RIGHTHAND",
          objectKind: "rightHand",
          aliases: [],
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
