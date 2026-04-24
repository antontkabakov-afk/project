# Project Structure

```text
crypto-tracker/
├── client/
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── package.json
│   ├── package-lock.json
│   ├── src/
│   │   ├── api/
│   │   ├── components/
│   │   ├── pages/
│   │   └── lib/
│   └── public/
├── server/
│   ├── server.slnx
│   └── server/
│       ├── Controllers/
│       ├── Date/
│       ├── Migrations/
│       ├── Models/
│       ├── Service/
│       ├── Dockerfile
│       ├── Program.cs
│       └── server.csproj
├── github-wiki/
├── docker-compose.yml
├── .env.example
└── README.md
```

## Service relationships

- `client/` builds the SPA and serves it through Nginx
- `server/server/` hosts the API, auth flow, health endpoints, background snapshot services, and EF Core
- `docker-compose.yml` connects frontend, API, and PostgreSQL into one local production-style deployment

## Notable runtime files

- `client/src/api/client.ts`: shared API client with refresh-on-401 retry
- `client/src/components/require-auth.tsx`: protected route guard
- `server/server/Program.cs`: service registration, CORS, forwarded headers, migrations, health endpoints
- `server/server/Service/AuthCookieSettings.cs`: cookie behavior from env vars
- `server/server/Service/CorsSettings.cs`: allowed origins from env vars
