export class WorkflowSemanticError extends Error {
  constructor(
    message: string,
    public readonly details: string[] = [message]
  ) {
    super(message);
    this.name = "WorkflowSemanticError";
  }
}

export class WorkflowProviderError extends Error {
  public readonly cause?: unknown;

  constructor(message: string, options?: { cause?: unknown }) {
    super(message);
    this.name = "WorkflowProviderError";
    this.cause = options?.cause;
  }
}
