# Mucho AI Backend Status

Last updated: 2026-05-19

This document summarizes the current status of the Mucho AI backend in this
workspace. It uses `mucho_ai_backend_implementation_plan (4).md` as the product
and architecture guide, then compares that plan with the backend files currently
present under `Backend-AI`.

## Executive Summary

The backend is a first prototype for automating the manual Mucho timeline step
where a designer records an interaction, reviews frame-based events, and adds
effects such as `follow`, `unfollow`, `throwAsset`, `show`, `hide`, and
`changeColor`.

The current implementation follows the intended v1 scope from the plan:

- It does not generate full Mucho states.
- It focuses on proposing timeline actions/effects.
- It expects Unity/Mucho to provide structured scene and simulation context.
- It validates requests with Zod.
- It exposes Express endpoints.
- It runs a simple LangGraph-style pipeline.
- It uses mock transcript/action-generation logic rather than a real model.

Repository note: the backend project structure has been restored. `src` now
contains TypeScript source and tests, while generated JavaScript/declaration
files are produced under `dist`.

## Current Folder State

Current top-level `Backend-AI` contents:

```text
Backend-AI/
  .gitignore
  .env.example
  README.md
  backend_status.md
  mucho_ai_backend_implementation_plan (4).md
  node_modules/
  package-lock.json
  package.json
  src/
  tsconfig.json
```

Current source structure:

```text
src/app.ts
src/server.ts
src/graph/**/*.ts
src/schemas/**/*.ts
src/**/*.ts
src/tests/**/*.test.ts
```

Generated output:

```text
dist/
```

The restored setup matches the intended TypeScript layout: source files live in
`src`, compiled output lives in `dist`, and `dist` is ignored by Git.

## Goal From The Implementation Plan

The guide defines a narrow v1 backend goal:

> Given what the designer said and what Mucho observed in the recording,
> propose which effects should be added to which objects at which frames.

The backend should automate old Phase 3 of Mucho:

1. Designer records themselves acting out an interaction.
2. Mucho detects frame-based events such as gestures and collisions.
3. Designer would normally inspect the timeline manually.
4. Designer would normally add effects manually.
5. Backend proposes those effects from speech plus structured recording data.

The backend should not:

- Generate a full state machine.
- Mutate the Unity project directly.
- Invent unsupported effects.
- Hallucinate objects, gestures, collisions, or frame numbers.
- Apply ambiguous actions automatically.

## Current Architecture

The current implementation is a compact Express service with a graph-like
analysis pipeline.

Main runtime files:

```text
src/app.js
src/server.js
src/graph/analyzeRecording.graph.js
src/graph/state.js
src/graph/nodes/validateRequest.node.js
src/graph/nodes/obtainTranscript.node.js
src/graph/nodes/extractChapters.node.js
src/graph/nodes/generateActionCandidates.node.js
src/graph/nodes/validateAndFilterActions.node.js
src/graph/nodes/buildResponse.node.js
src/schemas/*.js
```

The pipeline currently runs in this order:

```text
analyzeRecording(request)
  1. initializeState
  2. validateRequest
  3. obtainTranscript
  4. extractChapters
  5. generateActionCandidates
  6. validateAndFilterActions
  7. buildResponse
```

This matches the planned v1 shape, but the implementation is still a mock
heuristic system rather than a true LangGraph.js graph with model-backed nodes.

## Current Endpoints

### GET `/api/v1/health`

Returns:

```json
{
  "status": "ok"
}
```

Status:

- Implemented.
- Minimal response.
- The implementation plan suggests adding service name/version later.

### POST `/api/v1/effects/propose`

This is the main implemented endpoint.

Purpose:

- Accept a JSON Mucho recording payload.
- Validate it with Zod.
- Run the analysis pipeline.
- Return proposed `actionsToRecord`.

Status:

- Implemented.
- Currently JSON-only.
- This is a development/debug endpoint in the guide.
- The guide's eventual production endpoint is `/api/v1/recordings/analyze`.

### POST `/api/v1/audio/transcribe`

Purpose:

- Accept raw audio bytes with `application/octet-stream`.
- Return a mock transcript.

Status:

- Implemented as a mock.
- Does not call a real transcription provider.
- Correctly uses route-specific `express.raw()` before JSON middleware.

### GET `/dev`

Purpose:

- Developer console for manually submitting JSON payloads.

Status:

- Implemented.
- Contains embedded example payloads.
- Sends the textarea transcript as `payload.transcript`.
- Server also supports legacy `mockTranscript` by normalizing it to `transcript`
  when `transcript` is absent.

## Current Request Model

The current `/api/v1/effects/propose` schema accepts:

```json
{
  "projectId": "optional",
  "recordingId": "required",
  "languageHint": "en | it | da | auto",
  "recording": {
    "frameRate": "positive number",
    "durationFrames": "positive integer",
    "audioStartFrame": "nonnegative integer, default 0",
    "recordingMode": "voice_during_miming"
  },
  "transcript": {
    "text": "string",
    "language": "en | it | da | unknown",
    "segments": []
  },
  "useMockTranscript": false,
  "scene": {
    "assets": [],
    "contextObjects": []
  },
  "sequences": [],
  "contextObservations": [],
  "options": {
    "confidenceThreshold": 0.88,
    "includeDebugChapters": true,
    "allowDefaultTextHide": true
  }
}
```

Differences from the implementation guide:

- The guide's broader schema uses `scene.objects` with capabilities and IDs.
- The current schema uses `scene.assets` and `scene.contextObjects`.
- The guide discusses multipart audio plus payload for the future main endpoint.
- The current main endpoint uses JSON only.
- `audioStartFrame` allows any nonnegative integer, not only `0`.

## Scene Model

### Assets

Assets are valid action targets.

Supported asset kinds:

```text
sphere
cube
basketball
book
lamp
text
unknown
```

Each asset has:

```text
assetName
displayName?
assetKind
aliases?
```

### Context Objects

Context objects are not action targets. They are evidence or parameters.

Supported context kinds:

```text
leftHand
rightHand
leftFocus
rightFocus
gazeFocus
camera
other
```

This matches one of the guide's most important design principles:

> Unity detects facts. AI infers user intent.

## Sequence Model

The backend accepts three kinds of sequences:

```text
gesture
collision
existingAction
```

### Gesture Sequences

Gesture sequences include:

```text
rawGesture
gestureLabel: Closed | Pinch | Open
hand: left | right
startFrame
length
```

Supported raw gestures:

```text
Gesture.LEFTHANDGRAB
Gesture.LEFTHANDPINCH
Gesture.LEFTHANDOPEN
Gesture.RIGHTHANDGRAB
Gesture.RIGHTHANDPINCH
Gesture.RIGHTHANDOPEN
```

### Collision Sequences

Collision sequences include:

```text
objectAName
objectBName
startFrame
length
```

### Existing Action Sequences

Existing actions are sent by Unity so the backend can avoid duplicates and reason
about default timeline state.

Existing action fields:

```text
actionType
targetAssetName
params
source: default | manual | ai | unknown
startFrame
length
```

Supported action types:

```text
show
hide
changeColor
follow
unfollow
throwAsset
```

Current status:

- `length` allows `0` at schema level.
- `validateRequest` emits a `ZERO_LENGTH_SEQUENCE` warning for `length === 0`.
- This makes the warning reachable through HTTP rather than failing at parse time.

## Response Model

The backend returns:

```json
{
  "analysisId": "uuid",
  "recordingId": "recording id from request",
  "status": "completed | partial | failed",
  "transcript": {
    "text": "resolved transcript text",
    "language": "optional language",
    "source": "mock | provided | transcribed"
  },
  "chapters": [],
  "actionsToRecord": [],
  "unresolvedIntents": [],
  "warnings": []
}
```

This aligns with the guide's desired output shape:

- debug chapters;
- new action proposals;
- unresolved ambiguous intents;
- warnings and validation information.

## Implemented Action Generation

Current action generation is heuristic and conservative. It does not call an LLM.

Implemented output actions:

```text
show
hide
changeColor
follow
unfollow
throwAsset
```

### Follow

The generator looks for grab/pinch/hold/follow language and tries to resolve the
asset being held or followed.

Examples of markers:

```text
grab
pinch
hold
follow
follows
following
```

Frame anchoring:

- Uses the first matching `Closed` or `Pinch` gesture.
- Uses hand information to choose `LEFTHAND` or `RIGHTHAND`.
- Defaults to `RIGHTHAND` if not otherwise specified.

### Throw

The generator looks for throw-like language.

Examples of markers:

```text
throw
launch
shoot
toss
```

Targeting:

- Explicit target after the throw verb is preferred.
- Pronoun `it` can resolve to the current active asset from a previous chapter.
- If no active or explicit asset exists, the intent becomes unresolved.

Frame anchoring:

- Uses an `Open` gesture if available.
- Otherwise uses the chapter frame.

Actions generated:

- `unfollow`
- `throwAsset`

The current throw vector is still a v1 default:

```json
{
  "x": 0,
  "y": 0,
  "z": 1
}
```

### Show

The generator looks for show/display/appear language.

Targeting:

- Resolves the asset after the action verb.
- Collision condition objects should not become action targets.
- Example: in `when it hits the cube, show the hit text`, the target is
  `HitText(Clone)`, not `Cube(Clone)`.

Frame anchoring:

- If collision language is present and a matching collision exists, uses the
  collision start frame.
- Otherwise uses the chapter frame.

Default text handling:

- If showing a text asset later and Unity already sent a default `show` at frame
  `0`, the backend can add a `hide` at frame `0` when
  `allowDefaultTextHide` is true.

### Hide

The generator looks for hide/disappear language and resolves the explicit target
after the hide verb.

### Change Color

The generator supports color intent and canonicalizes colors.

Accepted canonical colors:

```text
red
green
blue
yellow
pink
gray
white
black
```

Supported normalization:

```text
grey -> gray
grigio -> gray
```

Simple Italian color words are also recognized:

```text
rosso
rossa
verde
blu
giallo
gialla
rosa
grigio
grigia
bianco
bianca
nero
nera
```

Example supported phrases:

```text
change the sphere to grey
make the ball red
show the hit text in green
```

## Duplicate Detection

Duplicate detection is frame-aware.

Two actions are considered duplicates only when they have:

- same `actionType`;
- same `targetAssetName`;
- same `startFrame`;
- same relevant params.

Relevant params currently considered:

```text
changeColor: color
follow: followTargetType
throwAsset: forceVelocityMode
```

This means an existing default `show` at frame `0` does not block:

- `show` at frame `200`;
- `hide` at frame `0`;
- `changeColor` at frame `200`.

## Validation Behavior

Current warnings:

```text
ZERO_LENGTH_SEQUENCE
INVALID_ACTION_TARGET
FRAME_OUT_OF_BOUNDS
NO_TRANSCRIPT
DUPLICATE_ACTION
```

Current error behavior:

- Missing transcript and `useMockTranscript: false` causes the pipeline status
  to become `failed`.
- Invalid Zod request shape causes HTTP `400`.
- Unexpected pipeline exceptions become a failed response with an internal error
  message in state.

Important gap:

- The `errors` array exists in graph state but is not included in the public
  response schema. Failed responses currently expose failure mostly through
  `status`, empty transcript/actions, and warnings where available.

## Transcript And Chaptering Status

Transcript source order:

1. Use `request.transcript` if present and nonempty.
2. Else use built-in mock transcript if `useMockTranscript` is true.
3. Else fail the pipeline.

Chapter extraction:

- Splits transcript on `.`, `!`, `?`, and line breaks.
- Preserves trailing fragments without final punctuation.
- Removes empty fragments.
- Assigns frame windows evenly across recording duration.
- Attaches overlapping sequence IDs and observation indexes.

Known limitation:

- Chapter timing is approximate. It does not use transcript timestamps yet.

## Developer Console Status

The `/dev` route is an embedded HTML page with:

- JSON payload textarea;
- mock transcript textarea;
- output response textarea;
- built-in example payloads.

Current behavior:

- The mock transcript textarea value is inserted into `payload.transcript`.
- The request is sent to `/api/v1/effects/propose`.

Known issue:

- The page contains mojibake characters in visible text and console messages,
  probably from emoji/encoding conversion in generated JavaScript.

## Production-Like Expected Behavior

For a payload with:

```text
Transcript:
I grab the basketball with my right hand, then I throw it.
When it hits the cube, show the hit text in green.

Scene:
Basketball(Clone)
Cube(Clone)
HitText(Clone)

Sequences:
right hand pinch
right hand open
basketball-cube collision
default show at frame 0 for assets
```

Expected action proposals:

```text
Basketball(Clone): follow at pinch frame
Basketball(Clone): unfollow at open frame
Basketball(Clone): throwAsset at open frame
HitText(Clone): hide at frame 0, if default text hide is enabled
HitText(Clone): show at collision frame
HitText(Clone): changeColor green at collision frame
```

Expected non-actions:

```text
Cube(Clone): no follow
Cube(Clone): no throwAsset
Cube(Clone): no show just because it was mentioned as a collision object
```

This behavior is aligned with the guide's conservative auto-apply principle.

## What Is Implemented Compared To The Guide

Implemented:

- Express service.
- Health endpoint.
- JSON proposal endpoint.
- Raw audio mock transcription endpoint.
- Zod request and response schemas.
- Scene assets vs context objects.
- Gesture, collision, and existing action sequences.
- Debug chapters.
- Action proposals.
- Unresolved intents.
- Warnings.
- Frame-aware duplicate detection.
- Conservative targeting for common follow/throw/show/color cases.
- Default text hide support.

Partially implemented:

- LangGraph-style pipeline exists, but it is simple function composition rather
  than a full LangGraph.js graph.
- Transcript chaptering exists, but timestamp alignment is heuristic.
- Context observations are accepted and attached to chapters, but not deeply
  used for action targeting.
- Italian support exists for simple color words only.
- Danish language hint is accepted, but there is no meaningful Danish intent
  parsing yet.

Not implemented:

- `/api/v1/recordings/analyze` production endpoint.
- Multipart audio plus JSON payload endpoint.
- Real transcription provider.
- OpenAI or model-backed action generation.
- AI provider abstraction.
- Effect catalog/capability validation per asset.
- Persistent storage by `analysisId`.
- Proposal refinement endpoint.
- Full Unity preview/accept/reject loop.
- State generation.
- State merging.
- Real force-vector inference for throws.
- Confidence calibration from model output.

## Current Repository Health

The backend is back in a workable TypeScript project state.

Restored:

- `package.json`
- `package-lock.json`
- `tsconfig.json`
- `.env.example`
- `src/**/*.ts`
- `src/tests/**/*.test.ts`

Cleaned up:

- Removed generated `.js`, `.d.ts`, and `.map` files from `src`.
- Build output now belongs in `dist/`.

Verified:

```text
npm run build
npm test -- --run
```

Both commands pass in the current workspace.

Known maintenance notes:

- `npm install` reports 4 moderate vulnerabilities in current dependencies.
- `uuid@9.0.1` is deprecated by npm's warning output. It still works, but should
  be upgraded deliberately later.
- `dist/` is ignored and should not be committed.

## Suggested Next Steps

### Short Term

1. Add this status document and the implementation plan to version control.
2. Decide whether to keep the current heuristic generator or replace it with a
   model-backed provider behind an interface.
3. Expand fixtures/examples for Unity integration payloads.
4. Address npm audit findings deliberately, without blind forced upgrades.

### Medium Term

1. Add `/api/v1/recordings/analyze`.
2. Add a provider interface for transcription.
3. Add a provider interface for model-backed action proposal generation.
4. Add effect capability validation per asset.
5. Use transcript timestamps when available.
6. Use context observations more directly for target disambiguation.

### Long Term

1. Integrate with Unity preview/accept/reject workflow.
2. Store analyses for debugging and reproducibility.
3. Expand multilingual support.
4. Infer better throw vectors from hand/object movement.
5. Add state generation only after timeline action proposal quality is stable.

## Status Label

Current backend status:

```text
Prototype logic exists and the local TypeScript project structure is restored.
```

Product readiness:

```text
Not production-ready.
Useful as a prototype/reference implementation for the v1 action-proposal flow.
```

Most important next action:

```text
Commit the restored source/test/package files and docs before adding new backend features.
```
