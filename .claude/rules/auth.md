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

Both public sites are anonymous. Only `/api/cms/admin/*` is protected, by `JwtAuthenticationMiddleware`
(not per Function). Every trigger is `AuthorizationLevel.Anonymous`; function keys are not auth.
The middleware's anonymous allow-list is: register, login, google, refresh, logout, verify-email,
resend-verification, forgot-password, set-password.

## Registration and email verification

- `POST auth/register` creates an `email_verification_required` account and emails
  `{Email:BaseUrl}/verify-email?token=...` (24h, single use, hashed). No token is issued.
- `POST auth/verify-email` moves the account to `pending`. Errors: `invalid_verification_token`,
  `verification_token_already_used`, `verification_token_expired` (safe: they require a token).
- `POST auth/resend-verification` issues a new link and invalidates older ones. **Always the same
  generic response** (unknown, verified or rate limited at 3/hour/email).

## Forgotten and first passwords

- `POST auth/forgot-password` emails a 1h reset link. **Always the same generic 200.** Only
  `pending` and `approved` accounts get a link (Google-only accounts included). Issuing one
  invalidates older reset links.
- `POST auth/set-password` redeems a setup or reset link (`password_tokens`, told apart by
  `purpose`). Sets the password, invalidates every other link, revokes all refresh tokens, issues
  no token. Errors: `invalid_setup_token`, `setup_token_already_used`, `setup_token_expired`.

## Sign-in

- **Password:** Argon2id verify → `status = approved` → access JWT (15 min) + refresh token (30 days).
  Unknown emails and missing hashes burn the same hashing time as a wrong password.
- **Google:** validate the ID token against Google's keys (`aud` = `Google:ClientId`), require a
  verified email, match by `google_subject_id` then email. An unknown email creates a **`pending`**
  account (skips email verification). Never auto-approve.
- A non-approved account is rejected at token issue with 403: `email_verification_required`,
  `account_pending`, `account_rejected` (with the reason), `account_disabled`.

## Tokens

- Claims: `sub`, `email`, `role`, `jti`.
- The refresh token is only ever an `HttpOnly; Secure; SameSite=Lax` cookie named `refreshToken`,
  scoped to `/api/cms/admin/auth`. `AuthResponse.RefreshToken` is `[JsonIgnore]`. The SPA keeps the
  access token in memory.
- Refresh **rotates**. A revoked token presented again revokes every session for that user and
  returns `refresh_token_reused`. Refresh also re-checks status and soft delete.
- Disable, reject, delete, password change and set-password revoke all refresh tokens, capping
  remaining access at one access-token lifetime.
- Logout succeeds even for unknown tokens.

## User management

- Only the `super_admin` may list, approve, reject, disable or delete users (`forbidden` otherwise).
- Nobody can act on their own account (`cannot_modify_self`) or on the super admin row
  (`cannot_modify_super_admin`).
- `ApproveAsync` accepts only `pending` accounts (`user_not_pending`).
- Reject requires a reason, shown to the user at sign-in.

## Super admin

- Exactly one, seeded on startup from `SuperAdmin:Email/FirstName/LastName`. Guarded by an
  advisory lock, a seed-if-missing check, and `ix_users_single_super_admin`.
- **No password in configuration.** The seeder writes `password_hash = null`, `status = approved`,
  `email_verified_at = now()` and emails a 24h setup link. It never touches an existing account;
  a lost link means deleting the row and restarting.

## Rate limiting

`LoginRateLimiter` is a Postgres fixed window (in-memory would reset per instance), counted
separately per `AuthAttemptAction`:

- Login failures: 10 per email + 30 per IP per 15 min. A successful login clears the email's count.
- Password reset requests: 3 per email + 3 per IP per 15 min (every request counts).

Public writes count their own rows per IP instead: reviews 3/24h, contact 5/24h. The contact
`website` honeypot files the row as `spam`, answers normally, and doesn't count toward the limit.
Client IPs come from `X-Forwarded-For` (port stripped) and are only used for limits, never authorization.

## Secrets

`Jwt:Signer`, `Email:ApiKey`, `NeonS3:AccessKey/SecretKey` and `ConnectionStrings:Default` live in
app settings / Key Vault. Never commit them or log them.
