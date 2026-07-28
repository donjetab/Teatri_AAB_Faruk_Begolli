using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/{languageCode:regex(^(sq|en)$)}/news")]
[Produces("application/json")]
public sealed class NewsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NewsListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NewsListItemDto>>> Get(
        string languageCode,
        CancellationToken cancellationToken)
    {
        var languages = await GetLanguagesAsync(languageCode, cancellationToken);
        if (languages is null)
        {
            return NotFound();
        }

        var (requestedLanguageId, defaultLanguageId) = languages.Value;
        var articles = await db.NewsArticles
            .AsNoTracking()
            .Where(x => x.IsPublished && x.PublishedAt != null)
            .OrderByDescending(x => x.PublishedAt)
            .Select(x => new
            {
                x.Id,
                PublishedAt = x.PublishedAt!.Value,
                CoverUrl = x.CoverMediaAsset == null ? null : x.CoverMediaAsset.FileUrl,
                x.ArticleType,
                x.ExternalUrl,
                Requested = x.Translations.FirstOrDefault(t => t.LanguageId == requestedLanguageId),
                Fallback = x.Translations.FirstOrDefault(t => t.LanguageId == defaultLanguageId)
            })
            .ToListAsync(cancellationToken);

        return Ok(articles
            .Where(x => x.Requested != null || x.Fallback != null)
            .Select(x =>
            {
                var translation = x.Requested ?? x.Fallback!;
                return new NewsListItemDto(
                    x.Id,
                    translation.Title,
                    translation.Slug,
                    translation.Summary,
                    x.PublishedAt,
                    x.CoverUrl,
                    x.ArticleType == NewsArticleType.External,
                    x.ExternalUrl,
                    x.Requested is null);
            })
            .ToList());
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(NewsDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NewsDetailDto>> GetBySlug(
        string languageCode,
        string slug,
        CancellationToken cancellationToken)
    {
        var languages = await GetLanguagesAsync(languageCode, cancellationToken);
        if (languages is null)
        {
            return NotFound();
        }

        var (requestedLanguageId, defaultLanguageId) = languages.Value;
        var article = await db.NewsArticles
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.CoverMediaAsset)
            .Include(x => x.GalleryAlbums)
                .ThenInclude(x => x.GalleryAlbumMedia)
                    .ThenInclude(x => x.MediaAsset)
                        .ThenInclude(x => x.Translations)
            .FirstOrDefaultAsync(x =>
                x.IsPublished && x.Translations.Any(t => t.Slug == slug),
                cancellationToken);

        if (article?.PublishedAt is null)
        {
            return NotFound();
        }

        var translation = article.Translations.FirstOrDefault(x => x.LanguageId == requestedLanguageId)
            ?? article.Translations.FirstOrDefault(x => x.LanguageId == defaultLanguageId);
        if (translation is null)
        {
            return NotFound();
        }

        var media = article.GalleryAlbums
            .Where(x => x.IsPublished)
            .SelectMany(x => x.GalleryAlbumMedia)
            .OrderBy(x => x.DisplayOrder)
            .Select(x =>
            {
                var mediaTranslation = x.MediaAsset.Translations
                    .FirstOrDefault(t => t.LanguageId == requestedLanguageId)
                    ?? x.MediaAsset.Translations.FirstOrDefault(t => t.LanguageId == defaultLanguageId);
                return new NewsMediaDto(
                    x.MediaAsset.Id,
                    x.MediaAsset.FileUrl,
                    x.MediaAsset.MimeType,
                    mediaTranslation?.AltText ?? translation.Title,
                    mediaTranslation?.Caption,
                    x.DisplayOrder,
                    x.IsCover);
            })
            .ToList();

        return Ok(new NewsDetailDto(
            article.Id,
            translation.Title,
            translation.Slug,
            translation.Summary,
            translation.Content,
            article.PublishedAt.Value,
            article.CoverMediaAsset?.FileUrl,
            article.ArticleType == NewsArticleType.External,
            article.ExternalUrl,
            article.ExternalSourceName,
            translation.LanguageId != requestedLanguageId,
            media));
    }

    private async Task<(int RequestedLanguageId, int DefaultLanguageId)?> GetLanguagesAsync(
        string languageCode,
        CancellationToken cancellationToken)
    {
        var languages = await db.Languages
            .Where(x => x.Code == languageCode || x.IsDefault)
            .Select(x => new { x.Id, x.Code, x.IsDefault })
            .ToListAsync(cancellationToken);
        var requested = languages.FirstOrDefault(x => x.Code == languageCode);
        var fallback = languages.FirstOrDefault(x => x.IsDefault);
        return requested is null || fallback is null ? null : (requested.Id, fallback.Id);
    }
}
