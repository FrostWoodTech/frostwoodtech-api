---
paths:
  - "**/Functions/**"
  - "**/Api/**"
  - "**/*Function.cs"
  - "**/*Functions.cs"
  - "**/Dtos/**"
  - "**/DTOs/**"
---

# API surface

One function per endpoint — not one router function. It keeps the Azure portal's monitoring
readable.

`FrostWoodTech.API/Docs/openapi.yaml` is the machine-readable copy of this document, hand-authored
and served at `/api/docs`. Nothing generates it: **when you add, remove or rename a route here,
update the spec in the same change**, or the three frontends are reading a contract that no
longer exists.

## Public (anonymous, cached)

```
GET /api/public/projects?site=agency|personal&tag=&category=&featured=&page=&pageSize=
GET /api/public/projects/{slug}
GET /api/public/articles?site=&tag=&featured=
GET /api/public/services?site=&featured=
GET /api/public/services/{slug}   # only this one carries `projects` and `faqs` — the list cards don't
GET /api/public/pricing/combos?featured=            # plans with no owning service — agency-only, no ?site=
GET /api/public/pricing/services/{serviceId}        # that service's tiers — agency-only, no ?site=
GET /api/public/faqs?site=
GET /api/public/certificates?featured=&page=&pageSize=   # personal-site only, no ?site=
GET /api/public/tags?isTechnology=&category=
GET /api/public/currencies                          # active, priced currencies + effective rate, no ?site=
GET /api/public/home?site=agency|personal
GET /api/public/reviews?sort=latest|rating|country&page=&pageSize=   # published only, not site-scoped
POST /api/public/reviews                                             # anonymous submission — one of two public writes
POST /api/public/contact                                             # anonymous contact-form submission — the other one
```

`?site=` is **required** wherever site visibility applies. A missing `site` is a `400` with code
`site_required` — never "return everything".

Pricing is two routes, not one with an optional `serviceId`. Absence of a query parameter must
never silently change the `where` clause — combo packs (`service_id is null`) and a service's
tiers (`service_id = @id`) are different questions, so they get different URLs. The admin list
keeps all three as explicit filters — `?serviceId=` (one service's tiers), `?comboOnly=true`
(no owning service), `?tiersOnly=true` (any service's tiers, as opposed to one specific service)
— defaulting to everything when none are sent. `serviceId` wins over `tiersOnly` if both are
sent, and `comboOnly` wins over both.
Pricing is agency-only, so neither pricing route (public or admin) takes a `?site=` — it's the
one entity family exempt from the "`?site=` is required" rule below. Certificates are the
opposite exemption — personal-only, so neither certificate route takes a `?site=` either.

Public endpoints return only rows where `is_published = true`, `is_deleted = false`, and the
matching `show_on_{site}` flag is true. Order by that site's `sort_order`, then by
`published_at`/`year` descending.

`/api/public/home` is one round trip instead of six. Return only the featured slices for that
site: featured projects, featured articles, featured services, featured pricing plans, FAQs,
featured reviews (reviews are shared across both sites, so that slice ignores `?site=`).

`POST /api/public/reviews` and `POST /api/public/contact` are the two exceptions to "public
endpoints are read-only". A review lands with `is_published = false`; a contact submission is
never public content at all — there is no matching public read, only the admin inbox below. Both
are rate limited per IP (see `.claude/rules/auth.md`) and never return the full row, just an id.
The contact endpoint additionally carries a `website` honeypot field: a bot that fills it gets a
normal 201 but the row is filed with `status = spam`, so it never learns it was caught.

## Admin (JWT required)

```
POST   /api/cms/admin/auth/login            # email + password
POST   /api/cms/admin/auth/google           # Google ID token exchange
POST   /api/cms/admin/auth/register
POST   /api/cms/admin/auth/verify-email          # body { token }, moves email_verification_required -> pending
POST   /api/cms/admin/auth/resend-verification   # body { email }, always returns the same generic response
POST   /api/cms/admin/auth/forgot-password  # body { email }, always returns the same generic response
POST   /api/cms/admin/auth/set-password     # body { token, password, confirmPassword }, single-use link
POST   /api/cms/admin/auth/refresh         # body { refreshToken }, rotates
POST   /api/cms/admin/auth/logout          # body { refreshToken }, revokes it

GET    /api/cms/admin/users                 # super_admin only, ?search= &status=
POST   /api/cms/admin/users/{id}/approve    # super_admin only
POST   /api/cms/admin/users/{id}/reject     # super_admin only, body { reason }
POST   /api/cms/admin/users/{id}/disable    # super_admin only
DELETE /api/cms/admin/users/{id}            # super_admin only, soft delete

CRUD   /api/cms/admin/projects              # + /{id}/images, /{id}/images/reorder
CRUD   /api/cms/admin/articles
CRUD   /api/cms/admin/services              # projectIds on the write DTO links case studies — no sub-route
CRUD   /api/cms/admin/pricing-plans   # + /reorder, no `site` in the body — pricing is agency-only
CRUD   /api/cms/admin/faqs                  # ?serviceId= scopes the list; ?globalOnly=true excludes every scoped FAQ
CRUD   /api/cms/admin/certificates          # + /reorder, no `site` in the body — personal-only
CRUD   /api/cms/admin/tags
CRUD   /api/cms/admin/currencies            # ?isActive= — manual override + live rate, no reorder
POST   /api/cms/admin/currencies/refresh-rates   # "Refresh live rates" — one call updates every currency
CRUD   /api/cms/admin/reviews               # + /reorder — publish/unpublish/featured all via PUT
GET,PUT,DELETE /api/cms/admin/contact-submissions[/{id}]   # ?status= &site= &serviceId= — no POST, no reorder

POST   /api/cms/admin/media/presigned-upload # Neon Object Storage presigned PUT URL
GET    /api/cms/admin/media/config          # base URL to resolve a media:// token into a loadable url
POST   /api/cms/admin/{entity}/reorder      # bulk sort_order update
```

Admin endpoints return drafts and metadata. Use admin-specific DTOs — never reuse the public
response DTOs, or admin-only fields will eventually leak to the public sites.

## Response conventions

- Lists: `{ items, page, pageSize, total }`. Default `pageSize` 20, cap at 100.
- Errors: RFC 7807 `application/problem+json` with a stable machine-readable `code`
  (`site_required`, `account_pending`, `slug_taken`, `validation_failed`, …).
- Public GETs: `Cache-Control: public, max-age=300` plus an `ETag`. Handle
  `If-None-Match` and return `304`.
- Admin responses: `Cache-Control: no-store`.
