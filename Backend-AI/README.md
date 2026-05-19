# Mucho AI Backend

TypeScript/Express prototype for proposing Mucho timeline actions from a
recording payload plus transcript.

## Install

```bash
npm install
```

## Build

```bash
npm run build
```

Build output is written to `dist/`.

## Test

```bash
npm test -- --run
```

## Run

```bash
npm run dev
```

or after building:

```bash
npm start
```

## Endpoints

- `GET /api/v1/health`
- `POST /api/v1/effects/propose`
- `POST /api/v1/audio/transcribe`
- `GET /dev`

See `backend_status.md` for the detailed current status and implementation
notes.
