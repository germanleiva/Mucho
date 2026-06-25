import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { createServer, type Server } from "http";
import type { AddressInfo } from "net";
import app from "../app.js";

describe("Mucho AI Backend - HTTP API", () => {
  let server: Server;
  let baseUrl: string;

  beforeAll(async () => {
    server = createServer(app);
    await new Promise<void>((resolve) => {
      server.listen(0, "127.0.0.1", resolve);
    });
    const address = server.address() as AddressInfo;
    baseUrl = `http://127.0.0.1:${address.port}`;
  });

  afterAll(async () => {
    await new Promise<void>((resolve, reject) => {
      server.close((error) => {
        if (error) {
          reject(error);
          return;
        }
        resolve();
      });
    });
  });

  it("returns a readable zero-length warning through HTTP", async () => {
    const response = await fetch(`${baseUrl}/api/v1/effects/propose`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        recordingId: "http-zero-length",
        recording: {
          frameRate: 30,
          durationFrames: 120,
          recordingMode: "voice_during_miming",
        },
        useMockTranscript: true,
        scene: {
          assets: [
            {
              assetName: "Cube(Clone)",
              displayName: "Cube",
              assetKind: "cube",
            },
          ],
          contextObjects: [],
        },
        sequences: [
          {
            sequenceId: "seq_001",
            sequenceKind: "existingAction",
            startFrame: 0,
            length: 0,
            actionType: "show",
            targetAssetName: "Cube(Clone)",
          },
        ],
        options: {},
      }),
    });

    expect(response.status).toBe(200);
    const body = await response.json();

    expect(body.warnings).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          code: "ZERO_LENGTH_SEQUENCE",
          message:
            "Sequence seq_001 has length 0. Mucho actions and triggers are expected to have meaningful length.",
        }),
      ])
    );
  });

  it("normalizes legacy mockTranscript to canonical transcript", async () => {
    const response = await fetch(`${baseUrl}/api/v1/effects/propose`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        recordingId: "http-mock-transcript",
        recording: {
          frameRate: 30,
          durationFrames: 120,
          recordingMode: "voice_during_miming",
        },
        useMockTranscript: false,
        mockTranscript: {
          text: "Show the sphere",
          language: "en",
          segments: [],
        },
        scene: {
          assets: [
            {
              assetName: "Sphere(Clone)",
              displayName: "Sphere",
              assetKind: "sphere",
            },
          ],
          contextObjects: [],
        },
        sequences: [],
        options: {},
      }),
    });

    expect(response.status).toBe(200);
    const body = await response.json();

    expect(body.status).toBe("completed");
    expect(body.transcript).toEqual(
      expect.objectContaining({
        text: "Show the sphere",
        source: "provided",
      })
    );
  });

  it("wires the dev console textarea to payload.transcript", async () => {
    const response = await fetch(`${baseUrl}/dev`);
    const html = await response.text();

    expect(response.status).toBe(200);
    expect(html).toContain("payload.transcript =");
  });

  it("returns chapters from the chapter test endpoint", async () => {
    const response = await fetch(`${baseUrl}/api/v1/chapters/test`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        text: "Show the ball. Hide the cube",
        frameRate: 30,
        durationFrames: 120,
      }),
    });
    const body = await response.json();

    expect(response.status).toBe(200);
    expect(body.chapters).toEqual([
      expect.objectContaining({
        summary: "Show the ball",
        frameStart: 0,
        frameEnd: 60,
      }),
      expect.objectContaining({
        summary: "Hide the cube",
        frameStart: 60,
        frameEnd: 120,
      }),
    ]);
  });

  it("serves the chaptering test page", async () => {
    const response = await fetch(`${baseUrl}/chapters`);
    const html = await response.text();

    expect(response.status).toBe(200);
    expect(html).toContain('id="chapterText"');
    expect(html).toContain("/api/v1/chapters/test");
  });

  it("runs a workflow iteration through the mock provider", async () => {
    const previousProvider = process.env.WORKFLOW_AI_PROVIDER;
    process.env.WORKFLOW_AI_PROVIDER = "mock";

    try {
      const response = await fetch(`${baseUrl}/api/v1/workflows/iterate`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          workflowId: "http-workflow",
          iteration: 0,
          recording: {
            frameRate: 30,
            durationFrames: 100,
          },
          transcript: {
            text: "The interaction is already complete.",
            language: "en",
            segments: [],
          },
          scene: {
            assets: [],
            contextObjects: [],
          },
          timeline: {
            sequences: [],
          },
          statePlaceholders: [
            {
              placeholderId: "p0",
              frameStart: 0,
              frameEnd: 100,
            },
          ],
          forceVectorCandidates: [],
          options: {},
        }),
      });
      const body = await response.json();

      expect(response.status).toBe(200);
      expect(body).toEqual(
        expect.objectContaining({
          workflowStatus: "completed",
          actionsComplete: true,
          nextStep: "apply_state_plan",
        })
      );
    } finally {
      if (previousProvider === undefined) {
        delete process.env.WORKFLOW_AI_PROVIDER;
      } else {
        process.env.WORKFLOW_AI_PROVIDER = previousProvider;
      }
    }
  });
});
