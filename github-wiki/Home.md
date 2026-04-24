# Crypto Tracker Wiki

Crypto Tracker is a three-service deployment:

- `web`: Nginx serving the built React frontend from `client/dist`
- `api`: ASP.NET Core API from `server/server/`
- `db`: PostgreSQL 17 for users, sessions, refresh tokens, price snapshots, and wallet snapshots

Service flow:

- Browser requests hit the frontend on `http://localhost:8080`
- Nginx proxies `/api` and `/health` to the API container
- The API talks to PostgreSQL and external CoinGecko / Moralis APIs

Pages:

- [Installation](Installation.md)
- [Environment Variables](Environment-Variables.md)
- [Local Development](Local-Development.md)
- [Docker Deployment](Docker-Deployment.md)
- [Production Deployment](Production-Deployment.md)
- [Database and Migrations](Database-and-Migrations.md)
- [Project Structure](Project-Structure.md)
- [Troubleshooting](Troubleshooting.md)
- [API Overview](API-Overview.md)
- [Frontend Overview](Frontend-Overview.md)
- [Authentication](Authentication.md)

Go-live checklist:

- Copy `.env.example` to `.env` and replace all placeholder secrets
- Set `POSTGRES_PASSWORD` and a strong `JWT_ACCESS_SECRET` of at least 32 bytes
- Set `MORALIS_API_KEY` if wallet tracking must work in production
- Set `CLIENT_ORIGINS` to the real public frontend origin
- Set `AUTH_COOKIE_SECURE=true` when traffic is served over HTTPS
- Run `docker compose build --pull`
- Run `docker compose up -d`
- Check `http://<host>/health/live` and `http://<host>/health/ready`
- Confirm registration, login, logout, wallet connect, asset history, and statistics work end to end
