using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/existing-content")]
public sealed class AdminExistingContentController(AppDbContext db) : ControllerBase
{
    [HttpGet("news")]
    public async Task<ActionResult<AdminExistingContentListDto>> News(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var all = db.NewsArticles.AsNoTracking().AsQueryable();
        var published = await all.CountAsync(x => x.IsPublished, token);
        var drafts = await all.CountAsync(x => !x.IsPublished, token);
        var query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Translations.Any(t => t.Title.Contains(term)));
        }
        if (status?.Equals("published", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(x => x.IsPublished);
        if (status?.Equals("draft", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(x => !x.IsPublished);
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.PublishedAt ?? x.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminExistingContentItemDto(x.Id,
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? "Pa titull",
                x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Title).FirstOrDefault() ?? "Untitled",
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Slug).FirstOrDefault() ?? "",
                x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Slug).FirstOrDefault() ?? "",
                x.IsPublished ? "Published" : "Draft", x.IsFeatured, x.UpdatedAt, x.PublishedAt,
                x.CoverMediaAsset == null ? null : x.CoverMediaAsset.FileUrl,
                x.ExternalSourceName ?? (x.ArticleType == NewsArticleType.External ? "External article" : "Authored article"), x.RelatedExternalLinks.Count)).ToListAsync(token);
        return Ok(new AdminExistingContentListDto(items, page, pageSize, total, published, drafts));
    }

    [HttpGet("pitf")]
    public async Task<ActionResult<AdminExistingContentListDto>> Pitf(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var all = db.PitfEditions.AsNoTracking().AsQueryable();
        var published = await all.CountAsync(x => x.IsPublished, token);
        var drafts = await all.CountAsync(x => !x.IsPublished, token);
        var query = all;
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.Translations.Any(t => t.Title.Contains(term)) || x.Year.ToString().Contains(term)); }
        if (status?.Equals("published", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(x => x.IsPublished);
        if (status?.Equals("draft", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(x => !x.IsPublished);
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.Year).ThenByDescending(x => x.EditionNumber).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminExistingContentItemDto(x.Id,
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? $"PITF {x.Year}",
                x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Title).FirstOrDefault() ?? $"PITF {x.Year}",
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Slug).FirstOrDefault() ?? "",
                x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Slug).FirstOrDefault() ?? "",
                x.IsPublished ? "Published" : "Draft", x.IsFeatured, x.UpdatedAt, x.IsPublished ? x.UpdatedAt : null,
                x.CoverMediaAsset == null ? null : x.CoverMediaAsset.FileUrl,
                $"{x.Year} · Edition {x.EditionNumber}", x.GalleryAlbums.Count)).ToListAsync(token);
        return Ok(new AdminExistingContentListDto(items, page, pageSize, total, published, drafts));
    }

    [HttpGet("gallery")]
    public async Task<ActionResult<AdminExistingContentListDto>> Gallery(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var all = db.GalleryAlbums.AsNoTracking().AsQueryable();
        var published = await all.CountAsync(x => x.IsPublished, token);
        var drafts = await all.CountAsync(x => !x.IsPublished, token);
        var query = all;
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.Translations.Any(t => t.Title.Contains(term))); }
        if (status?.Equals("published", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(x => x.IsPublished);
        if (status?.Equals("draft", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(x => !x.IsPublished);
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.EventDate).ThenByDescending(x => x.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminExistingContentItemDto(x.Id,
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? "Galeri",
                x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Title).FirstOrDefault() ?? "Gallery",
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Slug).FirstOrDefault() ?? "",
                x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Slug).FirstOrDefault() ?? "",
                x.IsPublished ? "Published" : "Draft", x.IsVisibleInGeneralGallery, x.UpdatedAt,
                x.IsPublished ? x.UpdatedAt : null, x.CoverMediaAsset == null ? null : x.CoverMediaAsset.FileUrl,
                x.AlbumType == GalleryAlbumType.Show ? "Play gallery"
                    : x.AlbumType == GalleryAlbumType.NewsArticle ? "News gallery"
                    : x.AlbumType == GalleryAlbumType.PitfEdition ? "PITF gallery" : "General gallery",
                x.GalleryAlbumMedia.Count)).ToListAsync(token);
        return Ok(new AdminExistingContentListDto(items, page, pageSize, total, published, drafts));
    }
}
