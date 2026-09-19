---
paths:
  - "**/Program.cs"
  - "**/*.csproj"
  - "**/host.json"
  - "**/local.settings.json"
  - "**/local.settings.example.json"
  - ".github/workflows/**"
---

# Azure Functions hosting & deployment

Isolated worker. All DI wiring lives in `Program.cs`. One Function per endpoint, every trigger
`AuthorizationLevel.Anonymous` (auth is middleware — see auth.md).

## Middleware order

`CorsMiddleware` (outermost, so errors get CORS headers) → `ExceptionHandlingMiddleware` (generic
problem+json 500) → `JwtAuthenticationMiddleware`. OPTIONS preflight is answered by the host's CORS
settings, not by code.

## Database

- `AddDbContextPool` with Npgsql `EnableRetryOnFailure` and a 30s command timeout.
- Runtime uses Neon's **pooled** connection string; keep the pool small via the connection string
  (`Maximum Pool Size`). Migrations use the **direct** string.
- Native enums must be mapped on both the `NpgsqlDataSourceBuilder` and the EF options (and in the
  design-time factory).
- Neon cold start (~1s after idle) is expected; don't add keep-alive timers.

## Configuration

- Loaded from `local.settings.json`, `local.settings.{DOTNET_ENVIRONMENT or AZURE_FUNCTIONS_ENVIRONMENT}.json`
  (default `Development`), then environment variables.
- `local.settings*.json` files are gitignored and marked `CopyToPublishDirectory=Never`; only
  `local.settings.example.json` is committed. In Azure everything comes from app settings.
- Never log configuration values, connection strings or tokens.

## Startup

Seeds the super admin and the USD base currency; a seeding failure is logged and never stops the
host. Schema changes are **never** applied at startup.

## CI/CD

- `ci.yml` (push/PR to `main` or `develop`): build → test (Testcontainers needs Docker, present on
  ubuntu runners) → `dotnet ef migrations has-pending-model-changes` (with a dummy connection string).
- `main_func-frostwoodtech-cms-prod.yml` (push to `main`, one at a time): build → **test** → OIDC
  login → read the `ConnectionStrings__Migration` app setting from the Function App →
  `dotnet ef database update` → publish → deploy → `/api/health` smoke check when
  `AZURE_FUNCTIONAPP_URL` is set. Tests run before migrations so a failure never touches the
  database, and the direct string never leaves Azure.
- Both workflows use `permissions: contents: read`.
- Migrations are created only with `dotnet ef migrations add`; never hand-edit migration, designer
  or snapshot files.
