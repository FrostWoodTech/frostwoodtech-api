---
paths:
  - "**/Services/**"
  - "**/*Service.cs"
  - "**/Validators/**"
  - "**/*Validator.cs"
---

# Service layer

The service layer is the **only** layer between the Functions and EF Core. It owns business
logic *and* data access. Do not introduce repositories, a unit-of-work wrapper, CQRS handlers,
or MediatR — the project is deliberately shallow so a junior developer can follow a request end
to end.

Shape: one service per aggregate (`ProjectService`, `ArticleService`, `TagService`,
`ServiceCatalogService`, `PricingService`, `FaqService`, `UserService`, `MediaService`).
Registered in DI in `Program.cs`, scoped.

The Function is thin: parse and bind input → call the service → map the result to a response
DTO → return. No EF Core queries in a Function.

## Validation rules the service layer must enforce

- `featured_on_agency` requires `show_on_agency`; same for personal. FAQs, pricing plans and
  certificates are exceptions — FAQs have no featured flag and share one `sort_order` across both
  sites; pricing plans are agency-only and certificates are personal-only, so both carry a plain
  `featured` flag with no `show_on_X` to require.
- `is_technology = false` ⇒ `technology_category` must be null.
- `is_technology = true` ⇒ `technology_category` is required.
- Exactly one `project_images.is_primary` per project. Setting a new primary clears the old one
  in the same transaction.
- `alt_text` is required on every image.
- Slug uniqueness, checked on create and on update. Warn (don't block) when a published entity's
  slug changes.
- `price_amount = null` is valid and means "Custom / Contact us" — don't default it to 0.
- A tag may not be deleted while a project or article still references it — `tag_in_use`.
- Public read paths always apply `is_published`, `is_deleted`, and the site flag. Never expose a
  service method that lets a caller skip those filters on the public surface.

## Queries

- Use `AsNoTracking()` on every read path.
- Project straight to the DTO in the query (`Select(...)`) rather than loading full entities and
  mapping afterwards — it keeps the SQL narrow.
- Load tags and images with explicit `Include`/projection. Watch for N+1 on list endpoints; a
  project list page must not issue one query per project for its tags.
- Soft delete is a `where is_deleted = false` filter — consider a global query filter on the
  DbContext so it can't be forgotten.
