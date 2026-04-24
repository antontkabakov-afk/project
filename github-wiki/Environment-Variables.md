# Environment Variables

## Root `.env`

Used by `docker-compose.yml`.

| Variable | Required | Default | Purpose |
| --- | --- | --- | --- |
| `WEB_PORT` | No | `8080` | Host port for the frontend container |
| `API_PORT` | No | `8081` | Host port for the API container |
| `POSTGRES_DB` | No | `crypto_tracker` | PostgreSQL database name |
| `POSTGRES_USER` | No | `postgres` | PostgreSQL username |
| `POSTGRES_PASSWORD` | Yes | none | PostgreSQL password |
| `JWT_ACCESS_SECRET` | Yes | none | JWT signing secret for access tokens, minimum 32 bytes |
| `CLIENT_ORIGINS` | Recommended | `http://localhost:8080,http://localhost:5173,http://localhost:4173` | Allowed browser origins for CORS |
| `AUTH_COOKIE_SECURE` | Recommended | `false` | Must be `true` for HTTPS deployments |
| `AUTH_COOKIE_SAME_SITE` | No | `Lax` | Cookie SameSite mode |
| `AUTH_COOKIE_DOMAIN` | No | empty | Optional cookie domain override |
| `COINGECKO_BASE_URL` | No | `https://api.coingecko.com/api/v3/` | CoinGecko API base URL |
| `COINGECKO_DEMO_API_KEY` | No | empty | Optional CoinGecko demo key |
| `COINGECKO_SUPPORTED_COINS` | No | built-in list | Tracked asset ids for price snapshots |
| `MORALIS_BASE_URL` | No | `https://deep-index.moralis.io/api/v2.2/` | Moralis API base URL |
| `MORALIS_API_KEY` | Recommended | empty | Required for wallet sync endpoints |
| `MORALIS_DEFAULT_CHAIN` | No | `eth` | Default wallet chain |
| `MORALIS_SUPPORTED_CHAINS` | No | built-in list | Allowed wallet chains |
| `CRYPTO_PRICE_SNAPSHOT_INTERVAL_MINUTES` | No | `15` | Background CoinGecko snapshot cadence |
| `PORTFOLIO_SNAPSHOT_INTERVAL_MINUTES` | No | `60` | Background wallet snapshot cadence |

## `client/.env`

Used only for local Vite development.

| Variable | Required | Default | Purpose |
| --- | --- | --- | --- |
| `VITE_DEV_PROXY_TARGET` | No | `http://localhost:5247` | Backend target for Vite `/api` proxy |
| `VITE_API_BASE_URL` | No | `/api` | Optional direct API base override |

## Notes

- The frontend no longer hardcodes `https://localhost:7269/api`.
- Docker deployments rely on the frontend reverse proxy, so `/api` stays same-origin.
- The backend requires `JWT_ACCESS_SECRET` and `PG_CONNECTION` at runtime. Compose derives `PG_CONNECTION` automatically from the Postgres env vars.
