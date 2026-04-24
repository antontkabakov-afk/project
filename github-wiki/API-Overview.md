# API Overview

Base paths:

- Direct API in Docker: `http://localhost:8081`
- Proxied API in Docker: `http://localhost:8080/api`
- Local dev API: `http://localhost:5247/api`

Health endpoints:

- `GET /health/live`
- `GET /health/ready`

Authentication endpoints:

| Method | Route | Auth required | Description |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | No | Create a user and issue cookies |
| `POST` | `/api/auth/login` | No | Validate credentials and issue cookies |
| `POST` | `/api/auth/refresh` | No | Rotate refresh token and renew cookies |
| `POST` | `/api/auth/logout` | No | Revoke session cookies server-side and client-side |

Wallet endpoints:

| Method | Route | Auth required | Description |
| --- | --- | --- | --- |
| `GET` | `/api/wallet` | Yes | Get current wallet connection status |
| `PUT` | `/api/wallet` | Yes | Connect a wallet and capture an initial snapshot |
| `POST` | `/api/wallet/snapshot` | Yes | Force a new wallet snapshot |

Portfolio endpoints:

| Method | Route | Auth required | Description |
| --- | --- | --- | --- |
| `GET` | `/api/portfolio/assets` | Yes | Latest stored portfolio assets |
| `GET` | `/api/portfolio/history` | Yes | Full snapshot history |
| `GET` | `/api/portfolio/statistics` | Yes | Calculated historical performance and allocation |

Crypto endpoints:

| Method | Route | Auth required | Description |
| --- | --- | --- | --- |
| `GET` | `/api/crypto/assets` | Yes | Latest supported crypto prices |
| `GET` | `/api/crypto/assets/{assetId}/history` | Yes | Stored price history for one supported asset |
