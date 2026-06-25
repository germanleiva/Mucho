import type { ActionPlanningOutput } from "../workflow/workflow.providers.js";

export const ACTION_PROMPT_VERSION = "action-v1" as const;

export type ActionPromptFewShotExample = {
  name: string;
  input: Record<string, unknown>;
  output: ActionPlanningOutput;
  expectedActionTypes: string[];
  expectedAnchors: string[];
};

export type ActionPromptFewShotScenario = {
  name: string;
  examples: ActionPromptFewShotExample[];
};

const pinchAndVoiceInput = {
  promptVersion: ACTION_PROMPT_VERSION,
  workflow: {
    workflowId: "few-shot-pinch-voice",
    iteration: 0,
    maxActionIterations: 6,
  },
  recording: {
    frameRate: 30,
    durationFrames: 120,
    audioStartFrame: 0,
  },
  transcript: {
    text: "When I pinch the ball, make it follow my hand. When I say red, turn the ball red.",
    language: "en",
    segments: [],
  },
  chapters: [],
  scene: {
    assets: [
      {
        assetId: "ball",
        assetName: "Ball",
        assetKind: "sphere",
        aliases: [],
        capabilities: ["follow", "changeColor"],
      },
    ],
    contextObjects: [
      {
        objectId: "right-hand",
        objectName: "RIGHTHAND",
        objectKind: "rightHand",
        aliases: ["my hand"],
      },
    ],
  },
  timeline: {
    gestures: [
      {
        sequenceId: "pinch-right",
        sequenceKind: "gesture",
        enabled: true,
        startFrame: 20,
        endFrame: 35,
        gestureLabel: "Pinch",
        hand: "right",
      },
    ],
    voiceCommands: [
      {
        sequenceId: "say-red",
        sequenceKind: "voice",
        enabled: true,
        startFrame: 60,
        endFrame: 72,
        command: "red",
      },
    ],
    collisions: [],
    existingActions: [],
  },
  forceCandidates: [],
  actionConfidenceThreshold: 0.85,
};

const pinchAndVoiceOutput: ActionPlanningOutput = {
  actions: [
    {
      actionType: "follow",
      targetAssetId: "ball",
      followTargetId: "right-hand",
      anchor: {
        kind: "sequence_start",
        sequenceId: "pinch-right",
      },
      evidenceIds: ["pinch-right"],
      confidence: 0.97,
      reason:
        "The transcript explicitly links the recorded pinch to the ball following the right hand.",
    },
    {
      actionType: "changeColor",
      targetAssetId: "ball",
      color: "red",
      anchor: {
        kind: "sequence_start",
        sequenceId: "say-red",
      },
      evidenceIds: ["say-red"],
      confidence: 0.98,
      reason:
        "The recorded voice command directly grounds the requested red color change.",
    },
  ],
  unresolvedIntents: [],
};

const forceBeforeInput = {
  promptVersion: ACTION_PROMPT_VERSION,
  workflow: {
    workflowId: "few-shot-force",
    iteration: 0,
    maxActionIterations: 6,
  },
  recording: {
    frameRate: 30,
    durationFrames: 150,
    audioStartFrame: 0,
  },
  transcript: {
    text: "Pinch the ball so it follows my hand. Open my hand and launch it at the lamp. When it hits the lamp, show the hit text and turn it green.",
    language: "en",
    segments: [],
  },
  chapters: [],
  scene: {
    assets: [
      {
        assetId: "ball",
        assetName: "Ball",
        assetKind: "sphere",
        aliases: [],
        capabilities: ["follow", "unfollow", "applyForce"],
      },
      {
        assetId: "lamp",
        assetName: "Lamp",
        assetKind: "lamp",
        aliases: [],
        capabilities: [],
      },
      {
        assetId: "hit-text",
        assetName: "HitText",
        assetKind: "text",
        aliases: ["hit text"],
        capabilities: ["hide", "show", "changeColor"],
      },
    ],
    contextObjects: [
      {
        objectId: "right-hand",
        objectName: "RIGHTHAND",
        objectKind: "rightHand",
        aliases: ["my hand"],
      },
    ],
  },
  timeline: {
    gestures: [
      {
        sequenceId: "pinch-right",
        sequenceKind: "gesture",
        enabled: true,
        startFrame: 18,
        endFrame: 42,
        gestureLabel: "Pinch",
        hand: "right",
      },
      {
        sequenceId: "open-right",
        sequenceKind: "gesture",
        enabled: true,
        startFrame: 58,
        endFrame: 65,
        gestureLabel: "Open",
        hand: "right",
      },
    ],
    voiceCommands: [],
    collisions: [],
    existingActions: [
      {
        sequenceId: "default-hit-text-visible",
        sequenceKind: "action",
        enabled: true,
        startFrame: 0,
        endFrame: 0,
        actionType: "show",
        targetAssetId: "hit-text",
        params: {},
        source: "default",
      },
    ],
  },
  forceCandidates: [
    {
      candidateId: "ball-release-force",
      assetId: "ball",
      frame: 58,
      initialVelocity: { x: 0.1, y: 0.2, z: 5.8 },
      source: "hand_velocity",
      aimTargetAssetId: "lamp",
    },
  ],
  actionConfidenceThreshold: 0.85,
};

const forceBeforeOutput: ActionPlanningOutput = {
  actions: [
    {
      actionType: "follow",
      targetAssetId: "ball",
      followTargetId: "right-hand",
      anchor: {
        kind: "sequence_start",
        sequenceId: "pinch-right",
      },
      evidenceIds: ["pinch-right"],
      confidence: 0.97,
      reason:
        "The pinch directly grounds starting the ball's follow behavior.",
    },
    {
      actionType: "unfollow",
      targetAssetId: "ball",
      anchor: {
        kind: "sequence_start",
        sequenceId: "open-right",
      },
      evidenceIds: ["open-right"],
      confidence: 0.96,
      reason:
        "Opening the hand explicitly releases the ball from the hand.",
    },
    {
      actionType: "applyForce",
      targetAssetId: "ball",
      forceCandidateId: "ball-release-force",
      scale: 1,
      evidenceIds: ["open-right", "ball-release-force"],
      confidence: 0.96,
      reason:
        "The supplied release force is grounded at the recorded hand opening and aims at the lamp.",
    },
  ],
  unresolvedIntents: [],
};

const forceAfterInput = {
  ...forceBeforeInput,
  workflow: {
    workflowId: "few-shot-force",
    iteration: 1,
    maxActionIterations: 6,
  },
  timeline: {
    ...forceBeforeInput.timeline,
    collisions: [
      {
        sequenceId: "ball-hits-lamp",
        sequenceKind: "collision",
        enabled: true,
        startFrame: 102,
        endFrame: 106,
        objectAId: "ball",
        objectBId: "lamp",
      },
    ],
    existingActions: [
      ...forceBeforeInput.timeline.existingActions,
      {
        sequenceId: "ai-follow-ball",
        sequenceKind: "action",
        enabled: true,
        startFrame: 18,
        endFrame: 18,
        actionType: "follow",
        targetAssetId: "ball",
        params: { followTargetId: "right-hand" },
        source: "ai",
      },
      {
        sequenceId: "ai-unfollow-ball",
        sequenceKind: "action",
        enabled: true,
        startFrame: 58,
        endFrame: 58,
        actionType: "unfollow",
        targetAssetId: "ball",
        params: {},
        source: "ai",
      },
      {
        sequenceId: "ai-force-ball",
        sequenceKind: "action",
        enabled: true,
        startFrame: 58,
        endFrame: 58,
        actionType: "applyForce",
        targetAssetId: "ball",
        params: {
          forceCandidateId: "ball-release-force",
          scale: 1,
        },
        source: "ai",
      },
    ],
  },
};

const forceAfterOutput: ActionPlanningOutput = {
  actions: [
    {
      actionType: "hide",
      targetAssetId: "hit-text",
      anchor: { kind: "recording_start" },
      evidenceIds: ["default-hit-text-visible", "ball-hits-lamp"],
      confidence: 0.95,
      reason:
        "The hit text is visible by default but should appear only after the observed collision.",
    },
    {
      actionType: "show",
      targetAssetId: "hit-text",
      anchor: {
        kind: "sequence_start",
        sequenceId: "ball-hits-lamp",
      },
      evidenceIds: ["ball-hits-lamp"],
      confidence: 0.98,
      reason:
        "Unity now reports the requested ball-lamp collision, which grounds showing the hit text.",
    },
    {
      actionType: "changeColor",
      targetAssetId: "hit-text",
      color: "green",
      anchor: {
        kind: "sequence_start",
        sequenceId: "ball-hits-lamp",
      },
      evidenceIds: ["ball-hits-lamp"],
      confidence: 0.98,
      reason:
        "The observed collision directly grounds changing the hit text to green.",
    },
  ],
  unresolvedIntents: [],
};

export const ACTION_PROMPT_FEW_SHOT_SCENARIOS: ActionPromptFewShotScenario[] = [
  {
    name: "Pinch follow and voice-triggered color",
    examples: [
      {
        name: "gesture and voice anchors",
        input: pinchAndVoiceInput,
        output: pinchAndVoiceOutput,
        expectedActionTypes: ["follow", "changeColor"],
        expectedAnchors: [
          "sequence_start:pinch-right",
          "sequence_start:say-red",
        ],
      },
    ],
  },
  {
    name: "Force workflow before and after Unity resimulation",
    examples: [
      {
        name: "before collision evidence",
        input: forceBeforeInput,
        output: forceBeforeOutput,
        expectedActionTypes: ["follow", "unfollow", "applyForce"],
        expectedAnchors: [
          "sequence_start:pinch-right",
          "sequence_start:open-right",
          "force:ball-release-force",
        ],
      },
      {
        name: "after collision evidence",
        input: forceAfterInput,
        output: forceAfterOutput,
        expectedActionTypes: ["hide", "show", "changeColor"],
        expectedAnchors: [
          "recording_start",
          "sequence_start:ball-hits-lamp",
          "sequence_start:ball-hits-lamp",
        ],
      },
    ],
  },
];

const ACTION_PROMPT_INSTRUCTIONS = `
You are the Mucho action-planning assistant.

Your task is to identify timeline actions explicitly or strongly implied by the
designer's transcript and grounded in the current Unity recording snapshot.

The current snapshot may be one iteration of a longer workflow. Return every
currently grounded action that is still missing. Do not predict physical events
that Unity has not observed yet.

Sources of truth:
1. The transcript describes designer intent.
2. Unity timeline sequences provide observable evidence and valid anchors.
3. Scene assets and context objects provide the only valid target IDs.
4. Existing actions describe what is already present.
5. Force candidates are the only valid source of force vectors.

Allowed actions:
- show: make an asset visible.
- hide: make an asset invisible.
- changeColor: apply one supported canonical color.
- follow: make an asset follow a supplied context object.
- unfollow: stop an existing or newly proposed follow.
- applyForce: apply a Unity-provided force candidate with a scale from 0.25 to 2.0.

Grounding rules:
- Use only IDs supplied in the context.
- Propose an action only if the target asset explicitly lists that capability.
- An empty capabilities array means capabilities are unknown; do not target that asset.
- Never infer capabilities from an asset's name or kind.
- Never invent actions, assets, gestures, voice commands, collisions, colors,
  force candidates, or timeline frames.
- Non-force actions must anchor to recording_start, sequence_start, or
  sequence_end.
- Prefer Unity sequence evidence over approximate transcript timing.
- "When I say X" requires a matching voice sequence. Otherwise return an
  unresolved intent.
- applyForce must select a supplied forceCandidateId. Never generate a vector.
- If applyForce may create a future collision, wait for Unity's next snapshot.
  Do not propose collision-dependent actions until that collision exists.
- If an asset is visible at recording start but should appear only later,
  propose hide at recording_start and show at the grounded later event.
- Do not repeat equivalent existing actions.
- Resolve pronouns only when one active asset is clearly established.
- Put ambiguous, unsupported, or weakly grounded requests in unresolvedIntents.
- Return an empty actions list only when no grounded actions remain.
- Reasons and unresolved-intent messages must always be in English.

Confidence guidance:
- 0.90-1.00: explicit intent, unique target, direct Unity evidence.
- 0.85-0.89: clear intent with one safe coreference or alignment inference.
- Below the configured threshold: prefer unresolvedIntents over an action.

Follow the supplied structured output format. The examples below are behavioral
examples, not additional valid IDs for the current request.
`.trim();

export const ACTION_SYSTEM_PROMPT = [
  ACTION_PROMPT_INSTRUCTIONS,
  ...ACTION_PROMPT_FEW_SHOT_SCENARIOS.map((scenario) =>
    [
      `Few-shot scenario: ${scenario.name}`,
      ...scenario.examples.flatMap((example) => [
        `Snapshot: ${example.name}`,
        `Input:\n${JSON.stringify(example.input)}`,
        `Output:\n${JSON.stringify(example.output)}`,
      ]),
    ].join("\n")
  ),
].join("\n\n");
