# Installation

## Prerequisites

- Node.js 22+
- npm 10+
- .NET 10 SDK
- Docker Desktop / Docker Engine with Compose
- Optional: `dotnet-ef` for manual migration commands

Install EF CLI if it is not already available:

```powershell
dotnet tool install --global dotnet-ef
```

## Clone and bootstrap

```powershell
git clone <your-repo-url>
Set-Location crypto-tracker
Copy-Item .env.example .env
Copy-Item client/.env.example client/.env
```

Install frontend dependencies:

```powershell
Set-Location client
npm ci
Set-Location ..
```

Restore backend dependencies:

```powershell
dotnet restore server/server/server.csproj
```

## Required configuration before running

- Set `POSTGRES_PASSWORD` in `.env`
- Set `JWT_ACCESS_SECRET` in `.env` to at least 32 bytes
- Set `MORALIS_API_KEY` in `.env` if you need wallet sync against Moralis

Useful next pages:

- [Environment Variables](Environment-Variables.md)
- [Local Development](Local-Development.md)
- [Docker Deployment](Docker-Deployment.md)
