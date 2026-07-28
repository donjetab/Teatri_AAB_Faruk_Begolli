# Admin Panel foundation

## Existing architecture inspected

- `frontend/src/App.jsx` owns React Router routes. The app uses `HashRouter`, React 19, Axios, and project CSS; there was no form framework or admin component library to reuse.
- `frontend/src/api/client.js` is the shared API client. Admin calls reuse it with credentials enabled.
- `backend/Program.cs` is the application composition root. Controllers use attribute routing and services use async Entity Framework queries.
- `backend/Data/AppDbContext.cs`, `backend/Models/Entities.cs`, and `backend/Data/Configurations/AppModelConfiguration.cs` define a translation-first SQL Server model.
- `TheatreInformation` already drives homepage and theatre details, so the admin editors extend it rather than creating duplicate settings.
- `Show`, `NewsArticle`, `PitfEdition`, `MediaAsset`, `ContactMessage`, and `NewsletterSubscriber` provide the real data used by dashboard summaries.
- `ErrorHandlingMiddleware` provides safe problem responses without internal exception details.

No prior authentication or authorization approach existed.

## Foundation added

- Cookie authentication uses an HTTP-only, strict same-site cookie and an eight-hour sliding session.
- Policies distinguish `SuperAdmin` and `ContentEditor`. Administration links are hidden from content editors and sensitive future endpoints should require the `SuperAdmin` policy.
- The first super admin is created only when the database has no admin users and all `AdminBootstrap` environment settings are supplied. No password is committed or seeded.
- Admin API routes live under `/api/admin` and require authorization except login.
- Admin React routes live under `#/admin`, with a protected route boundary and automatic login redirect.
- Shared admin layout, table, status, loading, empty, toast, language-tab and page-header components are in `frontend/src/admin/components`.

## Implemented phase screens

- Dashboard: real content counts and recent records. It intentionally contains no reservation statistics. Broken-link scanning is reported as zero until a scanner is implemented; potentially unused media is clearly labeled.
- Website Information: validated contact/social/reservation fields, bilingual theatre/footer content, branding references, save feedback, unload warning, and public preview.
- Homepage: bilingual section editor, section visibility, media references, news count, reservation link, save state, and preview.
- Translations: paginated missing Albanian/English issue list for shows and news, with content-type filtering.
- Reservations: an explicit placeholder with no fake data or calculations.
- Remaining navigation destinations have secure modular placeholders for later CRUD slices.

## Local first-admin setup

Set these only in the backend process environment, then start the API so migrations run:

```text
AdminBootstrap__Email=admin@example.com
AdminBootstrap__Password=<unique password, at least 12 characters>
AdminBootstrap__DisplayName=Theatre Administrator
```

Remove the bootstrap password from the process configuration after the user is created.

## Migration

`20260728152000_AddAdminPanelFoundation` adds admin users, activity records, branding/homepage fields, indexes, and foreign keys. The existing public entities and routes remain intact.
