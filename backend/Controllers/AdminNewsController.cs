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
    [HttpPost]
    public async Task<ActionResult<AdminNewsDetailDto>> Create(SaveAdminNewsRequest request, CancellationToken token)
    {
        var item = new NewsArticle
        {
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };
        var validationResult = await ApplyRequest(item, request, null, token);
        if (validationResult is not null) return validationResult;

        db.NewsArticles.Add(item);
        await db.SaveChangesAsync(token);
        AddActivity("Created", item.Id, $"Created {AlbanianTitle(item)}");
        await db.SaveChangesAsync(token);
        await LoadCover(item, token);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ToDto(item));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminNewsDetailDto>> Get(int id, CancellationToken token)
    {
        var item = await db.NewsArticles.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.CoverMediaAsset)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.GalleryAlbumMedia)
                .ThenInclude(x => x.MediaAsset).ThenInclude(x => x.Translations).ThenInclude(x => x.Language)
            .FirstOrDefaultAsync(x => x.Id == id, token);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminNewsDetailDto>> Update(int id, SaveAdminNewsRequest request, CancellationToken token)
    {
        var item = await db.NewsArticles.Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.CoverMediaAsset)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.GalleryAlbumMedia)
                .ThenInclude(x => x.MediaAsset).ThenInclude(x => x.Translations).ThenInclude(x => x.Language)
            .FirstOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        var validationResult = await ApplyRequest(item, request, id, token);
        if (validationResult is not null) return validationResult;
        item.UpdatedAt = clock.UtcNow;
        AddActivity("Updated", id, $"Updated {AlbanianTitle(item)}");
        await db.SaveChangesAsync(token);
        await LoadCover(item, token);
        return Ok(ToDto(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var item = await db.NewsArticles.Include(x => x.Translations).ThenInclude(x => x.Language)
            .FirstOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();

        var title = AlbanianTitle(item);
        db.NewsArticles.Remove(item);
        AddActivity("Deleted", id, $"Deleted {title}");
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpPost("{id:int}/gallery")]
    public async Task<ActionResult<AdminNewsDetailDto>> AttachGalleryMedia(
        int id, AttachNewsGalleryMediaRequest request, CancellationToken token)
    {
        var item = await LoadWithGallery(id, token);
        if (item is null) return NotFound();
        var media = await db.MediaAssets.FirstOrDefaultAsync(
            x => x.Id == request.MediaAssetId && x.IsActive && x.MimeType.StartsWith("image/"), token);
        if (media is null) return ValidationProblem("Choose an active image from the Media Library.");

        var album = item.GalleryAlbums.FirstOrDefault();
        if (album is null)
        {
            album = new GalleryAlbum
            {
                AlbumType = GalleryAlbumType.NewsArticle,
                NewsArticleId = item.Id,
                IsPublished = true,
                IsVisibleInGeneralGallery = false,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            };
            var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token);
            foreach (var language in languages)
            {
                var title = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id)?.Title ?? AlbanianTitle(item);
                album.Translations.Add(new GalleryAlbumTranslation
                {
                    LanguageId = language.Id,
                    Title = $"{title} Gallery",
                    Slug = $"news-{item.Id}-gallery-{language.Code}"
                });
            }
            item.GalleryAlbums.Add(album);
        }

        if (album.GalleryAlbumMedia.Any(x => x.MediaAssetId == media.Id))
            return Conflict(new ProblemDetails { Title = "Image already attached", Detail = "This image is already in the news gallery.", Status = 409 });

        var firstImage = album.GalleryAlbumMedia.Count == 0;
        album.GalleryAlbumMedia.Add(new GalleryAlbumMedia
        {
            MediaAssetId = media.Id,
            DisplayOrder = album.GalleryAlbumMedia.Count,
            IsCover = firstImage,
            IsFeatured = firstImage
        });
        if (firstImage || !item.CoverMediaAssetId.HasValue)
        {
            item.CoverMediaAssetId = media.Id;
            album.CoverMediaAssetId = media.Id;
        }
        album.UpdatedAt = item.UpdatedAt = clock.UtcNow;
        AddActivity("Added gallery image", item.Id, $"Added {media.FileName} to {AlbanianTitle(item)}");
        await db.SaveChangesAsync(token);
        return Ok(ToDto((await LoadWithGallery(id, token))!));
    }

    [HttpPut("{id:int}/gallery/{mediaId:int}/thumbnail")]
    public async Task<ActionResult<AdminNewsDetailDto>> SetThumbnail(int id, int mediaId, CancellationToken token)
    {
        var item = await LoadWithGallery(id, token);
        if (item is null) return NotFound();
        var album = item.GalleryAlbums.FirstOrDefault();
        if (album is null || !album.GalleryAlbumMedia.Any(x => x.MediaAssetId == mediaId))
            return ValidationProblem("Choose an image attached to this news article.");

        foreach (var galleryMedia in album.GalleryAlbumMedia)
        {
            galleryMedia.IsCover = galleryMedia.MediaAssetId == mediaId;
            galleryMedia.IsFeatured = galleryMedia.MediaAssetId == mediaId;
        }
        var orderedMedia = album.GalleryAlbumMedia
            .OrderBy(x => x.MediaAssetId == mediaId ? 0 : 1)
            .ThenBy(x => x.DisplayOrder)
            .ToList();
        for (var index = 0; index < orderedMedia.Count; index++)
            orderedMedia[index].DisplayOrder = index;

        item.CoverMediaAssetId = mediaId;
        album.CoverMediaAssetId = mediaId;
        album.UpdatedAt = item.UpdatedAt = clock.UtcNow;
        AddActivity("Changed thumbnail", item.Id, $"Changed the thumbnail for {AlbanianTitle(item)}");
        await db.SaveChangesAsync(token);
        return Ok(ToDto((await LoadWithGallery(id, token))!));
    }

    [HttpPut("{id:int}/gallery/order")]
    public async Task<ActionResult<AdminNewsDetailDto>> ReorderGallery(
        int id, ReorderNewsGalleryRequest request, CancellationToken token)
    {
        var item = await LoadWithGallery(id, token);
        if (item is null) return NotFound();
        var album = item.GalleryAlbums.FirstOrDefault();
        if (album is null) return NotFound();

        var currentIds = album.GalleryAlbumMedia.Select(x => x.MediaAssetId).OrderBy(x => x).ToArray();
        var requestedIds = request.MediaAssetIds.Distinct().OrderBy(x => x).ToArray();
        if (!currentIds.SequenceEqual(requestedIds))
            return ValidationProblem("The gallery order must contain every attached image exactly once.");

        var positions = request.MediaAssetIds
            .Select((mediaId, index) => new { mediaId, index })
            .ToDictionary(x => x.mediaId, x => x.index);
        foreach (var media in album.GalleryAlbumMedia)
            media.DisplayOrder = positions[media.MediaAssetId];

        album.UpdatedAt = item.UpdatedAt = clock.UtcNow;
        AddActivity("Reordered gallery", item.Id, $"Reordered images for {AlbanianTitle(item)}");
        await db.SaveChangesAsync(token);
        return Ok(ToDto((await LoadWithGallery(id, token))!));
    }

    [HttpDelete("{id:int}/gallery/{mediaId:int}")]
    public async Task<ActionResult<AdminNewsDetailDto>> DetachGalleryMedia(int id, int mediaId, CancellationToken token)
    {
        var item = await LoadWithGallery(id, token);
        if (item is null) return NotFound();
        var album = item.GalleryAlbums.FirstOrDefault();
        var galleryMedia = album?.GalleryAlbumMedia.FirstOrDefault(x => x.MediaAssetId == mediaId);
        if (album is null || galleryMedia is null) return NotFound();

        var removedThumbnail = galleryMedia.IsCover || item.CoverMediaAssetId == mediaId;
        db.GalleryAlbumMedia.Remove(galleryMedia);
        if (removedThumbnail)
        {
            var replacement = album.GalleryAlbumMedia
                .Where(x => x.MediaAssetId != mediaId)
                .OrderBy(x => x.DisplayOrder)
                .FirstOrDefault();
            if (replacement is not null)
            {
                replacement.IsCover = true;
                replacement.IsFeatured = true;
            }
            item.CoverMediaAssetId = replacement?.MediaAssetId;
            album.CoverMediaAssetId = replacement?.MediaAssetId;
        }
        album.UpdatedAt = item.UpdatedAt = clock.UtcNow;
        AddActivity("Removed gallery image", item.Id, $"Removed an image from {AlbanianTitle(item)}");
        await db.SaveChangesAsync(token);
        return Ok(ToDto((await LoadWithGallery(id, token))!));
    }

    private async Task<ActionResult?> ApplyRequest(
        NewsArticle item, SaveAdminNewsRequest request, int? existingId, CancellationToken token)
    {
        if (!Enum.TryParse<NewsArticleType>(request.ArticleType, true, out var type))
            return ValidationProblem("Invalid article type.");
        if (type == NewsArticleType.External && string.IsNullOrWhiteSpace(request.ExternalUrl))
            return ValidationProblem("External articles require an original article URL.");
        if (request.CoverMediaAssetId.HasValue &&
            !await db.MediaAssets.AnyAsync(x => x.Id == request.CoverMediaAssetId && x.IsActive, token))
            return ValidationProblem("The selected cover image does not exist.");

        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        if (!request.Translations.Any(x => x.LanguageCode == "sq") ||
            !request.Translations.Any(x => x.LanguageCode == "en"))
            return ValidationProblem("Albanian and English content are required.");

        foreach (var incoming in request.Translations)
        {
            if (!languages.TryGetValue(incoming.LanguageCode, out var language)) continue;
            if (string.IsNullOrWhiteSpace(incoming.Title) || string.IsNullOrWhiteSpace(incoming.Slug) ||
                string.IsNullOrWhiteSpace(incoming.Summary) || string.IsNullOrWhiteSpace(incoming.Content))
                return ValidationProblem($"Title, slug, summary and content are required in {incoming.LanguageCode.ToUpperInvariant()}.");

            var slug = incoming.Slug.Trim().ToLowerInvariant();
            if (await db.NewsArticleTranslations.AnyAsync(
                    x => (!existingId.HasValue || x.NewsArticleId != existingId.Value) &&
                         x.LanguageId == language.Id && x.Slug == slug, token))
                return Conflict(new ProblemDetails { Title = "Duplicate slug", Detail = $"The {incoming.LanguageCode.ToUpperInvariant()} slug is already used.", Status = 409 });
            var translation = item.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (translation is null) { translation = new NewsArticleTranslation { LanguageId = language.Id, Language = language }; item.Translations.Add(translation); }
            translation.Title = incoming.Title.Trim(); translation.Slug = slug;
            translation.Summary = incoming.Summary.Trim(); translation.Content = Sanitize(incoming.Content);
            translation.MetaTitle = Clean(incoming.MetaTitle); translation.MetaDescription = Clean(incoming.MetaDescription);
        }
        item.ArticleType = type; item.CoverMediaAssetId = request.CoverMediaAssetId;
        item.ExternalUrl = type == NewsArticleType.External ? Clean(request.ExternalUrl) : null;
        item.ExternalSourceName = type == NewsArticleType.External ? Clean(request.ExternalSourceName) : null;
        item.IsPublished = request.IsPublished; item.IsFeatured = request.IsFeatured;
        item.PublishedAt = request.IsPublished ? request.PublishedAt ?? item.PublishedAt ?? clock.UtcNow : request.PublishedAt;
        return null;
    }

    private void AddActivity(string action, int id, string summary)
    {
        db.AdminActivities.Add(new AdminActivity
        {
            AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
            Action = action,
            EntityType = "NewsArticle",
            EntityId = id.ToString(),
            Summary = summary,
            CreatedAt = clock.UtcNow
        });
    }

    private async Task LoadCover(NewsArticle item, CancellationToken token) =>
        item.CoverMediaAsset = item.CoverMediaAssetId.HasValue
            ? await db.MediaAssets.FindAsync([item.CoverMediaAssetId.Value], token)
            : null;

    private async Task<NewsArticle?> LoadWithGallery(int id, CancellationToken token) =>
        await db.NewsArticles
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.CoverMediaAsset)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.Translations)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.GalleryAlbumMedia)
                .ThenInclude(x => x.MediaAsset).ThenInclude(x => x.Translations).ThenInclude(x => x.Language)
            .FirstOrDefaultAsync(x => x.Id == id, token);

    private static string AlbanianTitle(NewsArticle item) =>
        item.Translations.FirstOrDefault(x => x.Language.Code == "sq")?.Title ?? "news article";

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
            t.Language.Code, t.Title, t.Slug, t.Summary, t.Content, t.MetaTitle, t.MetaDescription)).ToList(),
        x.GalleryAlbums.SelectMany(a => a.GalleryAlbumMedia).OrderBy(m => m.DisplayOrder)
            .Select(m => new AdminNewsGalleryMediaDto(
                m.MediaAssetId, m.MediaAsset.FileUrl, m.MediaAsset.FileName, m.MediaAsset.MimeType,
                m.MediaAsset.Translations.FirstOrDefault(t => t.Language.Code == "sq")?.Caption,
                m.MediaAsset.Translations.FirstOrDefault(t => t.Language.Code == "en")?.Caption,
                m.IsCover || x.CoverMediaAssetId == m.MediaAssetId))
            .ToList());
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
