export function renderChapterTestPage(defaultTranscript: string): string {
  return `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Chaptering Test</title>
  <style>
    * { box-sizing: border-box; }
    body {
      margin: 0;
      padding: 24px;
      background: #f5f5f5;
      color: #222;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    }
    main { max-width: 1000px; margin: 0 auto; }
    .panel {
      margin-top: 16px;
      padding: 20px;
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }
    textarea {
      width: 100%;
      min-height: 220px;
      padding: 12px;
      border: 1px solid #ccc;
      border-radius: 4px;
      font: 14px/1.5 'Monaco', 'Menlo', monospace;
      resize: vertical;
    }
    .settings {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 180px));
      gap: 12px;
      margin: 12px 0;
    }
    label { display: grid; gap: 5px; font-size: 13px; font-weight: 600; }
    input {
      padding: 8px;
      border: 1px solid #ccc;
      border-radius: 4px;
    }
    button {
      padding: 10px 18px;
      border: 0;
      border-radius: 4px;
      background: #0066cc;
      color: white;
      cursor: pointer;
      font-size: 14px;
    }
    button:disabled { opacity: 0.6; cursor: wait; }
    #status { margin-left: 10px; font-size: 13px; }
    .chapter {
      margin-top: 10px;
      padding: 12px;
      border-left: 4px solid #0066cc;
      background: #f7f9fc;
    }
    .frames { color: #555; font-size: 12px; margin-bottom: 5px; }
    .empty { color: #666; }
  </style>
</head>
<body>
  <main>
    <h1>Chaptering Test</h1>
    <p>Write a transcript below. Sentences and line breaks create candidate chapters, with frame time distributed by word count.</p>
    <section class="panel">
      <textarea id="chapterText" placeholder="Write or paste a transcript...">${escapeHtml(defaultTranscript)}</textarea>
      <div class="settings">
        <label>
          Frame rate
          <input id="frameRate" type="number" min="1" step="1" value="30">
        </label>
        <label>
          Duration in frames
          <input id="durationFrames" type="number" min="1" step="1" value="300">
        </label>
      </div>
      <button id="testButton" type="button">Extract chapters</button>
      <span id="status"></span>
    </section>
    <section class="panel">
      <h2>Chapters</h2>
      <div id="chapters" class="empty">Run the test to see chapter results.</div>
    </section>
  </main>
  <script>
    const button = document.getElementById('testButton');
    const status = document.getElementById('status');
    const output = document.getElementById('chapters');

    function renderChapters(chapters) {
      output.className = '';
      output.replaceChildren();

      if (chapters.length === 0) {
        output.className = 'empty';
        output.textContent = 'No chapters found.';
        return;
      }

      chapters.forEach((chapter, index) => {
        const element = document.createElement('article');
        element.className = 'chapter';

        const frames = document.createElement('div');
        frames.className = 'frames';
        frames.textContent =
          'Chapter ' + (index + 1) + ': frames ' +
          chapter.frameStart + ' to ' + chapter.frameEnd;

        const summary = document.createElement('div');
        summary.textContent = chapter.summary;

        element.append(frames, summary);
        output.appendChild(element);
      });
    }

    async function testChaptering() {
      button.disabled = true;
      status.textContent = 'Extracting...';

      try {
        const response = await fetch('/api/v1/chapters/test', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            text: document.getElementById('chapterText').value,
            frameRate: Number(document.getElementById('frameRate').value),
            durationFrames: Number(document.getElementById('durationFrames').value)
          })
        });
        const result = await response.json();

        if (!response.ok) {
          throw new Error(result.error || 'Request failed');
        }

        renderChapters(result.chapters);
        status.textContent = result.chapters.length + ' chapter(s) found';
      } catch (error) {
        output.className = 'empty';
        output.textContent = error && error.message ? error.message : String(error);
        status.textContent = 'Failed';
      } finally {
        button.disabled = false;
      }
    }

    button.addEventListener('click', testChaptering);
  </script>
</body>
</html>
  `;
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
