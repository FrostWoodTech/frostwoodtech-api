---
paths:
  - "**/*NeonStorage*.cs"
  - "**/*Media*.cs"
  - "**/*Image*.cs"
  - "**/*Upload*.cs"
---

# Media (Neon Object Storage)

Image bytes never touch the API. The client uploads straight to Neon Object Storage (S3-compatible)
using a presigned PUT URL the API generates.

```
Admin SPA → POST /api/cms/admin/media/presigned-upload { target, slug?, objectKey? }
          ← { uploadUrl, objectKey, expiresAt }
Admin SPA → PUT direct to Neon Object Storage using uploadUrl
Admin SPA → POST /api/cms/admin/projects/{id}/images { objectKey, url, width, height, altText }
```

For an **article** image, the admin SPA stores `media://{objectKey}` — not the resolved URL —
in `content_markdown`/`cover_image_key`. This keeps the raw markdown short and readable while
editing, and keeps storage swappable without rewriting every article. The admin editor needs to
render tokens it round-trips (preview pane, cover-picker gallery), so it fetches
`GET /api/cms/admin/media/config` once per session (`{ publicBaseUrl }`, effectively static — no
per-image resolve) and swaps a token's `media://` prefix for that base URL itself, client-side.
See `.claude/rules/schema.md`'s `articles` section for how the **public** path resolves the same
tokens server-side via `IArticleMediaResolver`. Every other upload target (`projects`, `services`,
`tags`) stores the resolved `publicUrl` directly, as before — the token scheme is articles-only.

The presigned-upload endpoint requires a valid admin JWT — an open bucket policy would let
anyone fill the account.

`IMediaService` (`NeonStorageService`) is the only place that talks to Neon Object Storage —
an `AmazonS3Client` configured with `ServiceURL` pointed at Neon's endpoint and
`ForcePathStyle = true` (Neon's S3-compatible API is path-style, not virtual-hosted-style).

## What the DB stores

`object_key`, `url`, `width`, `height`, `alt_text`. Never per-size URLs — the frontends are
responsible for whatever responsive delivery the bucket/CDN in front of it supports.

`alt_text` is required. Reject the request if it's missing or blank.

## Folder convention

```
frostwoodtech/projects/{slug}/
frostwoodtech/services/
frostwoodtech/tags/
frostwoodtech/articles/{slug}/
frostwoodtech/certificates/
```

The client never sends a folder path — it sends `target` (`projects | services | tags |
articles | certificates`) plus a `slug` when the target is `projects` or `articles`, and the API
builds the folder (used as the S3 key prefix). The slug is re-run through `SlugGenerator`, so
nothing outside the base folder is reachable even with a stolen admin token. The root comes from
`NeonS3__BaseFolder`. A certificate's file can be a PDF or an image — the DB stores its
`mime_type` alongside the usual `object_key`/`url`, and `width`/`height` are nullable since a PDF
has no dimensions.

## Deletes

Soft-deleting a row does not delete the object in storage. Hard delete also destroys the object
by `object_key`, and must tolerate the object already being gone.

The object delete runs **after** the row is committed, never before — and a failed delete is
logged, not surfaced. Losing an orphan object is cheap; failing the delete because Neon Object
Storage was unreachable is not. `project_images` rows are the one hard delete that exists today
(see `ProjectService.DeleteImageAsync`).

## Config

`NeonS3__Endpoint`, `NeonS3__AccessKey`, `NeonS3__SecretKey`, `NeonS3__Region`,
`NeonS3__BucketName`, `NeonS3__BaseFolder` — bound to `NeonStorageOptions`. Secrets live in app
settings / Key Vault, never in a committed `local.settings.json`.
