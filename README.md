# Crypto Tracker

Crypto Tracker is a full-stack portfolio dashboard for tracking one wallet per account, storing immutable portfolio snapshots, and visualizing live crypto market pricing alongside snapshot-based history.

The app runs as three services:

- `web`: React 18 + Vite, served by Nginx
- `api`: ASP.NET Core + EF Core + JWT cookie auth
- `db`: PostgreSQL 17

The current UI is optimized around a 30-day default view for history and statistics, with an `All time` fallback when you want the full archive.

## What The App Does

- Registers and authenticates users with HTTP-only auth cookies
- Connects one wallet address per account
- Stores append-only wallet snapshots in PostgreSQL
- Shows month-first portfolio history and statistics from stored snapshots
- Pulls supported crypto market prices from the backend
- Runs locally through one Docker Compose stack at `http://localhost:8080`

## Architecture

| Service | Path | Purpose | Default exposed port |
| --- | --- | --- | --- |
| Frontend | `client/` | React SPA rendered by Nginx | `8080` |
| API | `server/server/` | ASP.NET Core API, auth, pricing, snapshots | `8081` |
| Database | Compose service | PostgreSQL persistence | internal only |

Routing rules:

- The browser talks to `/api`
- In development, Vite proxies `/api` to the backend
- In containers, Nginx proxies `/api` and `/health` to the API container

## Requirements

- Node.js 22+
- npm 10+
- .NET 10 SDK
- Docker Desktop or Docker Engine with Compose
- Optional: `dotnet-ef` for manual migration commands

Install EF tooling if needed:

```powershell
dotnet tool install --global dotnet-ef
```

## Environment Setup

Create the root deployment file:

```powershell
Copy-Item .env.example .env
```

Optional frontend development env file:

```powershell
Copy-Item client/.env.example client/.env
```

Important variables:

- `POSTGRES_PASSWORD`
- `JWT_ACCESS_SECRET`
- `MORALIS_API_KEY` if wallet hydration is required
- `COINGECKO_DEMO_API_KEY` if you want an explicit CoinGecko key instead of unauthenticated traffic
- `CRYPTO_PRICE_BACKFILL_DAYS` controls how much market history the API backfills on startup

Cookie settings:

- Keep `AUTH_COOKIE_SECURE=false` for local HTTP Docker usage
- Set `AUTH_COOKIE_SECURE=true` for HTTPS deployments
- Leave `AUTH_COOKIE_DOMAIN=` empty for local `localhost`
- Use `AUTH_COOKIE_SAME_SITE=None` only for cross-site HTTPS setups

## Quick Start With Docker

Build and start the full stack:

```powershell
docker compose up --build -d
```

Open:

- Frontend: `http://localhost:8080`
- Direct API: `http://localhost:8081`
- Proxied API health: `http://localhost:8080/health/live`
- Direct API health: `http://localhost:8081/health/live`

Stop the stack:

```powershell
docker compose down
```

Rebuild after code changes:

```powershell
docker compose up --build -d
```

## Local Development

Start PostgreSQL first. The fastest option is the compose database:

```powershell
$env:POSTGRES_PASSWORD="postgres"
$env:JWT_ACCESS_SECRET="local-dev-jwt-secret-change-me-1234567890"
docker compose up -d db
```

Apply migrations manually if needed:

```powershell
$env:PG_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=crypto_tracker"
dotnet ef database update --project server/server/server.csproj --startup-project server/server/server.csproj
```

Run the API:

```powershell
dotnet run --project server/server/server.csproj --launch-profile http
```

Run the frontend:

```powershell
Set-Location client
npm ci
npm run dev
```

Local URLs:

- Frontend dev: `http://localhost:5173`
- API dev: `http://localhost:5247`
- API health: `http://localhost:5247/health/live`

## Data Flow Notes

### Wallet snapshots

- Wallet history and statistics are backed by stored `WalletSnapshots`
- History and statistics default to the last 30 days in the UI
- `All time` remains available for the full stored record

### Market prices

- The API pulls supported crypto prices through CoinGecko
- The supported asset set is controlled by `COINGECKO_SUPPORTED_COINS`
- On startup, the API backfills stored crypto market snapshots for the last 30 days by default
- If CoinGecko is temporarily unavailable, the API keeps the app alive and falls back as cleanly as possible

Current backend behavior:

- Background snapshot capture logs upstream CoinGecko failures instead of crashing the API
- The asset list can still render even if there is no stored price snapshot yet

### Authentication

- Registration creates the user, session, refresh token, and auth cookies in one retriable transaction
- Login issues new cookies and a new session
- Refresh returns `401` before login when no refresh cookie exists; the frontend treats that as a normal unauthenticated state

## Database Migrations

The API automatically runs `Database.Migrate()` on startup with retry logic, so the normal Docker flow does not need a dedicated migration container.

Manual migration commands:

```powershell
$env:PG_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=crypto_tracker"
dotnet ef database update --project server/server/server.csproj --startup-project server/server/server.csproj
```

Create a new migration:

```powershell
$env:PG_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=crypto_tracker"
dotnet ef migrations add <MigrationName> --project server/server/server.csproj --startup-project server/server/server.csproj
```

## Troubleshooting

- `docker compose config` fails with missing variables:
  copy `.env.example` to `.env` and set `POSTGRES_PASSWORD` and `JWT_ACCESS_SECRET`
- Login or signup appears successful but the browser is not authenticated:
  keep `AUTH_COOKIE_SECURE=false` on local HTTP and leave `AUTH_COOKIE_DOMAIN=` empty
- Wallet connection returns `Moralis API key is not configured.`:
  set `MORALIS_API_KEY`
- Wallet connection returns a Moralis validation error:
  verify the wallet address and selected chain
- Asset prices show fallback values or stale data:
  inspect `docker compose logs api` for CoinGecko errors and add `COINGECKO_DEMO_API_KEY` if needed
- The frontend cannot reach the backend in dev:
  confirm the API is running on `http://localhost:5247` or update `client/.env`
- The API exits during startup:
  verify `PG_CONNECTION` or the Compose database credentials, then inspect `docker compose logs api`
- Docker commands fail entirely:
  ensure the Docker daemon is running locally

## Verification

Verified in this repo during the latest pass:

- `dotnet build` in `server/server/`
- `npm run build` in `client/`
- `docker compose up --build -d`
- registration, refresh, login, and wallet endpoint checks against the running stack
- `GET /api/crypto/assets` returning live prices through the containerized app

## Repository Docs

- Wiki-style notes live in [`github-wiki/`](github-wiki/)
- Frontend-specific notes live in [`client/README.md`](client/README.md)
