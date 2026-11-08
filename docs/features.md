# Backend features

What the FrostWoodTech CMS API offers today. Public = read by the agency site, anonymous.
Admin = the admin SPA, JWT required. Endpoint details: `FrostWoodTech.API/Docs/openapi.yaml` (served at `/api/docs`).

## Content

| Feature | Public | Admin |
|---|---|---|
| **Projects** (case studies) | List (filter by tag, category, featured); detail by slug | Create, edit, publish, reorder, images (one primary), trash |
| **Products** | List; detail by slug | Same as projects, including images |
| **Articles** | List (filter by tag, featured); detail by slug | Create, edit, publish, reorder, trash; images embedded in the Markdown |
| **Services** | List; detail page with its linked projects and FAQs | Create, edit, publish, reorder, link projects, icon/hero/depth images, trash |
| **Pricing plans** | Combo packs; the pricing tiers for one service | Create, edit, publish, reorder, per-plan feature list, trash |
| **FAQs** | General FAQs; service-specific FAQs on that service's page | Create, edit, reorder, trash |
| **Tags** | List, filtered to technology or category tags | Create, edit, trash; a tag in use can't be deleted |
| **Currencies** | Active currencies with rates, for converting pricing | Create, edit, manual rate or live-rate refresh, trash; USD is the fixed base |
| **Reviews** | List (sort by latest, rating, country); visitors submit, held for approval | Approve/publish, feature, reorder, trash |
| **Home page** | One call: featured projects, articles, services, pricing, plus FAQs and reviews | — |

## Contact form

- Visitors submit enquiries; rate limited, with a hidden spam-trap field, and new enquiries send an email notification.
- Admin inbox: filter by status or service, set status (new, read, replied, archived, spam), add notes, trash.

## Admin accounts and sign-in

- Register with email and password, or sign in with Google.
- Email verification, forgot/reset password, change password, login rate limiting.
- User approval: new users wait until a super admin approves, rejects or disables them. One super admin is created on first start.

## Shared admin tools

- **Trash:** every content type and the contact inbox can be soft-deleted, restored, or permanently deleted (super admin only).
- **Media:** images upload directly to Neon Object Storage; the backend stores only the location, size and alt text.
- **Docs and health:** interactive API docs (`/api/docs`), the OpenAPI spec, and a health check.
