# CLAUDE.md — FrostWoodTech CMS Backend

## What this is

One headless CMS API + one database, powering **three** frontends:

| Consumer | Content it reads |
|---|---|
| Agency site (React) | Projects, products, articles, services, pricing, FAQs, reviews; posts contact + reviews |
| Personal site (React) | Projects, products, articles, certificates, FAQs, reviews; posts contact + reviews |
| Admin SPA (React) | Everything, incl. drafts, contact inbox and user approvals |

No duplicated rows per site — entities carry per-site visibility flags and the frontend asks for
the site it wants. One row can appear on both sites.

## Stack

.NET 10 **Azure Functions** isolated worker (not ASP.NET Web API) · **PostgreSQL on Neon** via
EF Core + Npgsql · **Neon Object Storage** (S3-compatible) for media · custom **JWT** +
**Google Sign-In** for admin auth.

## Layering

```
Azure Functions  →  Service layer  →  EF Core  →  Postgres
```

**No repository layer.** Services hold business logic *and* data access so a junior developer can
follow a request end to end. Do not propose repositories, CQRS, or a mediator.

## Two API surfaces — keep them physically separate

- **`/api/public/*`** — anonymous, read-only (except `POST` reviews and contact), cacheable.
  Published, non-deleted rows only.
- **`/api/cms/admin/*`** — JWT required, full CRUD, drafts and metadata.

Separate folders, separate DTOs. A public DTO never carries admin fields.

## Non-negotiables

- **Never store file bytes in Postgres.** Store the storage `object_key`, URL, dimensions and alt text.
- **Never let the client control `is_published` filtering.** Public endpoints filter server-side.
- **`?site=` is required** on every public endpoint with site visibility. Missing ⇒ `400 site_required`,
  never "return everything".
- Timestamps are `timestamptz`, UTC.
- Every content table has `created_at`, `updated_at`, `is_deleted` (soft delete).
- `alt_text` is required on every image.
- Secrets live in app settings / Key Vault, never in committed files. `local.settings*.json` is
  gitignored and never published.
- Never log connection strings or tokens.

## Conventions

- snake_case in Postgres, PascalCase in C#.
- Slugs: generated from the title, lowercase, hyphenated, unique (soft-deleted rows included),
  never empty, and **stable once published**.
- Markdown stored raw; sanitized on render in React.
- Soft delete for content; child rows (images, pricing features) are hard-deleted.
- Enums map to Postgres native enums — **never int**.
- Lists return `{ items, page, pageSize, total }`.
- Errors are RFC 7807 `application/problem+json` with a stable `code`.
- Comments: only facts the code can't show, one or two lines.
- Migrations: generated with `dotnet ef` only, never hand-edited.

## Tests

`FrostWoodTech.Tests` — unit tests, service integration tests against a real Postgres container,
and API guard tests (JWT middleware, `?site=` checks, route scan). Every bug fix gets a regression
test. Docker must be running.

## Where the detail lives

These load automatically when you touch matching files — don't read them preemptively:

| Rule | Covers |
|---|---|
| `.claude/rules/schema.md` | Tables, columns, indexes, enums |
| `.claude/rules/api-surface.md` | Endpoints, query params, caching |
| `.claude/rules/services-layer.md` | Service shape, validation rules |
| `.claude/rules/auth.md` | JWT + Google flow, user states, rate limits |
| `.claude/rules/media.md` | Presigned uploads, folders, deletes |
| `.claude/rules/functions-hosting.md` | DI, Neon pooling, config, CI/CD |

`docs/roadmap.md` lists known gaps, not current state. Don't implement from it unless asked.

## Glossary

| Term | Meaning |
|---|---|
| Site | `agency` or `personal` |
| Featured | Appears on that site's home page |
| Combo pack | A `pricing_plan` with `service_id = null` |
| Technology tag | `tags.is_technology = true`; has a `technology_category` |
| Category tag | `tags.is_technology = false`; e.g. Frontend, Backend |
