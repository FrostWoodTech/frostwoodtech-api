---
paths:
  - "**/Services/**"
  - "**/*Service.cs"
  - "**/Validators/**"
  - "**/*Validator.cs"
---

# Service layer

The only layer between Functions and EF Core; it owns business logic **and** data access. No
repositories, unit-of-work wrappers, CQRS or MediatR.

One service per aggregate, registered scoped in `Program.cs`. Services return
`ServiceResult<T>` for business failures instead of throwing; Functions map them with
`ProblemResults.FromError`.

## Validation rules

- `featured_on_X` requires `show_on_X`. FAQs have no featured flag; pricing plans and certificates
  have a plain `featured` flag.
- Tags: `is_technology` ⇔ `technology_category` is set.
- Exactly one primary image per project/product. The first image is always primary; setting a new
  primary clears the old one (two saves in one transaction, because the partial unique index is
  checked per statement); un-setting the current primary is ignored; deleting it promotes the next.
- `alt_text` is required on every image.
- Slugs: generated with `SlugGenerator`, rejected when empty, unique **including soft-deleted rows**
  (use `IgnoreQueryFilters()` for the check, since the unique index covers them). The same applies
  to currency codes.
- Project/product URLs, image URLs and certificate URLs must be absolute http(s).
- `price_amount = null` means "Custom / Contact us" — never default it to 0.
- A tag can't be deleted while a live project or article uses it (`tag_in_use`).
- Unknown foreign ids (tags, projects, services) are validation errors, not 500s.
- Sort orders never come from create/update (services excepted); new rows are appended.

## Trash

- Every content service has `GetTrashAsync`, `RestoreAsync` and `PurgeAsync`. Users are excluded.
- `DeleteAsync` sets `DeletedBy = _currentUser.UserId`; the DbContext stamps `DeletedAt` and clears both
  on restore. The DbContext can't take `CurrentUser`: it is pooled, and the pool needs a single
  `DbContextOptions` constructor.
- Load trashed rows with `_db.X.FindTrashedAsync(id, ct)` / `.Trashed()` (`Data/TrashQueries.cs`), so a
  live row is `not_found` for restore and purge.
- Purge starts with `_currentUser.RequireSuperAdmin<bool>()`. Read the object keys **before** removing
  the row, and delete the files after the commit.
- Purge guards count with `IgnoreQueryFilters()`, the opposite of the soft-delete guards: a deleted row
  still holds its `Restrict` links, and dropping one would strip a later restore. Codes: `tag_in_use`,
  `project_in_use`, `service_in_use`, `currency_in_use`.
- Restore has no slug guard: unique indexes cover deleted rows, so nothing can take a trashed slug. The
  real guards: `service_deleted` (FAQ or plan whose service is deleted) and `currency_deleted`.

## Publishing

`published_at` is stamped on the first publish and never cleared. `SetPublishedAsync` shows the row
on both sites **only on the first publish** (`published_at is null`); republishing keeps the
editor's site choice.

## Queries

- Public reads always apply `is_published`, the site flag, and (via the global filter) `is_deleted`.
  Never add a method that lets a public caller skip them.
- `AsNoTracking()` on reads; project straight to DTOs with `Select`; avoid N+1 on lists.
- Use `IgnoreQueryFilters()` only where a deleted row must be found on purpose (slug checks,
  user lookups, seeders).
- File deletes run after the database commit.
