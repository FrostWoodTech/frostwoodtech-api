# frostwoodtech-backend

One headless CMS API and one database for three frontends: the FrostWoodTech agency site, the
personal site, and the admin SPA.

.NET 10 Azure Functions (isolated worker) · PostgreSQL on Neon via EF Core + Npgsql ·
Neon Object Storage (S3-compatible) for media · JWT + Google Sign-In for admin auth.

Architecture and the rules for changing the code live in [`CLAUDE.md`](CLAUDE.md) and
[`.claude/rules/`](.claude/rules).

## Running locally

Prerequisites: .NET 10 SDK, [Azure Functions Core Tools v4][func], a Postgres database (Neon or
local), and Docker for the tests.

```bash
cp FrostWoodTech.API/local.settings.example.json FrostWoodTech.API/local.settings.json
# fill in ConnectionStrings:Default, Jwt:Signer, SuperAdmin:*, NeonS3:*, Email:*, Cors:AllowedOrigins
```

Settings load from `local.settings.json`, then `local.settings.{DOTNET_ENVIRONMENT}.json`
(default `Development`), then environment variables. All of these files are gitignored and are
never included in a publish.

Apply the schema and start the host:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project FrostWoodTech.API
cd FrostWoodTech.API && func start
```

The API runs on `http://localhost:7060`. The super admin is seeded from `SuperAdmin:*` on first
start and emailed a setup link (no password lives in config).

```bash
curl http://localhost:7060/api/health
curl "http://localhost:7060/api/public/home?site=agency"
```

## API reference

Set `Docs:Enabled` to `true` to serve the reference at `/api/docs` and the spec at
`/api/openapi.yaml` (both `404` otherwise). The spec is hand-written at
`FrostWoodTech.API/Docs/openapi.yaml`, so **a route change is a spec change**. Lint it with:

```bash
npx @redocly/cli lint FrostWoodTech.API/Docs/openapi.yaml
```

## Tests

```bash
dotnet test
```

Docker must be running. The suite has three layers:

- **Unit tests:** slugs, query parsing, caching headers, client IPs, password hashing, JWTs.
- **Integration tests:** every service against a real Postgres container (Testcontainers) with
  the real migrations, since native enums, `citext` and partial unique indexes can't be faked.
- **API guard tests:** the JWT middleware, the `?site=` check in public Functions, and a
  route scan that keeps every trigger anonymous and under a known prefix.

## Migrations

```bash
dotnet ef migrations add <Name> --project FrostWoodTech.API
```

Always generate migrations with the CLI; never hand-edit them. They are applied by the deploy
workflow, never at startup. CI fails if the model changed without a migration.

## Deployment

- `ci.yml` builds, runs the tests and checks for missing migrations on every push and PR to `main`.
- `main_func-frostwoodtech-cms-prod.yml` runs on push to `main`: build → test → migrate → publish →
  deploy → health check. A failing test stops the job before the database is touched. The migration
  string is read from the Function App's own settings at deploy time, so it never becomes a GitHub secret.

Repository configuration:

| Kind | Name | Notes |
|---|---|---|
| Secret | `AZUREAPPSERVICE_CLIENTID_*`, `AZUREAPPSERVICE_TENANTID_*`, `AZUREAPPSERVICE_SUBSCRIPTIONID_*` | OIDC login; also used to read the migration app setting |
| Variable | `AZURE_FUNCTIONAPP_NAME` | The Function App name |
| Variable | `AZURE_RESOURCE_GROUP` | Holds the Function App; needed to read its app settings |
| Variable | `AZURE_FUNCTIONAPP_URL` | e.g. `https://<app>.azurewebsites.net`; enables the post-deploy health check |

Function App settings (use `__` for nesting):

| Setting | Notes |
|---|---|
| `ConnectionStrings__Default` | Neon **pooled** string, used at runtime |
| `ConnectionStrings__Migration` | Neon **direct** (non-pooled) string; the deploy workflow reads it for DDL |
| `Jwt__Signer`, `Jwt__Issuer`, `Jwt__Audience` | Signer is a long random secret |
| `Google__ClientId` | Admin SPA OAuth client id |
| `SuperAdmin__Email`, `SuperAdmin__FirstName`, `SuperAdmin__LastName` | Identity only |
| `NeonS3__Endpoint`, `NeonS3__AccessKey`, `NeonS3__SecretKey`, `NeonS3__Region`, `NeonS3__BucketName` | Media storage |
| `Email__Provider` (`brevo`), `Email__ApiKey`, `Email__FromAddress`, `Email__FromName`, `Email__BaseUrl` | `BaseUrl` is the admin SPA URL |
| `Cors__AllowedOrigins` | Comma-separated exact origins of the three frontends |
| `Contact__NotifyAddress` | Optional; defaults to the super admin |
| `Docs__Enabled` | Optional; `true` to expose `/api/docs` |
| `Docs__ServerUrls__0`, `__1`, … | Optional; extra servers in the docs dropdown after "This host" (absolute URLs ending in `/api`) |

[func]: https://learn.microsoft.com/azure/azure-functions/functions-run-local
