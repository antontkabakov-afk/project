# Local Development

## Backend

Start only the database with Docker:

```powershell
$env:POSTGRES_PASSWORD="postgres"
$env:JWT_ACCESS_SECRET="local-dev-jwt-secret-change-me-1234567890"
docker compose up -d db
```

Set the EF connection string for migration commands:

```powershell
$env:PG_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=crypto_tracker"
```

Apply migrations:

```powershell
dotnet ef database update --project server/server/server.csproj --startup-project server/server/server.csproj
```

Run the API with the local launch profile:

```powershell
dotnet run --project server/server/server.csproj --launch-profile http
```

API URLs:

- `http://localhost:5247/api/...`
- `http://localhost:5247/health/live`
- `http://localhost:5247/health/ready`

## Frontend

The Vite config proxies `/api` and `/health` to `http://localhost:5247` by default.

```powershell
Set-Location client
npm ci
npm run dev
```

Frontend URL:

- `http://localhost:5173`

## Local behavior notes

- Protected routes are `/wallet`, `/assets`, `/history`, and `/statistics`
- Auth uses HTTP-only cookies issued by the backend
- If `MORALIS_API_KEY` is empty, wallet connect and snapshot calls will fail while the rest of the app can still boot
