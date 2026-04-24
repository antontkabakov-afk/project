# Authentication

## Current auth model

- Access token: JWT stored in the `access_token` HTTP-only cookie
- Refresh token: random token stored in the `refresh_token` HTTP-only cookie
- Session persistence: PostgreSQL tables `Session` and `RefreshToken`

## Backend behavior

- `POST /api/auth/login` validates credentials and issues both cookies
- `POST /api/auth/register` creates the user, creates a session, and issues both cookies
- `POST /api/auth/refresh` revokes the current refresh token and rotates a new one
- `POST /api/auth/logout` clears cookies and revokes session refresh tokens

## Frontend behavior

- Dashboard routes are guarded before rendering
- The shared API client retries once after calling `/api/auth/refresh` when a protected request returns `401`
- Login and signup redirect to `/wallet` on success

## Cookie settings

The backend reads these env vars:

- `AUTH_COOKIE_SECURE`
- `AUTH_COOKIE_SAME_SITE`
- `AUTH_COOKIE_DOMAIN`

Recommended values:

- Local HTTP Docker: `AUTH_COOKIE_SECURE=false`, `AUTH_COOKIE_SAME_SITE=Lax`
- Same-site HTTPS production: `AUTH_COOKIE_SECURE=true`, `AUTH_COOKIE_SAME_SITE=Lax`
- Cross-site HTTPS production: `AUTH_COOKIE_SECURE=true`, `AUTH_COOKIE_SAME_SITE=None`

## Operational notes

- If cookies are not being stored locally, `AUTH_COOKIE_SECURE` is usually set incorrectly for HTTP
- If a session is expired or revoked, protected pages redirect back to `/login`
