---
paths:
  - "**/Domain/**"
  - "**/Entities/**"
  - "**/Data/**"
  - "**/Infrastructure/**"
  - "**/Migrations/**"
  - "**/*DbContext.cs"
  - "**/*Configuration.cs"
---

# Database schema

snake_case in Postgres, PascalCase in C#. Every content table also has `created_at`, `updated_at`
(stamped by the DbContext) and `is_deleted` (hidden by a global query filter).

## Site visibility block

Applies to `projects`, `products`, `articles`, `services`:

```
show_on_agency        bool
featured_on_agency    bool      -- home page card; requires show_on_agency
agency_sort_order     int
show_on_personal      bool
featured_on_personal  bool      -- requires show_on_personal
personal_sort_order   int
```

Sort orders are never client-settable on create/update: new rows go to the end of both orders and
only change via `POST /api/cms/admin/{entity}/reorder` with `site` in the body. (Services currently
accept sort orders on create/update.) `faqs`, `pricing_plans` and `certificates` don't use this
block — see below.

## projects

```
id                  uuid pk
slug                text unique       -- from title; stable once published
title               text
year                int               -- 1990 .. next year
short_description   text              -- card blurb
description         text              -- markdown
website_url         text null         -- absolute http(s)
problem, solution, what_we_delivered, proof   text null   -- markdown
client_name         text null
is_published        bool
published_at        timestamptz null  -- stamped on first publish, never cleared
seo_title, seo_description   text null
+ site visibility block
```

`project_images` (hard delete; the stored file is deleted after the row commits):

```
id, project_id fk, object_key text, url text, alt_text text (required),
width int, height int, is_primary bool, sort_order int
```

Exactly one primary: `create unique index on project_images (project_id) where is_primary;`

`project_tags` — join `(project_id, tag_id)`, PK on both.

## products

Same shape as projects without tags or case-study fields. Pricing is free-form markdown, so
products never enter currency conversion.

```
id, slug text unique, name, tagline, description (markdown), price_details text null (markdown),
product_url text null (absolute http(s)), is_published, published_at null,
seo_title null, seo_description null
+ site visibility block
```

`product_images` — identical to `project_images`, with the same single-primary partial index.

## articles

No dedicated page; all articles render on one list.

```
id, title, excerpt, slug text unique,
content_markdown text null   -- markdown with media:// tokens
cover_image_key text null    -- media:// token or legacy URL
is_published, published_at null
+ site visibility block
```

Media inside articles is stored as `media://{objectKey}`, never a real URL. The public read path
resolves tokens with `IArticleMediaResolver`; admin responses return raw tokens. Legacy real URLs
pass through unchanged. `article_tags` — join `(article_id, tag_id)`.

## tags

```
id, name, slug text unique, is_technology bool,
technology_category tech_category null   -- required when is_technology, null otherwise
```

`tech_category`: `frontend | backend | language | database | tool_or_platform | cloud_devops |
ai_ml_dl | agentic_ai | design | other`

A tag can't be deleted while a live project or article uses it (`tag_in_use`).

## services

Every page section is a scalar or markdown field; images are uploaded assets.

```
id, slug text unique, name, short_description (markdown card blurb),
eyebrow, headline (falls back to name), deck                 text null
who_this_is_for, outcomes, capabilities (markdown lists), in_depth (markdown)   text null
primary_cta_label + primary_cta_url, secondary_cta_label + secondary_cta_url    text null, set together
icon_*, hero_image_*, depth_image_*: object_key, url, width, height, alt_text   null; alt_text required when set
seo_title, seo_description null, is_published, published_at null
+ site visibility block
```

`service_projects` — join `(service_id, project_id)`; the service's case studies. The public detail
endpoint only embeds projects and FAQs that are published and shown on the requested site.

## pricing_plans

Service tiers and combo packs (`service_id = null`) share one table. Agency-only: one `featured`
flag and one `sort_order` (reorder only, within its group).

```
id, service_id uuid null fk, name, tagline null,
price_amount numeric(12,2) null   -- null = "Custom / Contact us", never 0
currency char(3), price_type (fixed | starting_from | hourly | monthly | custom),
delivery_text null, description, is_popular, cta_label null, cta_url null,
is_published, featured, sort_order
```

`pricing_plan_features` — `id, pricing_plan_id fk, text, is_included bool, sort_order` (hard delete).

## certificates

Personal-site only: no visibility block, one `featured` flag and one global `sort_order` (reorder only).

```
id, name, issued_by,
issued_date date     -- required, not in the future
marks text null
object_key, url      -- url is absolute http(s)
mime_type            -- application/pdf or image/*
width, height int null   -- set together, positive; required for images, null allowed for PDFs
alt_text             -- required
is_published, featured, sort_order
```

Replacing the file on update deletes the old object after the save. Soft delete keeps the file.

## faqs

Own visibility flags, no featured flag, one sort order for both sites.

```
id, question, answer (markdown), service_id uuid null fk, sort_order,
is_published, show_on_agency, show_on_personal
```

`service_id` null = the global list; otherwise the FAQ appears only on that service's page.
`sort_order` is ordered within that scope.

## reviews

Not site-scoped; one pool for both sites.

```
id, name (100), country (100), country_code (2, ISO 3166-1), position null (100),
rating int (CHECK 1-5), review_text (2000), is_published (false on submission),
is_featured, sort_order, submitter_ip null (admin only)
```

Submitted anonymously via `POST /api/public/reviews`; see auth.md for rate limits.

## currencies

```
id, code char(3) unique (ISO 4217, upper-case), name (100), symbol (10),
manual_rate_from_usd numeric(18,6) null   -- admin override
live_rate_from_usd numeric(18,6) null     -- from the last refresh
live_rate_fetched_at timestamptz null, is_active
```

Effective rate = `manual ?? live`, computed in C#. Currencies with neither are hidden publicly.
Live rates change only via `POST /cms/admin/currencies/refresh-rates`. USD is seeded, must have a
manual rate of exactly 1, and can't be deactivated, renamed (`cannot_rename_base_currency`) or
deleted (`cannot_delete_base_currency`). Other currencies can't be deleted while a plan uses them
(`currency_in_use`).

Conversion: `displayed = amount / rate(plan.currency) * rate(displayCurrency)`.

## contact_submissions

Admin inbox only; never public.

```
id, name (100), email citext (255), phone null (30), company null (150), subject null (200),
message (4000), service_id null fk (SET NULL), budget_range contact_budget_range null,
site site, status contact_submission_status (default new), admin_notes null,
replied_at null, replied_by null fk users (SET NULL), submitter_ip null
```

`contact_submission_status`: `new | read | replied | archived | spam` (spam set only by the honeypot).
`contact_budget_range`: `under_one_k | one_to_five_k | five_to_fifteen_k | over_fifteen_k | not_sure`.

## users

```
id, email citext unique, first_name, last_name, password_hash null, google_subject_id null unique,
avatar_url null, role user_role (super_admin | admin),
status user_status (email_verification_required | pending | approved | rejected | disabled),
approved_by null fk users, approved_at null, rejection_reason null, last_login_at null,
email_verified_at null
+ timestamps / soft delete
```

At most one super admin: `create unique index ix_users_single_super_admin on users (role) where role = 'super_admin';`

Token tables store only SHA-256 hashes and are not soft-deletable, so replays are recognised:

- `refresh_tokens` — `id, user_id fk, token_hash unique, expires_at, created_at, revoked_at null, replaced_by_token_id null`
- `email_verification_tokens` — `id, user_id fk, token_hash unique, expires_at (24h), created_at, used_at null`
- `password_tokens` — same plus `purpose password_token_purpose (setup 24h | reset 1h)`
- `login_attempts` — `id, email, ip_address null, action auth_attempt_action (login | password_reset), attempted_at`

Behaviour rules: `.claude/rules/auth.md`.

## Seeding

On startup: the super admin (identity from config, null password, emailed setup link) and the USD
base currency. Both are idempotent.
