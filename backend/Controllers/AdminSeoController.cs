using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using System.Text.RegularExpressions;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin"), Route("api/admin/seo")]
public sealed class AdminSeoController(AppDbContext db, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("fix-safe")]
    public async Task<IActionResult> FixSafeWarnings(CancellationToken token)
    {
        var changed = 0;
        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        var shows = await db.Shows.Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var news = await db.NewsArticles.Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var pages = await db.StaticPages.Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var editions = await db.PitfEditions.Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var website = await db.TheatreInformation.FirstOrDefaultAsync(token);
        if (website is not null && !website.SocialSharingMediaAssetId.HasValue)
        {
            var fallbackImageId = website.LogoMediaAssetId ?? website.ReservationBannerMediaAssetId ??
                website.HeroBackgroundMediaAssetId ?? website.AboutPreviewMediaAssetId;
            if (fallbackImageId.HasValue)
            {
                website.SocialSharingMediaAssetId = fallbackImageId;
                changed++;
            }
        }
        var albums = await db.GalleryAlbums
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.GalleryAlbumMedia)
            .AsSplitQuery()
            .ToListAsync(token);
        foreach (var value in shows.SelectMany(x => x.Translations)) { var result = FillSeo(value.Title, value.ShortDescription, value.MetaTitle, value.MetaDescription); value.MetaTitle = result.Title; value.MetaDescription = result.Description; changed += result.Changed; }
        foreach (var value in news.SelectMany(x => x.Translations)) { var result = FillSeo(value.Title, value.Summary, value.MetaTitle, value.MetaDescription); value.MetaTitle = result.Title; value.MetaDescription = result.Description; changed += result.Changed; }
        foreach (var value in pages.SelectMany(x => x.Translations))
        {
            var source = FirstText(value.Content, value.Subtitle,
                value.Language.Code == "sq"
                    ? $"Informacione për {value.Title} në Teatrin AAB Faruk Begolli."
                    : $"Information about {value.Title} at AAB Theatre Faruk Begolli.");
            var result = FillSeo(value.Title, source, value.MetaTitle, value.MetaDescription);
            value.MetaTitle = result.Title; value.MetaDescription = result.Description; changed += result.Changed;
        }
        foreach (var value in editions.SelectMany(x => x.Translations)) { var result = FillSeo(value.Title, value.ShortDescription, value.MetaTitle, value.MetaDescription); value.MetaTitle = result.Title; value.MetaDescription = result.Description; changed += result.Changed; }

        var media = await db.MediaAssets.Include(x => x.Translations).ToDictionaryAsync(x => x.Id, token);
        foreach (var show in shows.Where(x => x.PosterMediaAssetId.HasValue))
            changed += SetAlt(media, languages, show.PosterMediaAssetId!.Value, show.Translations, "Poster for {0}", "Posteri i shfaqjes {0}");
        foreach (var article in news.Where(x => x.CoverMediaAssetId.HasValue))
            changed += SetAlt(media, languages, article.CoverMediaAssetId!.Value, article.Translations, "Cover image for {0}", "Fotografia kryesore për {0}");
        foreach (var edition in editions.Where(x => x.CoverMediaAssetId.HasValue))
            changed += SetAlt(media, languages, edition.CoverMediaAssetId!.Value, edition.Translations, "Cover image for {0}", "Fotografia kryesore për {0}");
        foreach (var page in pages.Where(x => x.FeaturedMediaAssetId.HasValue))
            changed += SetAlt(media, languages, page.FeaturedMediaAssetId!.Value, page.Translations, "Featured image for {0}", "Fotografia kryesore për {0}");
        foreach (var album in albums)
        foreach (var item in album.GalleryAlbumMedia.OrderBy(x => x.DisplayOrder))
            changed += SetAlt(media, languages, item.MediaAssetId, album.Translations,
                $"Gallery image {item.DisplayOrder + 1} from {{0}}",
                $"Fotografia {item.DisplayOrder + 1} nga galeria e {{0}}");
        if (website is not null)
        {
            changed += SetAltText(media, languages, website.LogoMediaAssetId, "Logo of AAB Theatre Faruk Begolli", "Logoja e Teatrit AAB Faruk Begolli");
            changed += SetAltText(media, languages, website.SocialSharingMediaAssetId, "AAB Theatre Faruk Begolli", "Teatri AAB Faruk Begolli");
            changed += SetAltText(media, languages, website.PitfFeatureMediaAssetId, "Prishtina International Theater Festival", "Festivali Ndërkombëtar i Teatrit në Prishtinë");
            changed += SetAltText(media, languages, website.PitfPageMediaAssetId, "Prishtina International Theater Festival", "Festivali Ndërkombëtar i Teatrit në Prishtinë");
        }

        await db.SaveChangesAsync(token);
        return Ok(new { changed, message = $"Safely completed {changed} missing SEO or accessibility fields from existing content." });
    }

    [HttpGet("issues")]
    public async Task<ActionResult<AdminSeoOverviewDto>> Issues(CancellationToken token)
    {
        var issues = new List<AdminContentIssueDto>();
        var languages = await db.Languages.AsNoTracking().Where(x => x.IsActive).Select(x => x.Code).ToListAsync(token);
        var shows = await db.Shows.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var news = await db.NewsArticles.AsNoTracking()
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.CoverMediaAsset)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.GalleryAlbumMedia).ThenInclude(x => x.MediaAsset)
            .AsSplitQuery()
            .ToListAsync(token);
        var pages = await db.StaticPages.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var editions = await db.PitfEditions.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var website = await db.TheatreInformation.AsNoTracking().FirstOrDefaultAsync(token);
        var hasDefaultSocialImage = website is not null && new[]
        {
            website.SocialSharingMediaAssetId, website.LogoMediaAssetId, website.ReservationBannerMediaAssetId,
            website.HeroBackgroundMediaAssetId, website.AboutPreviewMediaAssetId
        }.Any(x => x.HasValue);

        foreach (var show in shows.Where(x => x.Status == ShowStatus.Published))
            AuditTranslations(issues, "Play", show.Id, show.Translations.Select(x => new SeoTranslation(x.Language.Code, x.Title, x.Slug, x.MetaTitle, x.MetaDescription)), languages, $"/admin/shows/{show.Id}", show.PosterMediaAssetId.HasValue || show.UseLocalGalleryFallback, "poster image");
        foreach (var article in news.Where(x => x.IsPublished))
            AuditTranslations(issues, "News article", article.Id, article.Translations.Select(x => new SeoTranslation(x.Language.Code, x.Title, x.Slug, x.MetaTitle, x.MetaDescription)), languages, $"/admin/news/{article.Id}",
                IsVideoLed(article), "cover image or video");
        foreach (var page in pages.Where(x => x.IsPublished))
            AuditTranslations(issues, "Static page", page.Id, page.Translations.Select(x => new SeoTranslation(x.Language.Code, x.Title, x.Slug, x.MetaTitle, x.MetaDescription)), languages, $"/admin/pages?page={page.Id}", page.SocialSharingMediaAssetId.HasValue || hasDefaultSocialImage, "social-sharing image");
        foreach (var edition in editions.Where(x => x.IsPublished))
            AuditTranslations(issues, "PITF edition", edition.Id, edition.Translations.Select(x => new SeoTranslation(x.Language.Code, x.Title, x.Slug, x.MetaTitle, x.MetaDescription)), languages, "/admin/pitf", edition.CoverMediaAssetId.HasValue, "cover image");

        var usedMediaIds = shows.SelectMany(x => new[] { x.PosterMediaAssetId, x.FeaturedMediaAssetId })
            .Concat(news.SelectMany(x => new[] { x.CoverMediaAssetId, x.CardThumbnailMediaAssetId }))
            .Concat(pages.SelectMany(x => new[] { x.FeaturedMediaAssetId, x.ParallaxMediaAssetId, x.SocialSharingMediaAssetId }))
            .Concat(editions.SelectMany(x => new[] { x.CoverMediaAssetId, x.LogoMediaAssetId })).Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();
        usedMediaIds.UnionWith(await db.GalleryAlbumMedia.AsNoTracking().Select(x => x.MediaAssetId).ToListAsync(token));
        var websiteMedia = website is null ? [] : new int?[] { website.HeroBackgroundMediaAssetId, website.AboutPreviewMediaAssetId, website.ReservationBannerMediaAssetId, website.PitfFeatureMediaAssetId, website.PitfPageMediaAssetId, website.LogoMediaAssetId, website.SocialSharingMediaAssetId };
        usedMediaIds.UnionWith(websiteMedia.Where(x => x.HasValue).Select(x => x!.Value));
        var media = await db.MediaAssets.AsNoTracking().Include(x => x.Translations).Where(x => x.IsActive).ToListAsync(token);
        foreach (var item in media.Where(x => x.MimeType.StartsWith("image/") && usedMediaIds.Contains(x.Id) && !x.Translations.Any(t => !string.IsNullOrWhiteSpace(t.AltText))))
            issues.Add(new("Warning", "Accessibility", "Media file", item.FileName, "This image has no alternative text in any language.", "/admin/media"));
        foreach (var item in media.Where(x => IsMissingFile(x.FileUrl)))
            issues.Add(new("Error", "Broken media", "Media file", item.FileName, "The database references this file, but the physical file is missing from storage.", "/admin/media"));

        var ticketLinks = await db.ShowPerformances.AsNoTracking().Where(x => x.TicketUrl != null).Select(x => new
        {
            x.Id, x.TicketUrl, x.StartDateTimeUtc,
            ShowTitle = x.Show.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? $"Play #{x.ShowId}"
        }).ToListAsync(token);
        foreach (var item in ticketLinks.Where(x => !ValidHttpUrl(x.TicketUrl)))
            issues.Add(new("Error", "Invalid ticket link", "Performance", PerformanceLabel(item.ShowTitle, item.StartDateTimeUtc), $"The saved ticket address is invalid: {item.TicketUrl}", $"/admin/performances?performance={item.Id}"));
        return Ok(new AdminSeoOverviewDto(issues.Count(x => x.Severity == "Error"), issues.Count(x => x.Severity == "Warning"),
            issues.Count(x => x.Severity == "Information"), issues.OrderBy(x => x.Severity == "Error" ? 0 : x.Severity == "Warning" ? 1 : 2).ThenBy(x => x.ContentType).ToList()));
    }

    private static void AuditTranslations(List<AdminContentIssueDto> issues, string type, int id, IEnumerable<SeoTranslation> values,
        IReadOnlyList<string> languages, string path, bool hasVisualMedia, string imageName)
    {
        var translations = values.ToList();
        var title = translations.FirstOrDefault(x => x.Language == "sq")?.Title ?? translations.FirstOrDefault()?.Title ?? $"{type} #{id}";
        foreach (var language in languages)
        {
            var value = translations.FirstOrDefault(x => x.Language == language);
            var languageName = language == "sq" ? "Albanian" : "English";
            var editPath = type == "Static page" ? $"{path}&language={language}" : path;
            if (value is null || string.IsNullOrWhiteSpace(value.Title)) issues.Add(new("Error", "Missing translation", type, title, $"The {languageName} title is missing.", editPath));
            else
            {
                if (!string.IsNullOrWhiteSpace(value.MetaTitle) && value.MetaTitle.Length > 60) issues.Add(new("Warning", "SEO title", type, title, $"The {languageName} SEO title is {value.MetaTitle.Length} characters; aim for 60 or fewer.", editPath));
                if (string.IsNullOrWhiteSpace(value.MetaDescription)) issues.Add(new("Warning", "Meta description", type, title, $"The {languageName} meta description is empty.", editPath));
                else if (value.MetaDescription.Length > 160) issues.Add(new("Warning", "Meta description", type, title, $"The {languageName} meta description is {value.MetaDescription.Length} characters; aim for 160 or fewer.", editPath));
                if (string.IsNullOrWhiteSpace(value.Slug)) issues.Add(new("Error", "Page address", type, title, $"The {languageName} page address is empty.", editPath));
            }
        }
        if (!hasVisualMedia) issues.Add(new("Warning", "Social image", type, title, $"No {imageName} is selected.", path));
    }

    private bool IsMissingFile(string url) => !MediaStoragePath.Exists(environment, url);
    private static string PerformanceLabel(string showTitle, DateTimeOffset start) =>
        $"{showTitle} — {start.UtcDateTime:dd MMM yyyy, HH:mm} UTC";
    private static bool IsVideoLed(NewsArticle article) =>
        article.CoverMediaAssetId.HasValue ||
        article.GalleryAlbums.SelectMany(x => x.GalleryAlbumMedia).Any(x => x.MediaAsset.MimeType.StartsWith("video/")) ||
        article.Translations.Any(x => string.IsNullOrWhiteSpace(x.Content) && Regex.IsMatch(x.Title, @"\b(video|trailer)\b", RegexOptions.IgnoreCase));

    private static string FirstText(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static (string? Title, string? Description, int Changed) FillSeo(string title, string source, string? metaTitle, string? metaDescription)
    {
        var changed = 0;
        if (string.IsNullOrWhiteSpace(metaTitle)) { metaTitle = title.Trim(); changed++; }
        if (metaTitle?.Length > 60) { metaTitle = Shorten(metaTitle, 60); changed++; }
        if (string.IsNullOrWhiteSpace(metaDescription))
        {
            var text = Regex.Replace(source ?? "", "<[^>]+>", " ");
            metaDescription = Regex.Replace(text, @"\s+", " ").Trim();
            if (!string.IsNullOrWhiteSpace(metaDescription)) changed++;
        }
        if (metaDescription?.Length > 160) { metaDescription = Shorten(metaDescription, 160); changed++; }
        return (metaTitle, metaDescription, changed);
    }

    private static string Shorten(string value, int maximumLength)
    {
        var text = Regex.Replace(value, @"\s+", " ").Trim();
        if (text.Length <= maximumLength) return text;
        var contentLimit = maximumLength - 3;
        var cut = text.LastIndexOf(' ', contentLimit);
        if (cut < maximumLength / 2) cut = contentLimit;
        return text[..cut].TrimEnd(' ', ',', ';', ':', '-', '.') + "...";
    }
    private static int SetAlt<T>(IReadOnlyDictionary<int, MediaAsset> media, IReadOnlyDictionary<string, Language> languages, int mediaId, IEnumerable<T> translations, string enPattern, string sqPattern) where T : class
    {
        if (!media.TryGetValue(mediaId, out var asset)) return 0;
        var titles = translations.Select(value => new { Language = (Language)value.GetType().GetProperty("Language")!.GetValue(value)!, Title = (string)value.GetType().GetProperty("Title")!.GetValue(value)! }).ToDictionary(x => x.Language.Code, x => x.Title);
        var changed = 0;
        foreach (var code in new[] { "sq", "en" })
        {
            if (!languages.TryGetValue(code, out var language) || !titles.TryGetValue(code, out var title)) continue;
            var row = asset.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (row is null) { row = new MediaAssetTranslation { LanguageId = language.Id }; asset.Translations.Add(row); }
            if (!string.IsNullOrWhiteSpace(row.AltText)) continue;
            row.AltText = string.Format(code == "sq" ? sqPattern : enPattern, title); changed++;
        }
        return changed;
    }
    private static int SetAltText(IReadOnlyDictionary<int, MediaAsset> media, IReadOnlyDictionary<string, Language> languages, int? mediaId, string english, string albanian)
    {
        if (!mediaId.HasValue || !media.TryGetValue(mediaId.Value, out var asset)) return 0;
        var changed = 0;
        foreach (var value in new[] { (Code: "sq", Text: albanian), (Code: "en", Text: english) })
        {
            if (!languages.TryGetValue(value.Code, out var language)) continue;
            var row = asset.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (row is null) { row = new MediaAssetTranslation { LanguageId = language.Id }; asset.Translations.Add(row); }
            if (!string.IsNullOrWhiteSpace(row.AltText)) continue;
            row.AltText = value.Text; changed++;
        }
        return changed;
    }
    private static bool ValidHttpUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    private sealed record SeoTranslation(string Language, string Title, string Slug, string? MetaTitle, string? MetaDescription);
}
