# Docker Deployment

## Files involved

- `docker-compose.yml`
- `client/Dockerfile`
- `client/nginx.conf`
- `server/server/Dockerfile`
- `.env.example`

## Build the stack

```powershell
Copy-Item .env.example .env
docker compose build
```

## Run the stack

```powershell
docker compose up -d
```

## Inspect the stack

```powershell
docker compose ps
docker compose logs -f api
docker compose logs -f web
docker compose logs -f db
```

## Stop the stack

```powershell
docker compose down
```

## Container networking

- `web` reaches the API through `http://api:8080`
- `api` reaches PostgreSQL through `Host=db;Port=5432`
- PostgreSQL is persisted in the named volume `postgres_data`

## Default ports

- Frontend: `http://localhost:8080`
- API: `http://localhost:8081`
- Direct API health: `http://localhost:8081/health/live`
- Proxied health: `http://localhost:8080/health/live`

## Notes

- The frontend image is multi-stage: Node build stage, Nginx runtime stage
- The backend image is multi-stage: .NET SDK build stage, ASP.NET runtime stage
- Both runtime images include healthchecks
