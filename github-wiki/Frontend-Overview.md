# Frontend Overview

## Runtime

- Framework: React 18
- Build tool: Vite 8
- Router: `react-router-dom`
- Styling: Tailwind CSS plus component-level styles

## Route map

| Route | Access | Purpose |
| --- | --- | --- |
| `/` | Public | Landing page and auth entry point |
| `/login` | Public-only | Login screen |
| `/signup` | Public-only | Registration screen |
| `/wallet` | Auth-only | Wallet connection and snapshot creation |
| `/assets` | Auth-only | Supported crypto price list and history |
| `/history` | Auth-only | Stored wallet snapshot timeline |
| `/statistics` | Auth-only | Historical performance and allocation views |

## Auth flow

- `RequireAuth` protects dashboard routes
- `PublicOnlyRoute` redirects logged-in users away from `/login` and `/signup`
- The shared API client retries one request after `/api/auth/refresh` on `401`

## API strategy

- Default frontend API base: `/api`
- Local dev proxy target: `VITE_DEV_PROXY_TARGET`, default `http://localhost:5247`
- Docker runtime proxy target: `API_UPSTREAM`, default `http://api:8080`

## Build output

- Vite writes the production bundle to `client/dist`
- Nginx serves `client/dist` and uses SPA fallback to `index.html`
