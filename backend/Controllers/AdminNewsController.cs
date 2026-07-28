using System.Security.Claims;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/news")]
public sealed class AdminNewsController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminNewsDetailDto>> Get(int id, CancellationToken token)
    {
        var item = await db.NewsArticles.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.CoverMediaAsset).FirstOrDefaultAsync(x => x.Id == id, token);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminNewsDetailDto>> Update(int id, SaveAdminNewsRequest request, CancellationToken token)
    {
        var item = await db.NewsArticles.Include(x => x.Translations).ThenInclude(x => x.Language).Include(x => x.CoverMediaAsset).FirstOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        if (!Enum.TryParse<NewsArticleType>(request.ArticleType, true, out var type)) return ValidationProblem("Invalid article type.");
        if (type == NewsArticleType.External && string.IsNullOrWhiteSpace(request.ExternalUrl)) return ValidationProblem("External articles require an original article URL.");
        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        if (!request.Translations.Any(x => x.LanguageCode == "sq") || !request.Translations.Any(x => x.LanguageCode == "en")) return ValidationProblem("Albanian and English content are required.");
        foreach (var incoming in request.Translations)
        {
            if (!languages.TryGetValue(incoming.LanguageCode, out var language)) continue;
            if (await db.NewsArticleTranslations.AnyAsync(x => x.NewsArticleId != id && x.LanguageId == language.Id && x.Slug == incoming.Slug, token))
                return Conflict(new ProblemDetails { Title = "Duplicate slug", Detail = $"The {incoming.LanguageCode.ToUpperInvariant()} slug is already used.", Status = 409 });
            var translation = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (translation is null) { translation = new NewsArticleTranslation { LanguageId = language.Id, Language = language }; item.Translations.Add(translation); }
            translation.Title = incoming.Title.Trim(); translation.Slug = incoming.Slug.Trim().ToLowerInvariant();
            translation.Summary = incoming.Summary.Trim(); translation.Content = Sanitize(incoming.Content);
            translation.MetaTitle = Clean(incoming.MetaTitle); translation.MetaDescription = Clean(incoming.MetaDescription);
        }
        item.ArticleType = type; item.CoverMediaAssetId = request.CoverMediaAssetId;
        item.ExternalUrl = type == NewsArticleType.External ? Clean(request.ExternalUrl) : null;
        item.ExternalSourceName = type == NewsArticleType.External ? Clean(request.ExternalSourceName) : null;
        item.IsPublished = request.IsPublished; item.IsFeatured = request.IsFeatured;
        item.PublishedAt = request.IsPublished ? request.PublishedAt ?? item.PublishedAt ?? clock.UtcNow : request.PublishedAt;
        item.UpdatedAt = clock.UtcNow;
        db.AdminActivities.Add(new AdminActivity { AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
            Action = "Updated", EntityType = "NewsArticle", EntityId = id.ToString(), Summary = $"Updated {item.Translations.First(x => x.Language.Code == "sq").Title}", CreatedAt = clock.UtcNow });
        await db.SaveChangesAsync(token);
        item.CoverMediaAsset = request.CoverMediaAssetId.HasValue ? await db.MediaAssets.FindAsync([request.CoverMediaAssetId.Value], token) : null;
        return Ok(ToDto(item));
    }

    private static string Sanitize(string html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("target"); sanitizer.AllowedAttributes.Add("rel");
        sanitizer.PostProcessNode += (_, args) => {
            if (args.Node is IHtmlAnchorElement link) { link.Target = "_blank"; link.SetAttribute("rel", "noopener noreferrer"); }
        };
        return sanitizer.Sanitize(html ?? "");
    }
    private static AdminNewsDetailDto ToDto(NewsArticle x) => new(x.Id, x.ArticleType.ToString(), x.CoverMediaAssetId,
        x.CoverMediaAsset?.FileUrl, x.ExternalUrl, x.ExternalSourceName, x.IsPublished, x.IsFeatured, x.PublishedAt,
        x.CreatedAt, x.UpdatedAt, x.Translations.OrderBy(t => t.Language.Code).Select(t => new AdminNewsTranslationDto(
            t.Language.Code, t.Title, t.Slug, t.Summary, t.Content, t.MetaTitle, t.MetaDescription)).ToList());
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
