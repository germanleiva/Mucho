export function renderDeveloperConsolePage(
  defaultTranscript: string
): string {
  return `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Mucho AI Backend - Developer Console</title>
  <style>
    * { box-sizing: border-box; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      margin: 0;
      padding: 20px;
      background: #f5f5f5;
    }
    .container { max-width: 1400px; margin: 0 auto; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
    .panel {
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      padding: 20px;
    }
    textarea {
      width: 100%;
      height: 400px;
      padding: 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-family: 'Monaco', 'Menlo', monospace;
      font-size: 12px;
      resize: vertical;
    }
    button {
      background: #0066cc;
      color: white;
      border: none;
      padding: 10px 20px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      margin-top: 10px;
      width: 100%;
    }
    .controls { display: flex; gap: 10px; margin-bottom: 15px; }
    .controls button { margin-top: 0; flex: 1; padding: 8px 16px; }
    .small-button { background: #666; }
    .status { margin-top: 10px; padding: 10px; border-radius: 4px; font-size: 12px; }
    .loading { background: #e3f2fd; color: #1976d2; }
    .success { background: #e8f5e9; color: #388e3c; }
    .error { background: #ffebee; color: #c62828; }
    #response { resize: none; background: #f9f9f9; }
  </style>
</head>
<body>
  <div class="container">
    <h1>Mucho AI Backend - Developer Console</h1>
    <div class="row">
      <div class="panel">
        <h2>Input: JSON Payload</h2>
        <div class="controls">
          <button class="small-button" onclick="loadExample()">Load example</button>
          <button class="small-button" onclick="clearInput()">Clear input</button>
          <button class="small-button" onclick="clearOutput()">Clear output</button>
        </div>
        <textarea id="input" placeholder="Paste your JSON request here..."></textarea>
        <div style="margin-top:10px">
          <h3 style="margin:6px 0 8px 0">Mock voice transcript</h3>
          <textarea id="transcript" style="height:120px" placeholder="Paste or edit a mock transcript here..."></textarea>
        </div>
        <button id="proposeBtn" onclick="sendRequest()">Propose effects</button>
        <div id="inputStatus" class="status" style="display: none;"></div>
      </div>
      <div class="panel">
        <h2>Output: Backend Response</h2>
        <textarea id="response" readonly placeholder="Backend response will appear here..."></textarea>
      </div>
    </div>
  </div>

  <script>
    const defaultMockTranscript = ${JSON.stringify(defaultTranscript)};
    const examples = {
      basketball: {
        "projectId": "demo-project",
        "recordingId": "recording-001",
        "languageHint": "auto",
        "recording": {
          "frameRate": 60,
          "durationFrames": 420,
          "audioStartFrame": 0,
          "recordingMode": "voice_during_miming"
        },
        "useMockTranscript": true,
        "scene": {
          "assets": [
            { "assetName": "Basketball(Clone)", "displayName": "Basketball", "assetKind": "basketball" },
            { "assetName": "Cube(Clone)", "displayName": "Cube", "assetKind": "cube" },
            { "assetName": "HitText(Clone)", "displayName": "Hit Text", "assetKind": "text" }
          ],
          "contextObjects": [
            { "objectName": "RIGHTHAND", "displayName": "Right Hand", "objectKind": "rightHand" },
            { "objectName": "RIGHTFOCUS", "displayName": "Right Focus", "objectKind": "rightFocus" },
            { "objectName": "GAZEFOCUS", "displayName": "Gaze Focus", "objectKind": "gazeFocus" }
          ]
        },
        "sequences": [
          {
            "sequenceId": "gesture-001",
            "sequenceKind": "gesture",
            "startFrame": 40,
            "length": 55,
            "rawGesture": "Gesture.RIGHTHANDPINCH",
            "gestureLabel": "Pinch",
            "hand": "right"
          },
          {
            "sequenceId": "gesture-002",
            "sequenceKind": "gesture",
            "startFrame": 96,
            "length": 34,
            "rawGesture": "Gesture.RIGHTHANDOPEN",
            "gestureLabel": "Open",
            "hand": "right"
          },
          {
            "sequenceId": "collision-ball-cube",
            "sequenceKind": "collision",
            "startFrame": 210,
            "length": 8,
            "objectAName": "Basketball(Clone)",
            "objectBName": "Cube(Clone)"
          },
          {
            "sequenceId": "existing-show-ball",
            "sequenceKind": "existingAction",
            "startFrame": 0,
            "length": 420,
            "actionType": "show",
            "targetAssetName": "Basketball(Clone)",
            "params": {},
            "source": "default"
          },
          {
            "sequenceId": "existing-show-text",
            "sequenceKind": "existingAction",
            "startFrame": 0,
            "length": 420,
            "actionType": "show",
            "targetAssetName": "HitText(Clone)",
            "params": {},
            "source": "default"
          }
        ],
        "contextObservations": [],
        "options": {
          "confidenceThreshold": 0.85,
          "includeDebugChapters": true,
          "allowDefaultTextHide": true
        }
      }
    };

    function loadExample(name) {
      const example = name ? examples[name] : examples.basketball;
      document.getElementById('input').value = JSON.stringify(example, null, 2);
      document.getElementById('transcript').value = defaultMockTranscript;
    }

    function clearInput() {
      document.getElementById('input').value = '';
      document.getElementById('inputStatus').style.display = 'none';
    }

    function clearOutput() {
      document.getElementById('response').value = '';
      document.getElementById('inputStatus').style.display = 'none';
    }

    async function sendRequest() {
      const statusEl = document.getElementById('inputStatus');
      const responseEl = document.getElementById('response');
      let payload;

      try {
        payload = JSON.parse(document.getElementById('input').value);
      } catch (_error) {
        statusEl.className = 'status error';
        statusEl.textContent = 'Invalid JSON';
        statusEl.style.display = 'block';
        responseEl.value = '';
        return;
      }

      payload.transcript = {
        text: document.getElementById('transcript').value || '',
        language: 'en',
        segments: []
      };

      statusEl.className = 'status loading';
      statusEl.textContent = 'Sending request...';
      statusEl.style.display = 'block';

      try {
        const response = await fetch('/api/v1/effects/propose', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });
        const text = await response.text();
        let parsed = null;
        try { parsed = JSON.parse(text); } catch (_error) {}
        responseEl.value = parsed ? JSON.stringify(parsed, null, 2) : text;
        statusEl.className = response.ok ? 'status success' : 'status error';
        statusEl.textContent = response.ok
          ? 'Request successful'
          : 'Request failed: ' + response.status + ' ' + response.statusText;
      } catch (error) {
        statusEl.className = 'status error';
        statusEl.textContent = 'Network error: ' + (error && error.message ? error.message : String(error));
        responseEl.value = '';
      }
    }

    document.addEventListener('DOMContentLoaded', () => loadExample('basketball'));
  </script>
</body>
</html>
  `;
}
