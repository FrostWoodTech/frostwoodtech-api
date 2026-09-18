---
paths:
  - "**/*NeonStorage*.cs"
  - "**/*Media*.cs"
  - "**/*Image*.cs"
  - "**/*Upload*.cs"
---

# Media (Neon Object Storage)

File bytes never touch the API. The admin SPA uploads straight to Neon Object Storage
(S3-compatible) with a presigned PUT URL.

```
POST /api/cms/admin/media/presigned-upload { target, slug?, objectKey? }
  ← { uploadUrl, objectKey, publicUrl, expiresAt }      (15 min)
PUT  uploadUrl   (browser → storage)
POST /api/cms/admin/{projects|products}/{id}/images { objectKey, url, width, height, altText, isPrimary }
```

`IMediaService` (`NeonStorageService`) is the only code that talks to storage: an `AmazonS3Client`
with `ServiceURL` = Neon's endpoint and `ForcePathStyle = true`.

## What the DB stores

`object_key`, `url`, `width`, `height`, `alt_text` — never bytes or per-size URLs. `alt_text` is
required. Certificates also store `mime_type` (`application/pdf` or `image/*`); PDFs may omit
dimensions.

Articles store `media://{objectKey}` tokens instead of URLs, in both `content_markdown` and
`cover_image_key`. The public path resolves them server-side (`IArticleMediaResolver`); the admin SPA
resolves them client-side using `GET /api/cms/admin/media/config` (`{ publicBaseUrl }`).

## Folder convention

```
{BaseFolder}/projects/{slug}/    {BaseFolder}/products/{slug}/    {BaseFolder}/articles/{slug}/
{BaseFolder}/services/           {BaseFolder}/tags/               {BaseFolder}/certificates/
```

The client sends a `target`, never a folder. `slug` is required for projects, products and
articles and is re-run through `SlugGenerator`, so nothing outside the base folder is reachable.

## Deletes

- Soft-deleting a row never deletes its file.
- Hard-deleting an image row, or replacing a certificate's file, deletes the object **after** the
  row is committed. A failed delete is logged, never surfaced; a missing object counts as success.

## Config

`NeonS3:Endpoint`, `AccessKey`, `SecretKey`, `Region`, `BucketName`, `BaseFolder` (default
`frostwoodtech`), bound to `NeonStorageOptions`. Keys live in app settings / Key Vault.
