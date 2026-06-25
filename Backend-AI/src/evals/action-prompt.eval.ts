import "dotenv/config";
import OpenAI from "openai";
import { zodTextFormat } from "openai/helpers/zod";
import {
  ACTION_PROMPT_FEW_SHOT_SCENARIOS,
  ACTION_PROMPT_VERSION,
  ACTION_SYSTEM_PROMPT,
  type ActionPromptFewShotExample,
} from "../prompts/action-planning.prompt.js";
import {
  ActionPlanningOutputSchema,
  type ActionPlanningOutput,
} from "../workflow/workflow.providers.js";

if (process.env.RUN_OPENAI_EVALS !== "1") {
  console.error(
    "Live evaluation is disabled. Set RUN_OPENAI_EVALS=1 to spend API credit."
  );
  process.exitCode = 1;
} else {
  run().catch((error) => {
    console.error(error instanceof Error ? error.message : String(error));
    process.exitCode = 1;
  });
}

async function run(): Promise<void> {
  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey) {
    throw new Error("OPENAI_API_KEY is not configured.");
  }

  const model =
    process.env.OPENAI_ACTION_MODEL ||
    process.env.OPENAI_TEXT_MODEL ||
    "gpt-5.4-nano";
  const client = new OpenAI({ apiKey });
  const examples = ACTION_PROMPT_FEW_SHOT_SCENARIOS.flatMap(
    (scenario) => scenario.examples
  );
  let failed = false;

  for (const example of examples) {
    const result = await evaluateExample(client, model, example);
    failed ||= !result.structurallyValid || !result.matchesExpected;
    console.log(JSON.stringify(result, null, 2));
  }

  if (failed) {
    process.exitCode = 1;
  }
}

async function evaluateExample(
  client: OpenAI,
  model: string,
  example: ActionPromptFewShotExample
): Promise<Record<string, unknown>> {
  const response = await client.responses.parse({
    model,
    instructions: ACTION_SYSTEM_PROMPT,
    input: JSON.stringify(example.input),
    text: {
      format: zodTextFormat(
        ActionPlanningOutputSchema,
        "mucho_action_plan_eval"
      ),
    },
  });
  const parsed = ActionPlanningOutputSchema.safeParse(response.output_parsed);

  if (!parsed.success) {
    return {
      promptVersion: ACTION_PROMPT_VERSION,
      model,
      example: example.name,
      structurallyValid: false,
      matchesExpected: false,
      expectedActionTypes: example.expectedActionTypes,
      expectedAnchors: example.expectedAnchors,
      unresolvedIntents: [],
      error: parsed.error.message,
    };
  }

  const actualActionTypes = parsed.data.actions.map(
    (action) => action.actionType
  );
  const actualAnchors = parsed.data.actions.map(actionAnchor);
  const matchesExpected =
    sameMembers(actualActionTypes, example.expectedActionTypes) &&
    sameMembers(actualAnchors, example.expectedAnchors);

  return {
    promptVersion: ACTION_PROMPT_VERSION,
    model,
    example: example.name,
    structurallyValid: true,
    matchesExpected,
    expectedActionTypes: example.expectedActionTypes,
    actualActionTypes,
    expectedAnchors: example.expectedAnchors,
    actualAnchors,
    unresolvedIntents: parsed.data.unresolvedIntents,
  };
}

function actionAnchor(
  action: ActionPlanningOutput["actions"][number]
): string {
  if (action.actionType === "applyForce") {
    return `force:${action.forceCandidateId}`;
  }
  if (action.anchor.kind === "recording_start") {
    return "recording_start";
  }
  return `${action.anchor.kind}:${action.anchor.sequenceId}`;
}

function sameMembers(left: string[], right: string[]): boolean {
  const sortedLeft = [...left].sort();
  const sortedRight = [...right].sort();
  return (
    sortedLeft.length === sortedRight.length &&
    sortedLeft.every((value, index) => value === sortedRight[index])
  );
}
