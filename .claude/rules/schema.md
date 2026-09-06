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

snake_case in Postgres, PascalCase entities in C#. Every content table also gets
`created_at`, `updated_at`, `is_deleted`.

## Site visibility block

Applies to `projects`, `articles`, `services`:

```
show_on_agency          bool
featured_on_agency      bool      -- home page card
agency_sort_order       int

show_on_personal        bool
featured_on_personal    bool
personal_sort_order     int
```

`faqs` and `pricing_plans` are exceptions — see below.

`featured_on_X` may only be true when `show_on_X` is true — enforce in the service layer.

## projects

Own dedicated page per project, addressed by slug.

```
id                  uuid pk
slug                text unique          -- from title, editable, stable once published
title               text
year                int
short_description   text                 -- card / list blurb
description         text                 -- markdown, long form
website_url         text null
problem             text null            -- markdown
solution            text null            -- markdown
what_we_delivered   text null            -- markdown
proof               text null            -- metrics/results, markdown, optional
client_name         text null
is_published        bool
published_at        timestamptz null
seo_title           text null
seo_description     text null
+ site visibility block
+ timestamps / soft delete
```

`agency_sort_order` / `personal_sort_order` are never client-settable on create/update — a new
project is appended to the end of both orders, and each only changes via
`POST /api/cms/admin/projects/reorder` (`site` in the body picks which column). Same rule as
`articles`.

`project_images`

```
id            uuid pk
project_id    uuid fk
cloudinary_id text
url           text
alt_text      text          -- required
width, height int
is_primary    bool          -- exactly one per project
sort_order    int
```

Single primary enforced by a partial unique index:
`create unique index on project_images (project_id) where is_primary;`

`project_tags` — join `(project_id, tag_id)`, PK on both columns.

## articles

No dedicated page. All articles render on one list page. An article carries its own Markdown
body (`content_markdown`).

```
id                uuid pk
title             text
excerpt           text                 -- the "few lines"
published_at      timestamptz null     -- stamped once, the first time is_published goes true; never cleared
content_markdown  text null            -- article body, raw markdown with media:// references
cover_image_key   text null            -- Neon Object Storage object key
slug              text unique          -- internal linking, optional
is_published      bool
+ site visibility block
+ timestamps / soft delete
```

`agency_sort_order` / `personal_sort_order` are never client-settable on create/update — a new
article is appended to the end of both orders, and each only changes via
`POST /api/cms/admin/articles/reorder` (`site` in the body picks which column). The admin list
itself sorts by `updated_at` descending, not either sort order — drafts have no meaningful site
order.

### Embedded media in `content_markdown` and `cover_image_key`

Images referenced inside the markdown body — and the cover image itself — are never stored as
real URLs. Both hold a storage-independent `media://{objectKey}` token instead, where `objectKey`
is exactly what an `/admin/media/presigned-upload` (`target: articles`) call produced (e.g.
`media://frostwoodtech/articles/{slug}/{guid}`). Nothing outside `content_markdown` tracks
embedded images — there is deliberately no `article_attachments` table, since the markdown text
is already the source of truth for what's embedded.

`IArticleMediaResolver` (`ArticleMediaResolver`) rewrites every `media://...` token — in both
fields — to a real `IMediaService.GetPublicUrl` URL, and only runs on the **public** read path;
admin responses hand back the raw token so the editor round-trips it unresolved. This is what
keeps stored article content portable: swapping storage providers means changing `GetPublicUrl`,
never rewriting every article row. A `cover_image_key`/`content_markdown` saved before this token
scheme existed already holds a real URL with no `media://` prefix — the resolver leaves it
untouched, so old articles keep rendering without a migration.

The admin SPA needs to render tokens it round-trips too (the editor's live preview, the
cover-picker gallery) — it fetches `GET /admin/media/config` (`{ publicBaseUrl }`, fetched once
per session) and resolves a token client-side by swapping `media://` for that base URL itself,
instead of a per-image resolve call.

`article_tags` — join `(article_id, tag_id)`.

## tags

One table for both project categories and technologies.

```
id                  uuid pk
name                text
slug                text unique
is_technology       bool
technology_category tech_category null   -- required when is_technology = true
```

`tech_category` enum:

```
frontend | backend | language | database | tool_or_platform |
cloud_devops | ai_ml_dl | agentic_ai | design | other
```

Validation: `is_technology = false` ⇒ `technology_category` must be null.
Technology tags and category tags are distinguished by `is_technology`, not by a separate
column — group them at read time.

## services

No feature bullet list as a child table — every section of the page is either a scalar or a
markdown field. `short_description` is the card blurb; `headline`/`deck`/`in_depth` and the three
markdown bullet-list fields (`who_this_is_for`, `outcomes`, `capabilities`) are the page's own
sections. Icon, hero and depth image are each an uploaded asset (Neon Object Storage, same
presigned-upload flow as project images), not a Lucide icon key.

```
id                       uuid pk
slug                     text unique
name                     text                 -- short label; falls back for headline when that's null
short_description        text                 -- markdown, card blurb
eyebrow                  text null            -- small badge above the headline, e.g. "Arizona · Website pages"
headline                 text null            -- page H1
deck                     text null            -- hero sub-paragraph
who_this_is_for          text null            -- markdown bullet list
outcomes                 text null            -- markdown bullet list, "what you walk away with"
capabilities             text null            -- markdown bullet list, "what you get"
in_depth                 text null            -- markdown, long-form prose block
primary_cta_label        text null            -- set together with primary_cta_url, or not at all
primary_cta_url          text null
secondary_cta_label      text null            -- same all-or-nothing rule
secondary_cta_url        text null
icon_object_key          text null            -- Neon object key; set together with the rest, or not at all
icon_url                 text null
icon_width               int null
icon_height              int null
icon_alt_text            text null            -- required whenever icon_object_key is set
hero_image_object_key    text null            -- same all-or-nothing rule as the icon fields
hero_image_url           text null
hero_image_width         int null
hero_image_height        int null
hero_image_alt_text      text null            -- required whenever hero_image_object_key is set
depth_image_object_key   text null            -- same all-or-nothing rule; the "in depth" section's image
depth_image_url          text null
depth_image_width        int null
depth_image_height       int null
depth_image_alt_text     text null            -- required whenever depth_image_object_key is set
seo_title                text null
seo_description          text null
is_published             bool
published_at             timestamptz null
+ site visibility block
+ timestamps / soft delete
```

`service_projects` — join `(service_id, project_id)`, PK on both columns, same shape as
`project_tags`. The case studies shown on a service's page. No `sort_order` on the join — related
projects order by that project's own site sort order, then `year` descending, same as every other
project list.

## pricing_plans

Per-service tiers and general combo packs share **one table**. `service_id = null` means combo
pack. They render as the same card, need the same feature list, the same home-page flag, and
the same admin form — splitting them duplicates all of it.

Agency-only — pricing never appears on the personal site, so this is deliberately not the shared
site visibility block: one `featured` flag and one `sort_order`, not a pair per site. `sort_order`
is never client-settable on create/update — a new plan is appended to the end of its own group's
order (combo packs together, a service's tiers together), and only changes via
`POST /api/cms/admin/pricing-plans/reorder` (drag-and-drop, no `site` in the body — same reasoning
as `faqs`).

```
id                uuid pk
service_id        uuid null fk services   -- NULL = general/combo package
name              text                    -- "Starter", "Growth", "Landing Page Combo"
tagline           text null
price_amount      numeric(12,2) null      -- NULL = "Custom / Contact us"
currency          char(3)                 -- 'LKR', 'USD'
price_type        price_type              -- fixed | starting_from | hourly | monthly | custom
delivery_text     text null               -- free text, e.g. "2–3 weeks"
description       text
is_popular        bool                    -- the highlighted middle card
cta_label         text null
cta_url           text null
is_published      bool
featured          bool                    -- home page card
sort_order        int                     -- drag-and-drop only, never typed
```

`pricing_plan_features` — `id, pricing_plan_id fk, text, is_included bool, sort_order`

`is_included = false` renders a greyed-out row in the comparison table.

Query `where service_id = @id` for a service page, `where service_id is null` for combo packs.
If a combo pack ever needs to span several services, add `pricing_plan_services
(pricing_plan_id, service_id)` and keep `service_id` as the primary owner.

## certificates

Personal-site only — certificates never appear on the agency site, so there is no site-visibility
block at all, not even a single-site pair. One `featured` flag and one `sort_order`, same shape
as `pricing_plans` but without a `service_id` scope — there's only ever one global order.
`sort_order` is never client-settable on create/update, and only changes via
`POST /api/cms/admin/certificates/reorder` (drag-and-drop, no `site` in the body).

```
id            uuid pk
name          text
issued_by     text
issued_date   date
marks         text null       -- free text: "95%", "Distinction", "8.5/10"
object_key    text            -- Neon Object Storage key
url           text
mime_type     text            -- e.g. "application/pdf", "image/png"
width         int null        -- null for a PDF, which has no dimensions
height        int null
alt_text      text            -- required, even for a PDF (describes what the certificate is)
is_published  bool
featured      bool
sort_order    int             -- drag-and-drop only, never typed
+ timestamps / soft delete
```

The uploaded file can be a PDF or an image — unlike every other uploaded-asset field group in
this schema, `width`/`height` are nullable rather than always-required, since a PDF has none.

## faqs

Deliberately not the shared site visibility block — no "featured" concept, and one `sort_order`
shared by both sites rather than one per site.

```
id, question, answer (markdown), service_id uuid null fk services, sort_order,
is_published, show_on_agency bool, show_on_personal bool, timestamps
```

No free-text category — one was tried and dropped. Scoping is a real FK instead: `service_id`
null means the general FAQ list both public sites render; a non-null value scopes the FAQ to that
service's own page only, same "null means general" convention `pricing_plans.service_id` already
uses. `sort_order` is ordered within whichever scope the FAQ belongs to, not across every FAQ.
Reordering via `POST /api/cms/admin/faqs/reorder` renumbers exactly the ids it's handed — no
`site` in the body, and no `serviceId` either, since it never needs to know the scope to renumber
it.

## reviews

Visitor-submitted testimonials. Not site-scoped — one shared pool feeds the home page slice and
the dedicated reviews page on both frontends, unlike the entities in the site visibility block
above.

```
id              uuid pk
name            text
country         text
country_code    text(2)          -- ISO 3166-1 alpha-2, e.g. 'US' — frontend renders the flag
position        text null
rating          int              -- 1-5, CHECK (rating BETWEEN 1 AND 5)
review_text     text
is_published    bool             -- false on submission; admin publishes
is_featured     bool             -- home page slice
sort_order      int
submitter_ip    text null        -- admin-only, spam moderation + the submission rate limit
+ timestamps / soft delete
```

Submitted anonymously through the one public **write** endpoint in the API
(`POST /api/public/reviews`) — everything else under `/api/public/*` is read-only. Rate limited
per IP; see `.claude/rules/auth.md`.

## currencies

Display currencies. Each carries **two independent rates** rather than one:

```
id                     uuid pk
code                   char(3)         -- ISO 4217, unique, upper-cased: 'USD', 'LKR'
name                   text            -- max 100
symbol                 text            -- max 10, display only: 'Rs', '$'
manual_rate_from_usd   numeric(18,6) null   -- "price set by me" — admin-typed, sticky
live_rate_from_usd     numeric(18,6) null   -- "actual price" — from the last refresh
live_rate_fetched_at   timestamptz null     -- when that refresh happened
is_active              bool            -- whether visitors can pick it
+ timestamps / soft delete
```

**Effective rate** — what a visitor actually converts at — is
`manual_rate_from_usd ?? live_rate_from_usd`, computed on read (`Currency.EffectiveRateFromUsd`),
never stored. The manual override wins whenever both exist. A currency with neither is excluded
from `GET /public/currencies` rather than shown at a fabricated rate — still fully visible and
editable in the admin, which is how you notice "no rate yet, needs a refresh."

The live side never updates itself on a schedule — only `POST /cms/admin/currencies/refresh-rates`
(the admin's "Refresh live rates" button) touches it, via `IExchangeRateProvider`
(`OpenExchangeRateProvider`, hitting the free, keyless `open.er-api.com`) — one HTTP call updates
every currency's live rate at once, since the provider returns the full table for one request
regardless. A weekly/monthly Timer-triggered Function calling the same
`CurrencyService.RefreshLiveRatesAsync` is a natural future addition; none exists yet.

USD is the base every rate is expressed against, seeded on startup by `BaseCurrencySeeder` so it
always exists. The service layer pins it: USD's `manual_rate_from_usd` must always be present and
exactly 1 (its live rate is irrelevant, since the override always wins), it may not be deactivated,
renamed (`cannot_rename_base_currency`) or deleted (`cannot_delete_base_currency`). Any other
currency is refused deletion while a pricing plan still prices in it (`currency_in_use`), the same
shape as `tag_in_use`.

The list is admin-editable rather than a fixed enum — whatever is added, refreshed or given an
override, and marked active, becomes selectable on the public sites; every other visitor falls
back to USD.

`pricing_plans.currency` is unchanged and still means "the currency this amount is authored in".
Conversion is general rather than USD-only, so existing non-USD rows keep working with nothing to
back-fill:

```
displayed = amount / rate(plan.currency) * rate(displayCurrency)
```

## contact_submissions

A visitor's "contact us" enquiry. Not site-scoped in the visibility-block sense — like `reviews`
and `certificates` it is exempt — but `site` records which frontend the form was on. Unlike
`reviews`, this is never public content: there is no `is_published` and no public read endpoint
at all, only the admin inbox.

```
id              uuid pk
name            text             -- max 100
email           citext           -- max 255
phone           text null        -- max 30, free text as entered
company         text null        -- max 150
subject         text null        -- max 200
message         text             -- max 4000
service_id      uuid null fk services   -- "what are you interested in"; null = general enquiry
budget_range    contact_budget_range null
site            site             -- which frontend the form was on
status          contact_submission_status   -- defaults to 'new'
admin_notes     text null        -- admin-only, never leaves /cms/admin
replied_at      timestamptz null
replied_by      uuid null fk users
submitter_ip    text null        -- admin-only, spam moderation + the submission rate limit
+ timestamps / soft delete
```

`service_id` uses the same "null means general" convention as `faqs.service_id` and
`pricing_plans.service_id`. Both the `service_id` and `replied_by` foreign keys `SET NULL` on
delete — losing a service or an admin account must never destroy the enquiry that referenced it.

`contact_submission_status` enum: `new | read | replied | archived | spam`. `spam` is set by the
server (a filled honeypot field on the public submission), never chosen by the visitor.

`contact_budget_range` enum: `under_one_k | one_to_five_k | five_to_fifteen_k | over_fifteen_k |
not_sure` — spelled out because the snake_case enum translator mangles leading digits.

Submitted anonymously through `POST /api/public/contact`, one of the two public **write**
endpoints in the API (see also `reviews`). Rate limited per IP, more generously than reviews (5 in
24h, not 3) since a genuine prospect may legitimately follow up more than once; see
`.claude/rules/auth.md`.

## users

```
id                uuid pk
email             citext unique
full_name         text
password_hash     text null        -- null for Google-only accounts
google_subject_id text null unique
avatar_url        text null
role              user_role         -- super_admin | admin
status            user_status       -- email_verification_required | pending | approved | rejected | disabled
approved_by       uuid null fk users
approved_at       timestamptz null
rejection_reason  text null
last_login_at     timestamptz null
email_verified_at timestamptz null  -- set once, survives a later reject/disable
+ timestamps
```

`email_verification_tokens` — mirrors `refresh_tokens`' shape (hashed, single-use, not soft
deleted so a replayed link is still recognised):

```
id            uuid pk
user_id       uuid fk users, cascade delete
token_hash    text unique        -- sha-256 hex of the raw token
expires_at    timestamptz        -- 24h from issue
created_at    timestamptz
used_at       timestamptz null
```

`password_tokens` — identical shape and reasoning, for the links that give an account a password:
a first one (the seeded super admin) or a replacement (forgotten password). One table with a
discriminator, not two — only the lifetime and email wording differ.

```
id            uuid pk
user_id       uuid fk users, cascade delete
token_hash    text unique             -- sha-256 hex of the raw token
purpose       password_token_purpose  -- setup | reset
expires_at    timestamptz             -- 24h for setup, 1h for reset
created_at    timestamptz
used_at       timestamptz null
```

`login_attempts` carries an `action auth_attempt_action` (`login | password_reset`) so the two
rate-limited actions are counted independently — both indexes lead with it.

`users` also carries a filtered unique index so the database itself caps the super admin at one:

```sql
create unique index ix_users_single_super_admin on users (role) where "role" = 'super_admin';
```

Behaviour rules live in `.claude/rules/auth.md`.

## Seeding

Idempotent seeder ships the super admin — identity only, from app settings, with a null
`password_hash` and an emailed setup link — and the technology tag set.
