import { v4 as uuidv4 } from "uuid";
import type { ActionToRecord } from "../../schemas/action.schema.js";
import type { Asset } from "../../schemas/scene.schema.js";
import type {
  CollisionSequence,
  ExistingActionSequence,
  GestureSequence,
  Sequence,
} from "../../schemas/sequence.schema.js";
import type { AnalyzeRecordingState } from "../state.js";

type ColorName = Extract<
  ActionToRecord,
  { actionType: "changeColor" }
>["color"];

type FollowTargetType = Extract<
  ActionToRecord,
  { actionType: "follow" }
>["followTargetType"];

interface ChapterInput {
  text: string;
  frameStart: number;
  frameEnd: number;
  relatedSequenceIds: string[];
}

interface AssetMatch {
  asset: Asset;
  index: number;
  endIndex: number;
  phrase: string;
  specificity: number;
}

type TargetResolution =
  | { status: "found"; asset: Asset }
  | { status: "none" }
  | { status: "ambiguous"; reason: string };

const GRAB_MARKERS = [
  "grab",
  "grabs",
  "grabbed",
  "pinch",
  "pinches",
  "pinched",
  "hold",
  "holds",
  "held",
];
const FOLLOW_MARKERS = ["follow", "follows", "following"];
const THROW_MARKERS = [
  "throw",
  "throws",
  "threw",
  "launch",
  "launches",
  "shoot",
  "shoots",
  "toss",
  "tosses",
];
const SHOW_MARKERS = ["show", "shows", "appear", "appears", "display", "displays"];
const HIDE_MARKERS = ["hide", "hides", "disappear", "disappears"];
const CHANGE_COLOR_MARKERS = [
  "change",
  "changes",
  "make",
  "makes",
  "turn",
  "turns",
  "set",
  "color",
  "colour",
];
const ACTION_TARGET_STOPS = [
  "when",
  "if",
  "after",
  "before",
  "once",
  "then",
  "and",
  "with",
  "because",
];
const COLOR_WORDS: Array<{ word: string; color: ColorName }> = [
  { word: "red", color: "red" },
  { word: "rosso", color: "red" },
  { word: "rossa", color: "red" },
  { word: "green", color: "green" },
  { word: "verde", color: "green" },
  { word: "blue", color: "blue" },
  { word: "blu", color: "blue" },
  { word: "yellow", color: "yellow" },
  { word: "giallo", color: "yellow" },
  { word: "gialla", color: "yellow" },
  { word: "pink", color: "pink" },
  { word: "rosa", color: "pink" },
  { word: "gray", color: "gray" },
  { word: "grey", color: "gray" },
  { word: "grigio", color: "gray" },
  { word: "grigia", color: "gray" },
  { word: "white", color: "white" },
  { word: "bianco", color: "white" },
  { word: "bianca", color: "white" },
  { word: "black", color: "black" },
  { word: "nero", color: "black" },
  { word: "nera", color: "black" },
];

export function generateActionCandidates(
  state: AnalyzeRecordingState
): AnalyzeRecordingState {
  const { request, chapters, transcript } = state;

  if (!transcript) {
    return state;
  }

  const candidates: ActionToRecord[] = [];
  const chapterInputs = getChapterInputs(
    transcript.text,
    chapters,
    request.recording.durationFrames
  );
  let activeAsset: Asset | null = findLatestFollowedAsset(
    request.sequences,
    request.scene.assets
  );
  let activeFrame = 0;

  for (const chapter of chapterInputs) {
    const sentence = normalizePhrase(chapter.text);

    if (!sentence) {
      continue;
    }

    const followTarget = resolveFollowTarget(
      sentence,
      request.scene.assets,
      activeAsset
    );

    if (hasAnyMarker(sentence, [...GRAB_MARKERS, ...FOLLOW_MARKERS])) {
      if (followTarget.status === "found") {
        const followTargetType = resolveFollowTargetType(sentence);
        const followFrame =
          findGestureFrame(
            request.sequences,
            ["Closed", "Pinch"],
            handFromFollowTarget(followTargetType),
            chapter.frameStart
          ) ?? chapter.frameStart;

        candidates.push({
          actionId: uuidv4(),
          actionType: "follow",
          targetAssetName: followTarget.asset.assetName,
          startFrame: Math.max(0, followFrame),
          followTargetType,
          explanation:
            "The user identified this asset as the hand-held/following object.",
          confidence: 0.9,
        });

        activeAsset = followTarget.asset;
        activeFrame = followFrame;
      } else if (followTarget.status === "ambiguous") {
        addUnresolvedIntent(
          state,
          chapter,
          "Ambiguous follow target",
          followTarget.reason
        );
      }
    }

    if (hasAnyMarker(sentence, THROW_MARKERS)) {
      const throwTarget = resolveThrowTarget(
        sentence,
        request.scene.assets,
        activeAsset
      );

      if (throwTarget.status === "found") {
        const throwFrame =
          findGestureFrame(
            request.sequences,
            ["Open"],
            undefined,
            activeFrame || chapter.frameStart
          ) ?? chapter.frameStart;

        candidates.push({
          actionId: uuidv4(),
          actionType: "unfollow",
          targetAssetName: throwTarget.asset.assetName,
          startFrame: Math.max(0, throwFrame),
          explanation: "The user will throw the active asset, so unfollow first.",
          confidence: 0.9,
        });
        candidates.push({
          actionId: uuidv4(),
          actionType: "throwAsset",
          targetAssetName: throwTarget.asset.assetName,
          startFrame: Math.max(0, throwFrame),
          forceVelocityMode: { x: 0, y: 0, z: 1 },
          explanation: "The user said to throw this asset.",
          confidence: 0.9,
        });

        activeAsset = throwTarget.asset;
        activeFrame = throwFrame;
      } else if (throwTarget.status === "ambiguous") {
        addUnresolvedIntent(
          state,
          chapter,
          "Ambiguous throw target",
          throwTarget.reason
        );
      }
    }

    const showTarget = hasAnyMarker(sentence, SHOW_MARKERS)
      ? resolveTargetAfterMarkers(sentence, SHOW_MARKERS, request.scene.assets)
      : ({ status: "none" } satisfies TargetResolution);

    if (showTarget.status === "found") {
      const showFrame =
        findCollisionFrame(
          sentence,
          request.sequences,
          request.scene.assets,
          activeAsset,
          showTarget.asset
        ) ?? chapter.frameStart;

      candidates.push({
        actionId: uuidv4(),
        actionType: "show",
        targetAssetName: showTarget.asset.assetName,
        startFrame: Math.max(0, showFrame),
        explanation: "The user explicitly asked to show this asset.",
        confidence: 0.9,
      });

      if (
        showTarget.asset.assetKind === "text" &&
        showFrame > 0 &&
        (request.options.allowDefaultTextHide ?? true) &&
        hasExistingDefaultShow(request.sequences, showTarget.asset.assetName)
      ) {
        candidates.push({
          actionId: uuidv4(),
          actionType: "hide",
          targetAssetName: showTarget.asset.assetName,
          startFrame: 0,
          explanation:
            "Hiding text at frame 0 so it can appear later at the requested moment.",
          confidence: 0.9,
        });
      }
    } else if (showTarget.status === "ambiguous") {
      addUnresolvedIntent(
        state,
        chapter,
        "Ambiguous show target",
        showTarget.reason
      );
    }

    if (hasAnyMarker(sentence, HIDE_MARKERS)) {
      const hideTarget = resolveTargetAfterMarkers(
        sentence,
        HIDE_MARKERS,
        request.scene.assets
      );

      if (hideTarget.status === "found") {
        candidates.push({
          actionId: uuidv4(),
          actionType: "hide",
          targetAssetName: hideTarget.asset.assetName,
          startFrame: Math.max(0, chapter.frameStart),
          explanation: "The user explicitly asked to hide this asset.",
          confidence: 0.9,
        });
      } else if (hideTarget.status === "ambiguous") {
        addUnresolvedIntent(
          state,
          chapter,
          "Ambiguous hide target",
          hideTarget.reason
        );
      }
    }

    const colorIntent = findColorIntent(sentence);
    if (colorIntent) {
      const colorTarget = resolveColorTarget(
        sentence,
        request.scene.assets,
        showTarget.status === "found" ? showTarget.asset : null
      );

      if (colorTarget.status === "found") {
        const colorFrame =
          findCollisionFrame(
            sentence,
            request.sequences,
            request.scene.assets,
            activeAsset,
            colorTarget.asset
          ) ?? chapter.frameStart;

        candidates.push({
          actionId: uuidv4(),
          actionType: "changeColor",
          targetAssetName: colorTarget.asset.assetName,
          startFrame: Math.max(0, colorFrame),
          color: colorIntent.color,
          explanation: `The user specified the color ${colorIntent.color} for this asset.`,
          confidence: 0.9,
        });
      } else if (colorTarget.status === "ambiguous") {
        addUnresolvedIntent(
          state,
          chapter,
          "Ambiguous color target",
          colorTarget.reason
        );
      }
    }
  }

  state.actionCandidates = candidates;
  return state;
}

function getChapterInputs(
  transcriptText: string,
  chapters: AnalyzeRecordingState["chapters"],
  durationFrames: number
): ChapterInput[] {
  if (chapters.length > 0) {
    return chapters.map((chapter) => ({
      text: chapter.summary,
      frameStart: chapter.frameStart,
      frameEnd: chapter.frameEnd,
      relatedSequenceIds: chapter.relatedSequenceIds,
    }));
  }

  const fragments = splitTranscriptFragments(transcriptText);
  const framesPerFragment =
    fragments.length > 0 ? Math.floor(durationFrames / fragments.length) : durationFrames;

  return fragments.map((fragment, index) => {
    const frameStart = index * framesPerFragment;
    return {
      text: fragment,
      frameStart,
      frameEnd:
        index === fragments.length - 1
          ? durationFrames
          : Math.min(durationFrames, frameStart + framesPerFragment),
      relatedSequenceIds: [],
    };
  });
}

function splitTranscriptFragments(text: string): string[] {
  return text
    .split(/[.!?]+|\r?\n+/)
    .map((fragment) => fragment.trim())
    .filter((fragment) => fragment.length > 0);
}

function resolveFollowTarget(
  sentence: string,
  assets: Asset[],
  activeAsset: Asset | null
): TargetResolution {
  const grabbedTarget = resolveTargetAfterMarkers(
    sentence,
    GRAB_MARKERS,
    assets
  );

  if (grabbedTarget.status !== "none") {
    return grabbedTarget;
  }

  const followMarkerIndex = findFirstMarkerIndex(sentence, FOLLOW_MARKERS);

  if (followMarkerIndex >= 0) {
    const subjectTarget = selectSingleAsset(
      findAssetMatches(sentence, assets).filter(
        (match) => match.endIndex <= followMarkerIndex
      )
    );

    if (subjectTarget.status !== "none") {
      return subjectTarget;
    }

    const targetAfterMarker = resolveTargetAfterMarkers(
      sentence,
      FOLLOW_MARKERS,
      assets
    );

    if (targetAfterMarker.status !== "none") {
      return targetAfterMarker;
    }
  }

  if (activeAsset && hasAnyMarker(sentence, ["it"])) {
    return { status: "found", asset: activeAsset };
  }

  return { status: "none" };
}

function resolveThrowTarget(
  sentence: string,
  assets: Asset[],
  activeAsset: Asset | null
): TargetResolution {
  const explicitTarget = resolveTargetAfterMarkers(
    sentence,
    THROW_MARKERS,
    assets
  );

  if (explicitTarget.status !== "none") {
    return explicitTarget;
  }

  if (activeAsset && containsPronounAfterMarker(sentence, THROW_MARKERS)) {
    return { status: "found", asset: activeAsset };
  }

  if (activeAsset) {
    return { status: "found", asset: activeAsset };
  }

  return {
    status: "ambiguous",
    reason: "The transcript contains a throw intent but no clear asset target.",
  };
}

function resolveColorTarget(
  sentence: string,
  assets: Asset[],
  showTarget: Asset | null
): TargetResolution {
  if (showTarget) {
    return { status: "found", asset: showTarget };
  }

  const actionTarget = resolveTargetAfterMarkers(
    sentence,
    CHANGE_COLOR_MARKERS,
    assets
  );

  if (actionTarget.status !== "none") {
    return actionTarget;
  }

  const colorIntent = findColorIntent(sentence);

  if (!colorIntent) {
    return { status: "none" };
  }

  return selectSingleAsset(
    findAssetMatches(sentence, assets).filter(
      (match) => match.endIndex <= colorIntent.index
    )
  );
}

function resolveTargetAfterMarkers(
  sentence: string,
  markers: string[],
  assets: Asset[]
): TargetResolution {
  const markerIndex = findFirstMarkerIndex(sentence, markers);

  if (markerIndex < 0) {
    return { status: "none" };
  }

  const segmentEnd = findSegmentEnd(sentence, markerIndex, ACTION_TARGET_STOPS);
  return selectSingleAsset(
    findAssetMatches(sentence, assets).filter(
      (match) => match.index >= markerIndex && match.index < segmentEnd
    )
  );
}

function selectSingleAsset(matches: AssetMatch[]): TargetResolution {
  const bestByAsset = new Map<string, AssetMatch>();

  for (const match of matches) {
    const existing = bestByAsset.get(match.asset.assetName);
    if (
      !existing ||
      match.index < existing.index ||
      (match.index === existing.index && match.specificity > existing.specificity)
    ) {
      bestByAsset.set(match.asset.assetName, match);
    }
  }

  const ordered = Array.from(bestByAsset.values()).sort(
    (a, b) => a.index - b.index || b.specificity - a.specificity
  );

  if (ordered.length === 0) {
    return { status: "none" };
  }

  const first = ordered[0];
  const competingAtSamePosition = ordered.filter(
    (match) => match.index === first.index
  );

  if (competingAtSamePosition.length > 1) {
    return {
      status: "ambiguous",
      reason: `Multiple assets match the same action target phrase: ${competingAtSamePosition
        .map((match) => match.asset.assetName)
        .join(", ")}.`,
    };
  }

  return { status: "found", asset: first.asset };
}

function findAssetMatches(sentence: string, assets: Asset[]): AssetMatch[] {
  const matches: AssetMatch[] = [];

  for (const asset of assets) {
    for (const phrase of getAssetPhrases(asset)) {
      const paddedSentence = ` ${sentence} `;
      const paddedPhrase = ` ${phrase} `;
      let searchIndex = 0;
      let matchIndex = paddedSentence.indexOf(paddedPhrase, searchIndex);

      while (matchIndex >= 0) {
        matches.push({
          asset,
          index: matchIndex,
          endIndex: matchIndex + phrase.length,
          phrase,
          specificity: phrase.length + phrase.split(" ").length,
        });
        searchIndex = matchIndex + paddedPhrase.length - 1;
        matchIndex = paddedSentence.indexOf(paddedPhrase, searchIndex);
      }
    }
  }

  return matches.sort((a, b) => a.index - b.index || b.specificity - a.specificity);
}

function getAssetPhrases(asset: Asset): string[] {
  const rawPhrases = [
    asset.assetName,
    asset.displayName,
    asset.assetKind,
    ...(asset.aliases ?? []),
  ];

  return Array.from(
    new Set(
      rawPhrases
        .filter((phrase): phrase is string => Boolean(phrase))
        .map((phrase) => normalizePhrase(phrase))
        .filter((phrase) => phrase.length > 0)
    )
  ).sort((a, b) => b.length - a.length);
}

function findGestureFrame(
  sequences: Sequence[],
  labels: GestureSequence["gestureLabel"][],
  hand?: GestureSequence["hand"],
  afterFrame?: number
): number | null {
  const gestures = sequences
    .filter((seq): seq is GestureSequence => seq.sequenceKind === "gesture")
    .filter((seq) => labels.includes(seq.gestureLabel))
    .filter((seq) => !hand || seq.hand === hand)
    .sort((a, b) => a.startFrame - b.startFrame);
  const afterMatch = gestures.find(
    (seq) => afterFrame === undefined || seq.startFrame >= afterFrame
  );

  return (afterMatch ?? gestures[0])?.startFrame ?? null;
}

function findCollisionFrame(
  sentence: string,
  sequences: Sequence[],
  assets: Asset[],
  activeAsset: Asset | null,
  actionTarget: Asset
): number | null {
  if (
    !hasAnyMarker(sentence, [
      "hit",
      "hits",
      "collide",
      "collides",
      "touch",
      "touches",
    ])
  ) {
    return null;
  }

  const mentionedAssets = new Set(
    findAssetMatches(sentence, assets)
      .map((match) => match.asset.assetName)
      .filter((assetName) => assetName !== actionTarget.assetName)
  );
  const collisions = sequences
    .filter((seq): seq is CollisionSequence => seq.sequenceKind === "collision")
    .sort((a, b) => a.startFrame - b.startFrame);

  for (const collision of collisions) {
    const participants = [collision.objectAName, collision.objectBName];
    const hasActiveAsset =
      activeAsset !== null && participants.includes(activeAsset.assetName);
    const mentionedParticipantCount = participants.filter((participant) =>
      mentionedAssets.has(participant)
    ).length;

    if (hasActiveAsset && mentionedParticipantCount > 0) {
      return collision.startFrame;
    }

    if (mentionedParticipantCount === 2) {
      return collision.startFrame;
    }
  }

  return null;
}

function hasExistingDefaultShow(
  sequences: Sequence[],
  targetAssetName: string
): boolean {
  return sequences.some(
    (seq) =>
      seq.sequenceKind === "existingAction" &&
      seq.actionType === "show" &&
      seq.targetAssetName === targetAssetName &&
      seq.startFrame === 0
  );
}

function findLatestFollowedAsset(
  sequences: Sequence[],
  assets: Asset[]
): Asset | null {
  const followed = sequences
    .filter(
      (seq): seq is ExistingActionSequence =>
        seq.sequenceKind === "existingAction" && seq.actionType === "follow"
    )
    .sort((a, b) => b.startFrame - a.startFrame)[0];

  if (!followed) {
    return null;
  }

  return assets.find((asset) => asset.assetName === followed.targetAssetName) ?? null;
}

function findColorIntent(
  sentence: string
): { color: ColorName; index: number } | null {
  const matches = COLOR_WORDS.map(({ word, color }) => ({
    color,
    index: findWordIndex(sentence, word),
  }))
    .filter((match) => match.index >= 0)
    .sort((a, b) => a.index - b.index);

  return matches[0] ?? null;
}

function resolveFollowTargetType(sentence: string): FollowTargetType {
  if (hasAnyMarker(sentence, ["left"])) {
    return "LEFTHAND";
  }

  if (hasAnyMarker(sentence, ["gaze"])) {
    return "GAZEFOCUS";
  }

  if (hasAnyMarker(sentence, ["focus"])) {
    return "RIGHTFOCUS";
  }

  return "RIGHTHAND";
}

function handFromFollowTarget(
  followTargetType: FollowTargetType
): GestureSequence["hand"] | undefined {
  if (followTargetType === "LEFTHAND" || followTargetType === "LEFTFOCUS") {
    return "left";
  }

  if (followTargetType === "RIGHTHAND" || followTargetType === "RIGHTFOCUS") {
    return "right";
  }

  return undefined;
}

function containsPronounAfterMarker(sentence: string, markers: string[]): boolean {
  const markerIndex = findFirstMarkerIndex(sentence, markers);

  if (markerIndex < 0) {
    return false;
  }

  return findWordIndex(sentence.slice(markerIndex), "it") >= 0;
}

function hasAnyMarker(sentence: string, markers: string[]): boolean {
  return markers.some((marker) => findWordIndex(sentence, marker) >= 0);
}

function findFirstMarkerIndex(sentence: string, markers: string[]): number {
  const markerIndexes = markers
    .map((marker) => findWordIndex(sentence, marker))
    .filter((index) => index >= 0);

  return markerIndexes.length > 0 ? Math.min(...markerIndexes) : -1;
}

function findWordIndex(sentence: string, word: string): number {
  const normalizedWord = normalizePhrase(word);

  if (!normalizedWord) {
    return -1;
  }

  return ` ${sentence} `.indexOf(` ${normalizedWord} `);
}

function findSegmentEnd(
  sentence: string,
  startIndex: number,
  stopWords: string[]
): number {
  const endIndexes = stopWords
    .map((stopWord) => findWordIndex(sentence.slice(startIndex), stopWord))
    .filter((index) => index > 0)
    .map((index) => startIndex + index);

  return endIndexes.length > 0 ? Math.min(...endIndexes) : sentence.length;
}

function normalizePhrase(value: string): string {
  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .toLowerCase()
    .replace(/\(clone\)/g, " ")
    .replace(/[^a-z0-9]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function addUnresolvedIntent(
  state: AnalyzeRecordingState,
  chapter: ChapterInput,
  summary: string,
  reason: string
): void {
  state.unresolvedIntents.push({
    intentId: uuidv4(),
    summary,
    reason,
    relatedSequenceIds: chapter.relatedSequenceIds,
  });
}
