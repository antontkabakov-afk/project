# Production Deployment

## Recommended approach

Use the provided Docker Compose stack as the baseline deployment. It is the simplest robust path for this repository because it includes:

- PostgreSQL
- Automatic API migrations on startup
- Frontend reverse proxying for `/api` and `/health`
- Healthchecked startup order

## Pre-deploy configuration

1. Copy `.env.example` to `.env`.
2. Replace `POSTGRES_PASSWORD`.
3. Replace `JWT_ACCESS_SECRET` with a long random value of at least 32 bytes.
4. Set `MORALIS_API_KEY` if wallet features must work.
5. Set `CLIENT_ORIGINS` to the real public frontend origin.
6. Set `AUTH_COOKIE_SECURE=true` when the public site is behind HTTPS.
7. If frontend and backend are on different sites, set `AUTH_COOKIE_SAME_SITE=None` and keep `AUTH_COOKIE_SECURE=true`.

## Build

```powershell
docker compose build --pull
```

## Run

```powershell
docker compose up -d
```

## Validate after deployment

```powershell
docker compose ps
docker compose logs --tail=200 api
docker compose logs --tail=200 web
```

Check:

- `http://<host>/health/live`
- `http://<host>/health/ready`
- frontend page load
- register/login/logout
- wallet connect
- assets/history/statistics pages

## Manual backend build path

If you need non-container build artifacts:

```powershell
dotnet publish server/server/server.csproj -c Release -o .artifacts/server
Set-Location client
npm ci
npm run build
Set-Location ..
```

## Go-live checklist

- `.env` exists on the deployment host
- `POSTGRES_PASSWORD` and `JWT_ACCESS_SECRET` are rotated from placeholders
- `AUTH_COOKIE_SECURE=true` for HTTPS
- `CLIENT_ORIGINS` matches the public frontend origin
- database volume backups are configured
- health endpoints return `200`
- registration/login/logout are verified manually
- wallet sync works with the configured Moralis key
- logs are clean after startup
