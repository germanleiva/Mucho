import OpenAI from "openai";
import { zodTextFormat } from "openai/helpers/zod";
import { ZodError } from "zod";
import { buildActionPlanningContext } from "./action-planning.context.js";
import {
  ActionPlanningOutputSchema,
  StateMergeOutputSchema,
  type ActionPlanningOutput,
  type ActionPlanningProvider,
  type StateMergeOutput,
  type StateMergeProvider,
  type WorkflowPlanningContext,
} from "./workflow.providers.js";
import { WorkflowProviderError } from "./workflow.errors.js";
import { ACTION_SYSTEM_PROMPT } from "../prompts/action-planning.prompt.js";

let client: OpenAI | undefined;

export class OpenAIActionPlanningProvider
  implements ActionPlanningProvider
{
  async planActions(
    context: WorkflowPlanningContext
  ): Promise<ActionPlanningOutput> {
    try {
      const response = await getClient().responses.parse({
        model:
          process.env.OPENAI_ACTION_MODEL ||
          process.env.OPENAI_TEXT_MODEL ||
          "gpt-5.4-nano",
        instructions: ACTION_SYSTEM_PROMPT,
        input: JSON.stringify(buildActionPlanningContext(context)),
        text: {
          format: zodTextFormat(
            ActionPlanningOutputSchema,
            "mucho_action_plan"
          ),
        },
      });

      if (!response.output_parsed) {
        throw new Error("The model returned no structured action plan.");
      }
      return response.output_parsed;
    } catch (error) {
      if (error instanceof ZodError) {
        return {
          actions: [],
          unresolvedIntents: [
            {
              summary: "Invalid structured action output",
              reason: error.message,
              evidenceIds: [],
            },
          ],
        };
      }
      throw new WorkflowProviderError(
        `Action planning failed: ${error instanceof Error ? error.message : String(error)}`,
        { cause: error }
      );
    }
  }
}

export class OpenAIStateMergeProvider implements StateMergeProvider {
  async mergeStates(
    context: WorkflowPlanningContext
  ): Promise<StateMergeOutput> {
    try {
      const response = await getClient().responses.parse({
        model:
          process.env.OPENAI_STATE_MODEL ||
          process.env.OPENAI_TEXT_MODEL ||
          "gpt-5.4-nano",
        instructions: STATE_SYSTEM_PROMPT,
        input: JSON.stringify(buildStateModelContext(context)),
        text: {
          format: zodTextFormat(
            StateMergeOutputSchema,
            "mucho_state_merge_plan"
          ),
        },
      });

      if (!response.output_parsed) {
        throw new Error("The model returned no structured state plan.");
      }
      return response.output_parsed;
    } catch (error) {
      if (error instanceof ZodError) {
        return { groups: [] };
      }
      throw new WorkflowProviderError(
        `State merging failed: ${error instanceof Error ? error.message : String(error)}`,
        { cause: error }
      );
    }
  }
}

function getClient(): OpenAI {
  const apiKey = process.env.OPENAI_API_KEY;
  if (!apiKey) {
    throw new Error("OPENAI_API_KEY is not configured.");
  }
  client ??= new OpenAI({ apiKey });
  return client;
}

function buildStateModelContext(context: WorkflowPlanningContext): unknown {
  return {
    transcript: context.request.transcript,
    chapters: context.chapters,
    scene: context.request.scene,
    timeline: context.request.timeline,
    statePlaceholders: context.request.statePlaceholders,
    forceVectorCandidates: context.request.forceVectorCandidates,
  };
}

const STATE_SYSTEM_PROMPT = `
You are the Mucho interaction-state merger. The action timeline is complete.
Group every supplied state placeholder exactly once, preserving order.

Rules:
- A group may contain only adjacent placeholder IDs.
- Do not skip, duplicate, or reorder placeholders.
- Prefer a small number of semantically meaningful interaction phases.
- Use short natural names such as idle, drag, release, and hit.
- Base merging on transcript intent, timeline actions, gestures, collisions,
  and chapters.
- Return rationale and confidence for every group.
`.trim();
