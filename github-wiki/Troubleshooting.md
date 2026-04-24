# Troubleshooting

## `docker compose config` says a required variable is missing

- Copy `.env.example` to `.env`
- Set `POSTGRES_PASSWORD`
- Set `JWT_ACCESS_SECRET`

## Docker build or run fails because the daemon is unavailable

- Start Docker Desktop or the Docker Engine service
- Re-run `docker compose build` or `docker compose up -d`

## API exits during startup

- Check `docker compose logs api`
- Verify `PG_CONNECTION` or the compose Postgres credentials
- Confirm PostgreSQL is healthy before the API starts

## Cookies are not persisted in the browser

- On local HTTP, keep `AUTH_COOKIE_SECURE=false`
- On HTTPS, set `AUTH_COOKIE_SECURE=true`
- If frontend and backend are cross-site, set `AUTH_COOKIE_SAME_SITE=None`

## Frontend requests fail in local development

- Confirm the API is running on `http://localhost:5247`
- Confirm `client/.env` points `VITE_DEV_PROXY_TARGET` at the correct backend
- Restart `npm run dev` after changing `client/.env`

## Wallet endpoints return Moralis errors

- Set `MORALIS_API_KEY`
- Confirm the wallet address is a valid EVM address
- Confirm the selected chain is in `MORALIS_SUPPORTED_CHAINS`

## Health endpoint returns `503`

- The API process is up, but PostgreSQL is not reachable
- Check database credentials, network connectivity, and Postgres container health

## Frontend route refresh returns `404`

- Serve the built frontend through the provided Nginx config
- Do not serve `client/dist` with a static server that lacks SPA fallback
