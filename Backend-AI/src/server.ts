import app from "./app.js";

const PORT = process.env.PORT || 3000;

app.listen(PORT, () => {
  console.log(`Mucho AI Backend running on http://localhost:${PORT}`);
  console.log(`Developer console: http://localhost:${PORT}/dev`);
  console.log(`Health check: http://localhost:${PORT}/api/v1/health`);
  console.log(`Effects proposal: POST http://localhost:${PORT}/api/v1/effects/propose`);
});
