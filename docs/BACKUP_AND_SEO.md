# Teatri AAB backup and SEO operations

## Backups

The site has two equally important data sets:

1. SQL Server database (`TheatreAab`): content, users, settings, media records and activity history.
2. `backend/wwwroot/uploads`: physical images, videos and documents.

Run from the repository root on the server:

```powershell
.\scripts\backup-teatri.ps1 -SqlServer "localhost\SQLEXPRESS" -Database "TheatreAab" -OutputRoot "E:\TeatriBackups"
```

The script creates a SQL Server checksum backup, runs `RESTORE VERIFYONLY`, archives uploads and writes SHA-256 hashes. A successful verification does not replace a restore drill.

Recommended schedule:

- database: daily, retain 30 daily and 12 monthly copies;
- uploads: daily after database backup, retain on the same schedule;
- keep at least three copies, on two storage types, with one encrypted copy off-site;
- restrict backup access because the database contains administrator and contact information;
- test a restore into a separate non-production database at least quarterly;
- monitor failures and storage capacity.

Never restore from the Admin Panel. Stop writes, preserve the current database/uploads, restore into a staging location, verify the application, and only then perform a controlled production cutover.

## SEO deployment checklist

- Replace fragment (`#/sq/...`) routing with History API URLs before production indexing.
- Configure the production host to return `index.html` for public routes while keeping `/api`, `/uploads`, `robots.txt` and `sitemap.xml` as real server paths.
- Generate an XML sitemap from published database content using the final HTTPS domain.
- Put the sitemap at the site root and reference it from `robots.txt`.
- Register the website in Google Search Console and submit the sitemap.
- Validate public URLs using URL Inspection, Rich Results Test and PageSpeed Insights.
- Ensure canonical and `hreflang` URLs use the final HTTPS origin, never localhost.
- Add unique metadata for each play and news detail page.
- Add Event structured data only for individual, published performances with accurate dates, venue and ticket URL.
- Monitor Search Console indexing, Core Web Vitals and structured-data reports after releases.
