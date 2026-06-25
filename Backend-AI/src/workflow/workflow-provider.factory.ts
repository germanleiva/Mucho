import {
  MockActionPlanningProvider,
  MockStateMergeProvider,
} from "./mock-workflow.providers.js";
import {
  OpenAIActionPlanningProvider,
  OpenAIStateMergeProvider,
} from "./openai-workflow.providers.js";
import type { WorkflowProviders } from "./iterate-workflow.js";

export function createWorkflowProviders(): WorkflowProviders {
  if (process.env.WORKFLOW_AI_PROVIDER === "mock") {
    return {
      actionProvider: new MockActionPlanningProvider(),
      stateProvider: new MockStateMergeProvider(),
    };
  }

  return {
    actionProvider: new OpenAIActionPlanningProvider(),
    stateProvider: new OpenAIStateMergeProvider(),
  };
}
