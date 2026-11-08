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

One Function per endpoint. Functions are thin: parse input → call the service → return.

`FrostWoodTech.API/Docs/openapi.yaml` is the hand-written contract served at `/api/docs`.
**Adding, removing or renaming a route means updating the spec in the same change.**
`RouteAuthorizationTests` fails if a route sits outside `public/`, `cms/admin/`, `health`, `docs`
or `openapi.yaml`, or if a public route other than reviews/contact isn't GET.

## Public (anonymous, cached)

```
GET  /api/public/projects?tag=&category=&featured=&page=&pageSize=
GET  /api/public/projects/{slug}
GET  /api/public/products?featured=&page=&pageSize=
GET  /api/public/products/{slug}
GET  /api/public/articles?tag=&featured=&page=&pageSize=
GET  /api/public/articles/{slug}
GET  /api/public/services?featured=&page=&pageSize=
GET  /api/public/services/{slug}                    # only this one embeds projects + faqs
GET  /api/public/faqs                           # global FAQs, not paged
GET  /api/public/home                           # featured slices in one call
GET  /api/public/pricing/combos?featured=
GET  /api/public/pricing/services/{serviceId}
GET  /api/public/tags?isTechnology=&category=
GET  /api/public/currencies
GET  /api/public/reviews?sort=latest|rating|country&page=&pageSize=
POST /api/public/reviews                        # anonymous; lands unpublished
POST /api/public/contact                        # anonymous; admin inbox only
```

- Only `is_published`, non-deleted rows. Order by `sort_order`, then `published_at`/`year` descending.
- A legacy `?site=` is ignored, never rejected (`PublicEndpointTests`).
- Combo packs and a service's tiers are separate routes on purpose: an absent query parameter must
  never silently change the `where` clause.
- `home` returns featured projects, articles, services, combo plans, FAQs and
  featured reviews. Services are called sequentially (shared DbContext).
- The two POSTs return only an `id`. Rate limits and the honeypot are in auth.md.

## Admin (JWT required except the auth allow-list)

```
POST /auth/register | login | google | refresh | logout | verify-email | resend-verification
     | forgot-password | set-password                        # anonymous
GET  /auth/me    POST /auth/change-password

GET  /users?search=&status=     POST /users/{id}/approve | reject | disable     DELETE /users/{id}

CRUD /projects      + /reorder, /{id}/publish, /{id}/images, /{id}/images/{imageId}, /{id}/images/reorder
CRUD /products      + same as projects
CRUD /articles      + /reorder, /{id}/publish
CRUD /services      + /reorder, /{id}/publish; projectIds in the body replaces case-study links
CRUD /pricing-plans + /reorder, /{id}/publish, /{id}/features[/{featureId}], /{id}/features/reorder
CRUD /faqs          + /reorder; ?serviceId= scopes, ?globalOnly=true excludes scoped FAQs
CRUD /tags          + GET /tags/categories
CRUD /reviews       + /reorder; publish and featured via PUT
CRUD /currencies    + POST /currencies/refresh-rates; ?isActive=
GET,PUT,DELETE /contact-submissions[/{id}]   ?status=&serviceId=&search=

POST /media/presigned-upload    GET /media/config

# Trash: the same three routes for projects, products, articles, services, pricing-plans, faqs,
# tags, currencies, reviews and contact-submissions (not users)
GET    /{entity}/trash?search=&page=&pageSize=     # soft-deleted rows -> { items: TrashedItemResponse }
POST   /{entity}/{id}/restore                      # back to the live list; 200 with the admin DTO
DELETE /{entity}/{id}/permanent                    # super admin only; 204
```

All paths above are under `/api/cms/admin`. Pricing admin filters: `serviceId`, `tiersOnly`,
`comboOnly` (`comboOnly` wins, then `serviceId`). Admin DTOs are separate from public DTOs.

## Response conventions

- Lists: `{ items, page, pageSize, total }`; `pageSize` defaults to 20, capped at 100.
- Errors: RFC 7807 `application/problem+json` with a stable `code` (`site_required`,
  `validation_failed`, `slug_taken`, `not_found`, `forbidden`, …). Unhandled exceptions become a
  generic `500 internal_error`.
- Public GETs: `Cache-Control: public, max-age=300` plus an `ETag`; `If-None-Match` (lists, weak
  tags and `*`) returns `304`.
- Admin responses: `Cache-Control: no-store`.
