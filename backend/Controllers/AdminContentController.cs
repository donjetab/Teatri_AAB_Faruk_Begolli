using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/content")]
public sealed class AdminContentController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet("website-information")]
    public async Task<ActionResult<WebsiteInformationDto>> GetWebsiteInformation(CancellationToken token)
    {
        var info = await WebsiteInformationQuery().FirstOrDefaultAsync(token);
        return info is null ? NotFound() : Ok(ToWebsiteDto(info));
    }

    [HttpPut("website-information")]
    public async Task<ActionResult<WebsiteInformationDto>> UpdateWebsiteInformation(UpdateWebsiteInformationRequest request, CancellationToken token)
    {
        var info = await WebsiteInformationQuery(false).FirstOrDefaultAsync(token);
        if (info is null) return NotFound();
        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        if (request.Translations.Any(x => !languages.ContainsKey(x.LanguageCode)))
            return ValidationProblem("One or more language codes are invalid.");
        info.Address = request.Address.Trim();
        info.Phone = request.Phone.Trim();
        info.Email = request.Email.Trim().ToLowerInvariant();
        info.FacebookUrl = Clean(request.FacebookUrl);
        info.InstagramUrl = Clean(request.InstagramUrl);
        info.ReservationUrl = NormalizeReservationUrl(request.ReservationUrl);
        info.LogoMediaAssetId = request.LogoMediaAssetId;
        info.FaviconMediaAssetId = request.FaviconMediaAssetId;
        info.SocialSharingMediaAssetId = request.SocialSharingMediaAssetId;
        foreach (var item in request.Translations)
        {
            var language = languages[item.LanguageCode];
            var translation = info.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (translation is null)
            {
                translation = new TheatreInformationTranslation { LanguageId = language.Id };
                info.Translations.Add(translation);
            }
            translation.TheatreName = item.TheatreName.Trim();
            translation.AddressDisplayText = item.AddressDisplayText.Trim();
            translation.FooterCopyrightText = item.FooterCopyrightText.Trim();
        }
        info.UpdatedAt = clock.UtcNow;
        AddActivity("Updated", "WebsiteInformation", info.Id.ToString(), "Updated theatre contact, branding and footer settings");
        await db.SaveChangesAsync(token);
        var updated = await WebsiteInformationQuery().FirstAsync(x => x.Id == info.Id, token);
        return Ok(ToWebsiteDto(updated));
    }

    [HttpGet("homepage")]
    public async Task<ActionResult<AdminHomepageDto>> GetHomepage(CancellationToken token)
    {
        var info = await db.TheatreInformation.Include(x => x.Translations).ThenInclude(x => x.Language).FirstOrDefaultAsync(token);
        return info is null ? NotFound() : Ok(ToHomepageDto(info));
    }

    [HttpPut("homepage")]
    public async Task<ActionResult<AdminHomepageDto>> UpdateHomepage(AdminHomepageDto request, CancellationToken token)
    {
        var info = await db.TheatreInformation.Include(x => x.Translations).ThenInclude(x => x.Language).FirstOrDefaultAsync(token);
        if (info is null || info.Id != request.Id) return NotFound();
        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        info.HeroBackgroundMediaAssetId = request.HeroMediaAssetId;
        info.AboutPreviewMediaAssetId = request.AboutMediaAssetId;
        info.ReservationBannerMediaAssetId = request.ReservationMediaAssetId;
        info.PitfFeatureMediaAssetId = request.PitfMediaAssetId;
        info.HeroIsVisible = request.HeroIsVisible;
        info.ReservationBannerIsVisible = request.ReservationBannerIsVisible;
        info.PitfFeatureIsVisible = request.PitfFeatureIsVisible;
        info.LatestNewsCount = Math.Clamp(request.LatestNewsCount, 1, 12);
        info.PrimaryButtonLink = Clean(request.PrimaryButtonLink);
        info.AboutButtonLink = Clean(request.AboutButtonLink);
        info.ReservationUrl = NormalizeReservationUrl(request.ReservationUrl);
        info.PitfDestinationUrl = Clean(request.PitfDestinationUrl);
        foreach (var item in request.Translations.Where(x => languages.ContainsKey(x.LanguageCode)))
        {
            var translation = info.Translations.First(x => x.LanguageId == languages[item.LanguageCode].Id);
            translation.HeroSlogan = item.HeroSlogan.Trim();
            translation.HeroSupportingText = Clean(item.HeroSupportingText) ?? string.Empty;
            translation.HeroButtonText = item.HeroButtonText.Trim();
            translation.AboutTitle = item.AboutTitle.Trim();
            translation.AboutShort = item.AboutShort.Trim();
            translation.AboutButtonText = item.AboutButtonText.Trim();
            translation.ReservationCallToActionTitle = item.ReservationTitle.Trim();
            translation.ReservationCallToActionText = item.ReservationText.Trim();
            translation.ReservationButtonText = item.ReservationButtonText.Trim();
            translation.PitfFeatureTitle = item.PitfTitle.Trim();
            translation.PitfShortDescription = item.PitfDescription.Trim();
            translation.PitfFeatureButtonText = item.PitfButtonText.Trim();
        }
        info.UpdatedAt = clock.UtcNow;
        AddActivity("Updated", "Homepage", info.Id.ToString(), "Updated homepage sections");
        await db.SaveChangesAsync(token);
        return Ok(ToHomepageDto(info));
    }

    [HttpGet("translations")]
    public async Task<ActionResult<PagedResultDto<TranslationIssueDto>>> GetTranslations([FromQuery] string? contentType, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token);
        var issues = new List<TranslationIssueDto>();
        var includes = (string value) => string.IsNullOrWhiteSpace(contentType) || contentType.Equals(value, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Equals("shows", StringComparison.OrdinalIgnoreCase))
        {
            var items = await db.Shows.AsNoTracking().Include(x => x.Translations).ToListAsync(token);
            foreach (var item in items) foreach (var language in languages)
            {
                var translation = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
                AddTranslationIssue(issues, "Show", item.Id, item.Translations.FirstOrDefault()?.Title ?? "Untitled", language.Code,
                    translation is null, translation is not null && (Blank(translation.Title) || Blank(translation.ShortDescription) || Blank(translation.FullDescription)), item.UpdatedAt);
            }
        }
        if (includes("news"))
        {
            var items = await db.NewsArticles.AsNoTracking().Include(x => x.Translations).ToListAsync(token);
            foreach (var item in items) foreach (var language in languages)
            {
                var translation = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
                AddTranslationIssue(issues, "News", item.Id, item.Translations.FirstOrDefault()?.Title ?? "Untitled", language.Code,
                    translation is null, translation is not null && (Blank(translation.Title) || Blank(translation.Summary)), item.UpdatedAt);
            }
        }
        if (includes("pages"))
        {
            var items = await db.StaticPages.AsNoTracking().Include(x => x.Translations).ToListAsync(token);
            foreach (var item in items) foreach (var language in languages)
            {
                var translation = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
                AddTranslationIssue(issues, "Page", item.Id, item.PageKey, language.Code, translation is null,
                    translation is not null && (Blank(translation.Title) || (item.PageKey == "about" && Blank(translation.Content))), item.UpdatedAt);
            }
        }
        if (includes("pitf"))
        {
            var items = await db.PitfEditions.AsNoTracking().Include(x => x.Translations).ToListAsync(token);
            foreach (var item in items) foreach (var language in languages)
            {
                var translation = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
                AddTranslationIssue(issues, "PITF", item.Id, $"PITF {item.Year}", language.Code, translation is null,
                    translation is not null && (Blank(translation.Title) || Blank(translation.ShortDescription) || Blank(translation.FullDescription)), item.UpdatedAt);
            }
        }
        if (includes("website"))
        {
            var items = await db.TheatreInformation.AsNoTracking().Include(x => x.Translations).ToListAsync(token);
            foreach (var item in items) foreach (var language in languages)
            {
                var translation = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
                AddTranslationIssue(issues, "Website", item.Id, "Homepage and website information", language.Code, translation is null,
                    translation is not null && (Blank(translation.TheatreName) || Blank(translation.HeroSlogan) || Blank(translation.AboutTitle)
                        || Blank(translation.AboutShort) || Blank(translation.ReservationCallToActionTitle) || Blank(translation.ReservationCallToActionText)), item.UpdatedAt);
            }
        }
        var ordered = issues.OrderByDescending(x => x.UpdatedAt).ToList();
        return Ok(new PagedResultDto<TranslationIssueDto>(ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, ordered.Count));
    }

    private static void AddTranslationIssue(List<TranslationIssueDto> issues, string type, int id, string title,
        string language, bool missing, bool incomplete, DateTimeOffset updatedAt)
    {
        if (missing || incomplete)
            issues.Add(new TranslationIssueDto(type, id, title, language, missing ? "Missing" : "Incomplete", updatedAt));
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

    private void AddActivity(string action, string type, string id, string summary) =>
        db.AdminActivities.Add(new AdminActivity { AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null, Action = action, EntityType = type, EntityId = id, Summary = summary, CreatedAt = clock.UtcNow });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private IQueryable<TheatreInformation> WebsiteInformationQuery(bool readOnly = true)
    {
        var query = db.TheatreInformation
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.LogoMediaAsset)
            .Include(x => x.FaviconMediaAsset)
            .Include(x => x.SocialSharingMediaAsset)
            .AsSplitQuery();
        return readOnly ? query.AsNoTracking() : query;
    }

    private static WebsiteInformationDto ToWebsiteDto(TheatreInformation x) => new(
        x.Id, x.Address, x.Phone, x.Email, x.FacebookUrl, x.InstagramUrl, x.ReservationUrl,
        x.LogoMediaAssetId, x.LogoMediaAsset?.FileUrl,
        x.FaviconMediaAssetId, x.FaviconMediaAsset?.FileUrl,
        x.SocialSharingMediaAssetId, x.SocialSharingMediaAsset?.FileUrl,
        x.Translations.Select(t => new LocalizedWebsiteInformationDto(t.Language.Code, t.TheatreName, t.AddressDisplayText, t.FooterCopyrightText)).ToList(), x.UpdatedAt);
    private static AdminHomepageDto ToHomepageDto(TheatreInformation x) => new(x.Id, x.HeroBackgroundMediaAssetId, x.AboutPreviewMediaAssetId, x.ReservationBannerMediaAssetId, x.PitfFeatureMediaAssetId, x.HeroIsVisible, x.ReservationBannerIsVisible, x.PitfFeatureIsVisible, x.LatestNewsCount, x.PrimaryButtonLink ?? "#/sq/shfaqjet", x.AboutButtonLink ?? "#/sq/per-ne", NormalizeReservationUrl(x.ReservationUrl), x.PitfDestinationUrl ?? "https://pitf.teatriaab.com/", x.Translations.Select(t => new LocalizedHomepageDto(t.Language.Code, t.HeroSlogan, t.HeroSupportingText, ValueOrDefault(t.HeroButtonText, t.Language.Code, "Shiko programin", "View program"), ValueOrDefault(t.AboutTitle, t.Language.Code, "Për Ne", "About"), t.AboutShort, ValueOrDefault(t.AboutButtonText, t.Language.Code, "Mëso më shumë", "Learn more"), t.ReservationCallToActionTitle, t.ReservationCallToActionText, ValueOrDefault(t.ReservationButtonText, t.Language.Code, "Rezervo biletën", "Reserve ticket"), ValueOrDefault(t.PitfFeatureTitle, t.Language.Code, "Prishtina International Theatre Festival", "Prishtina International Theatre Festival"), t.PitfShortDescription, ValueOrDefault(t.PitfFeatureButtonText, t.Language.Code, "Programi PITF", "PITF program"))).ToList(), x.UpdatedAt);
    private static string ValueOrDefault(string? value, string languageCode, string sq, string en) =>
        string.IsNullOrWhiteSpace(value) ? (languageCode == "sq" ? sq : en) : value;
    private static string? NormalizeReservationUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Equals("https://example.com/reservations", StringComparison.OrdinalIgnoreCase)
            ? "#/sq/rezervo"
            : value.Trim();
}
