# Database and Migrations

## Database engine

- PostgreSQL 17 in Docker Compose

## EF Core location

- DbContext: `server/server/Date/AppDbContext.cs`
- Design-time factory: `server/server/Date/AppDbContextFactory.cs`
- Migrations: `server/server/Migrations/`

## Startup migration behavior

The API runs `Database.Migrate()` automatically during startup and retries transient database connection failures before failing the process.

This means:

- `docker compose up -d` is enough for normal deployments
- a separate migration container is not required for this repo

## Manual apply

```powershell
$env:PG_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=crypto_tracker"
dotnet ef database update --project server/server/server.csproj --startup-project server/server/server.csproj
```

## Create a new migration

```powershell
$env:PG_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=crypto_tracker"
dotnet ef migrations add <MigrationName> --project server/server/server.csproj --startup-project server/server/server.csproj
```

## Remove the last migration

```powershell
$env:PG_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=crypto_tracker"
dotnet ef migrations remove --project server/server/server.csproj --startup-project server/server/server.csproj
```

## Data stored in PostgreSQL

- `Users`
- `Session`
- `RefreshToken`
- `Transactions`
- `WalletSnapshots`
- `CryptoPriceSnapshots`
