import express, { Express, Request, Response } from "express";
import { z, ZodError } from "zod";
import { analyzeRecording } from "./graph/analyzeRecording.graph.js";
import { extractChapters } from "./graph/nodes/extractChapters.node.js";
import { initializeState } from "./graph/state.js";
import { renderChapterTestPage } from "./html/chapter-test.page.js";
import { renderDeveloperConsolePage } from "./html/developer-console.page.js";
import { AnalyzeRecordingRequestSchema } from "./schemas/request.schema.js";
import { transcribeAudio } from "./services/transcription.service.js";
import { iterateWorkflow } from "./workflow/iterate-workflow.js";
import {
  WorkflowProviderError,
  WorkflowSemanticError,
} from "./workflow/workflow.errors.js";
import { createWorkflowProviders } from "./workflow/workflow-provider.factory.js";
import {
  WorkflowIterationRequestSchema,
  type WorkflowIterationRequest,
  type WorkflowIterationResponse,
} from "./workflow/workflow.schema.js";

const app: Express = express();

const mockTranscriptText =
  "So, when I pinch the ball, I want the ball to follow my hand. And then, I am aiming at the lamp. And when I open my hand and I set the word jump, I expect the ball to go from my hand and hit the lamp. Because I don't have a trash bin ball, whatever. So...";

const ChapterTestRequestSchema = z.object({
  text: z.string().trim().min(1),
  frameRate: z.number().positive().default(30),
  durationFrames: z.number().int().positive().default(300),
});

function normalizeRequestBody(body: unknown): unknown {
  if (!body || typeof body !== "object" || Array.isArray(body)) {
    return body;
  }

  const payload = body as Record<string, unknown>;

  if (!payload.transcript && payload.mockTranscript) {
    return {
      ...payload,
      transcript: payload.mockTranscript,
    };
  }

  return body;
}

app.use(
  "/api/v1/audio/transcribe",
  express.raw({
    type: ["application/octet-stream", "audio/*", "video/mp4"],
    limit: "25mb",
  })
);
app.use(express.json({ limit: "10mb" }));
app.use(express.static("public"));

app.get("/api/v1/health", (_req: Request, res: Response) => {
  res.json({ status: "ok" });
});

app.post(
  "/api/v1/workflows/iterate",
  async (req: Request, res: Response): Promise<void> => {
    let request: WorkflowIterationRequest;
    try {
      request = WorkflowIterationRequestSchema.parse(req.body);
    } catch (error) {
      res.status(400).json({
        error: "Invalid workflow request format",
        details: error instanceof ZodError ? error.errors : [String(error)],
      });
      return;
    }

    try {
      const response = await iterateWorkflow(
        request,
        createWorkflowProviders()
      );
      res.json(response);
    } catch (error) {
      if (error instanceof WorkflowSemanticError) {
        res.status(422).json({
          error: error.message,
          details: error.details,
        });
        return;
      }

      if (error instanceof WorkflowProviderError || error instanceof ZodError) {
        res.status(502).json(
          failedWorkflowResponse(
            request,
            error instanceof Error ? error.message : String(error)
          )
        );
        return;
      }

      res.status(500).json({
        error: "Workflow iteration failed",
        message: error instanceof Error ? error.message : String(error),
      });
    }
  }
);

app.post("/api/v1/chapters/test", (req: Request, res: Response) => {
  try {
    const input = ChapterTestRequestSchema.parse(req.body);
    const request = AnalyzeRecordingRequestSchema.parse({
      recordingId: "chapter-test",
      recording: {
        frameRate: input.frameRate,
        durationFrames: input.durationFrames,
        recordingMode: "voice_during_miming",
      },
      transcript: {
        text: input.text,
        language: "unknown",
        segments: [],
      },
      scene: {
        assets: [],
        contextObjects: [],
      },
      sequences: [],
      contextObservations: [],
      options: {},
    });
    const state = initializeState(request, "chapter-test");
    state.transcript = request.transcript ?? null;
    const result = extractChapters(state);

    res.json({
      text: input.text,
      frameRate: input.frameRate,
      durationFrames: input.durationFrames,
      chapters: result.chapters,
    });
  } catch (error) {
    if (error instanceof ZodError) {
      res.status(400).json({
        error: "Invalid chapter test request",
        details: error.errors,
      });
      return;
    }

    res.status(500).json({
      error: "Chapter extraction failed",
      message: error instanceof Error ? error.message : String(error),
    });
  }
});

app.post("/api/v1/effects/propose", async (req: Request, res: Response) => {
  try {
    const request = AnalyzeRecordingRequestSchema.parse(
      normalizeRequestBody(req.body)
    );
    const response = await analyzeRecording(request);
    res.json(response);
  } catch (error) {
    if (error instanceof ZodError) {
      res.status(400).json({
        error: "Invalid request format",
        details: error.errors,
      });
      return;
    }

    res.status(500).json({
      error: "Internal server error",
      message: error instanceof Error ? error.message : String(error),
    });
  }
});

app.post(
  "/api/v1/audio/transcribe",
  async (req: Request, res: Response): Promise<void> => {
    try {
      const audioBytes = req.body;

      if (!Buffer.isBuffer(audioBytes) || audioBytes.length === 0) {
        res.status(400).json({ error: "Missing raw audio bytes." });
        return;
      }

      const languageHeader = req.header("x-audio-language");
      const language =
        languageHeader && /^[a-z]{2}$/i.test(languageHeader)
          ? languageHeader.toLowerCase()
          : undefined;
      const mimeType = normalizeAudioMimeType(req.header("content-type"));
      const filename = getAudioFilename(
        req.header("x-audio-filename"),
        mimeType,
        audioBytes
      );
      const transcript = await transcribeAudio({
        bytes: audioBytes,
        filename,
        mimeType,
        language,
      });

      res.json({
        transcript,
      });
    } catch (error) {
      const missingApiKey =
        error instanceof Error &&
        error.message === "OPENAI_API_KEY is not configured.";
      res.status(missingApiKey ? 503 : 502).json({
        error: "Audio transcription failed.",
        message: error instanceof Error ? error.message : String(error),
      });
    }
  }
);

app.get("/chapters", (_req: Request, res: Response) => {
  res.type("html").send(renderChapterTestPage(mockTranscriptText));
});

app.get("/dev", (_req: Request, res: Response) => {
  res.type("html").send(renderDeveloperConsolePage(mockTranscriptText));
});

function normalizeAudioMimeType(contentType?: string): string | undefined {
  const mimeType = contentType?.split(";", 1)[0].trim().toLowerCase();
  return mimeType && mimeType !== "application/octet-stream"
    ? mimeType
    : undefined;
}

function getAudioFilename(
  requestedFilename: string | undefined,
  mimeType: string | undefined,
  audioBytes: Buffer
): string {
  const safeFilename = requestedFilename
    ?.split(/[\\/]/)
    .pop()
    ?.replace(/[^a-zA-Z0-9._-]/g, "_");

  if (safeFilename && /\.[a-z0-9]+$/i.test(safeFilename)) {
    return safeFilename;
  }

  return `recording.${detectAudioExtension(mimeType, audioBytes)}`;
}

function detectAudioExtension(
  mimeType: string | undefined,
  audioBytes: Buffer
): string {
  const extensionsByMimeType: Record<string, string> = {
    "audio/m4a": "m4a",
    "audio/mp4": "mp4",
    "audio/mpeg": "mp3",
    "audio/ogg": "ogg",
    "audio/wav": "wav",
    "audio/webm": "webm",
    "audio/x-m4a": "m4a",
    "audio/x-wav": "wav",
    "video/mp4": "mp4",
  };

  if (mimeType && extensionsByMimeType[mimeType]) {
    return extensionsByMimeType[mimeType];
  }
  if (audioBytes.subarray(0, 4).toString("ascii") === "RIFF") {
    return "wav";
  }
  if (audioBytes.subarray(0, 3).toString("ascii") === "ID3") {
    return "mp3";
  }
  if (
    audioBytes.length >= 4 &&
    audioBytes[0] === 0x1a &&
    audioBytes[1] === 0x45 &&
    audioBytes[2] === 0xdf &&
    audioBytes[3] === 0xa3
  ) {
    return "webm";
  }

  return "wav";
}

function failedWorkflowResponse(
  request: WorkflowIterationRequest,
  message: string
): WorkflowIterationResponse {
  return {
    workflowId: request.workflowId,
    iteration: request.iteration,
    workflowStatus: "failed",
    actionsComplete: false,
    nextStep: "none",
    chapters: [],
    timelinePatch: { operations: [] },
    statePlan: null,
    warnings: [
      {
        code: "PROVIDER_FAILURE",
        message,
      },
    ],
    unresolvedIntents: [],
  };
}

export default app;
