---
paths:
  - "**/Auth/**"
  - "**/*Auth*.cs"
  - "**/*Jwt*.cs"
  - "**/*Token*.cs"
  - "**/*User*.cs"
  - "**/Middleware/**"
---

# Auth

Both public sites are fully anonymous. Only `/api/cms/admin/*` (the CMS) is protected.

## Registration and email verification

`POST /cms/admin/auth/register` creates a `email_verification_required` account and emails a
verification link (`{Email:BaseUrl}/verify-email?token=...`, 24h expiry, single-use, hashed in
the DB exactly like a refresh token — see `EmailVerificationToken`/`EmailVerificationTokenGenerator`).
No token is issued at registration.

`POST /cms/admin/auth/verify-email` consumes the token and moves the account to `pending`. Errors:
`invalid_verification_token` (unknown token), `verification_token_already_used`,
`verification_token_expired` — none of these leak whether an email is registered, since the token
itself already proves inbox possession.

`POST /cms/admin/auth/resend-verification` issues a fresh token, invalidating any prior unused one.
It **always returns the same generic response** regardless of whether the email exists, is
already verified, or is rate-limited (3 sends/hour/email) — this endpoint must never be usable to
enumerate accounts.

## Forgotten passwords

`POST /cms/admin/auth/forgot-password` issues a 1h reset link and emails it. **Always the same
generic 200** — unknown, unverified, rejected, disabled, rate limited or provider-down all look
identical, since the service never returns a failure here. Only `pending` and `approved`
accounts get a link; issuing one invalidates any outstanding unused reset link for that user.
Google-only accounts (null `password_hash`) do get one — a null hash is a normal state, not a
broken one, same as the seeded super admin below.

## Setting a first password

`POST /cms/admin/auth/set-password` redeems a single-use link (`{Email:BaseUrl}/set-password?token=...`,
hashed in `password_tokens` like a verification token). Setup and reset links are the same row,
told apart by `purpose`, and redeemed identically. Sets `password_hash`, invalidates every other
outstanding link for the user regardless of purpose, revokes all refresh tokens, issues no token
of its own. Errors — `invalid_setup_token`, `setup_token_already_used`, `setup_token_expired` —
are not an enumeration risk, same reasoning as verify-email: reaching any of them already
requires holding a token.

## Password login

Verify Argon2id hash → check `status = approved` → issue an access JWT (15 min) plus a refresh
token (rotating, stored **hashed** in the DB, 30 days).

## Google sign-in

React sends the Google ID token → API validates it against Google's JWKS and checks `aud`
matches the configured client ID → match the user by `google_subject_id`, falling back to the
verified email → issue the same JWT pair.

Google sign-in for an email with no user row creates a **`pending`** user directly — it skips
`email_verification_required` entirely, since Google has already verified the address. Never
auto-approve.

## Tokens

Claims: `sub`, `email`, `role`, `jti`. Validate on every `/api/cms/admin/*` call in a **Functions
middleware**, not per-function.

The refresh token never reaches the response or request body — `Login`/`GoogleSignIn`/`Refresh`
set it as an `HttpOnly; Secure; SameSite=Lax` cookie (`refreshToken`, scoped to
`/api/cms/admin/auth`, see `HttpResponses.SetRefreshTokenCookie`), and `Refresh`/`Logout` read it
back the same way. `AuthResponse`'s `RefreshToken`/`RefreshTokenExpiresAt` properties exist for
the service layer and those Functions to read in C# only — they're `[JsonIgnore]`d, so nothing
ever serializes the raw value. The frontend keeps the access token in memory only (never
persisted) and relies on the cookie for silent reauthentication on load — see
`frostwoodtech-web`'s `src/admin/services/httpClient.ts`.

Refresh tokens **rotate**: each refresh revokes the presented token and issues a new one. A
revoked token presented again is treated as theft — every live token for that user is revoked and
the call fails with `refresh_token_reused`.

A JWT cannot be recalled once issued, so revocation is what actually bounds access: disable,
reject, delete and change-password all revoke that user's refresh tokens, which caps their
remaining access at one access-token lifetime (15 min). Refresh also re-checks `status`, so a
disabled user cannot renew.

Function-level `AuthorizationLevel` is set to `Anonymous` everywhere; function keys are not an
auth system. Authorization is the middleware's job.

## User states

`status`: `email_verification_required | pending | approved | rejected | disabled`.

Registration is open but powerless. A non-approved user is **rejected at token issue** with
`403` (`NotApproved` in `UserService`) — do not issue a scopeless token. One place to get it
wrong is better than two. Codes: `email_verification_required`, `account_pending`,
`account_rejected`, `account_disabled`.

`ApproveAsync` only accepts a `pending` account (`user_not_pending` otherwise) — defense in depth
so an unverified account can't be approved by a stale admin tab or a direct DB edit.

## Super admin rules

- Exactly one `super_admin`, seeded on first deploy from `SuperAdmin__Email` / `__FirstName` /
  `__LastName`. Guarded three ways: `SuperAdminSeeder`'s `pg_advisory_xact_lock`, its
  seed-if-missing check, and a filtered unique index (`ix_users_single_super_admin`).
- **No super admin password lives in configuration** — there is no `SuperAdmin__Password`. The
  seeder writes `password_hash = null`, `status = approved`, `email_verified_at = now()` (trusted
  config, nothing to verify), then emails a 24h setup link; the account can't sign in until it's
  redeemed.
- The seeder never touches an existing account, since config no longer knows what its password
  should be. Losing the setup link means deleting the row and redeploying to re-seed.
- Only `super_admin` may approve, reject, disable, or change roles.
- A `super_admin` cannot disable or demote themselves.

## Secrets

`Jwt__Signer`, `Google__ClientId`, Cloudinary keys, `ConnectionStrings__Default` live in app
settings / Key Vault. Never in a committed `local.settings.json`.

## Rate limiting

The login endpoint needs rate limiting per IP and per email —
`ILoginRateLimiter`/`LoginRateLimiter`, a Postgres-backed fixed window (in-memory would reset
per instance since Functions scale out).

`forgot-password` uses the same limiter and table, discriminated by `AuthAttemptAction` so the
two are counted and limited **independently**: 10/email + 30/IP per 15 min for login failures,
3/email + 3/IP for reset requests (every request counts, not just failures — there's no such
thing as a failed one from the caller's side).

`POST /api/public/reviews` and `POST /api/public/contact` — the two anonymous public writes —
use the same fixed-window idea, per IP only, but count rows in their own table rather than a
separate attempts table (every submission is already persisted, unlike a failed login). See
`ReviewService.SubmitAsync` (3/24h) and `ContactService.SubmitAsync` (5/24h — a genuine prospect
may legitimately follow up more than once). The contact endpoint also has a `website` honeypot
field: a filled one is filed as `status = spam` and still answered normally, so a bot never learns
it was caught, and honeypot submissions don't count against the rate limit.
