# Roadmap

Known gaps and planned additions — **not** current state. Don't build from this file unless asked;
`CLAUDE.md` and `.claude/rules/` describe what the code does.

Last reviewed: 2026-09-13.

## Known gaps

### Slug change warning

The rules say a published entity's slug should stay stable and editors should be warned when it
changes. Nothing warns today; articles, projects, products, services and tags change slugs silently.
Adding it needs a `warnings` field on admin write responses, the spec, and the admin SPA.

### Service URL validation

Service CTA URLs and image URLs are not checked for absolute http(s), unlike projects, products,
images and certificates. Services also still accept sort orders on create/update.

### Scheduled exchange-rate refresh

Live rates only change when an admin presses refresh. A timer-triggered Function calling
`CurrencyService.RefreshLiveRatesAsync` would keep them current.

### Host-level tests

Tests cover services, helpers, the JWT middleware and Functions called directly, but nothing runs
the Functions host itself (DI registration, routing). The post-deploy `/api/health` check is the
only end-to-end signal.

### Combo packs spanning several services

`pricing_plans.service_id = null` means combo pack. A combo naming several services would need a
`pricing_plan_services` join table, keeping `service_id` as the primary owner.

### Observability

Service-layer failure paths don't log, so a rejected write leaves no trace beyond its response.

### Trash auto-purge

Deleted rows stay in the trash until a super admin purges them. `deleted_at` is indexed, so a timer
Function could purge rows older than N days through the same `PurgeAsync` methods.
