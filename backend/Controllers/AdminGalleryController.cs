using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/gallery")]
public sealed class AdminGalleryController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet("general")]
    public async Task<ActionResult<IReadOnlyList<AdminGalleryMediaDto>>> GetGeneral(CancellationToken token)
    {
        var media = await db.GalleryAlbumMedia.AsNoTracking()
            .Where(x => x.GalleryAlbum.AlbumType == GalleryAlbumType.General)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new AdminGalleryMediaDto(
                x.MediaAssetId, x.MediaAsset.FileUrl, x.MediaAsset.FileName,
                x.MediaAsset.Translations.Where(t => t.Language.Code == "sq").Select(t => t.AltText).FirstOrDefault(),
                x.DisplayOrder, x.IsCover, x.IsFeatured, x.MediaAsset.PhotographerCredit))
            .ToListAsync(token);
        return Ok(media);
    }

    [HttpGet]
    public async Task<ActionResult<AdminGalleryListDto>> Get(
        [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken token = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var all = db.GalleryAlbums.AsNoTracking();
        var published = await all.CountAsync(x => x.IsPublished, token);
        var drafts = await all.CountAsync(x => !x.IsPublished, token);
        var totalImages = await db.GalleryAlbumMedia.CountAsync(token);

        var query = all.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Translations.Any(t => t.Title.Contains(term)));
        }
        if (status?.Equals("published", StringComparison.OrdinalIgnoreCase) == true)
            query = query.Where(x => x.IsPublished);
        if (status?.Equals("draft", StringComparison.OrdinalIgnoreCase) == true)
            query = query.Where(x => !x.IsPublished);

        var total = await query.CountAsync(token);
        var albums = await query
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.GalleryAlbumMedia).ThenInclude(x => x.MediaAsset).ThenInclude(x => x.Translations)
            .Include(x => x.Show).ThenInclude(x => x!.Translations).ThenInclude(x => x.Language)
            .Include(x => x.NewsArticle).ThenInclude(x => x!.Translations).ThenInclude(x => x.Language)
            .Include(x => x.PitfEdition).ThenInclude(x => x!.Translations).ThenInclude(x => x.Language)
            .OrderByDescending(x => x.EventDate).ThenByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(token);

        var items = albums.Select(album =>
        {
            var related = album.Show?.Translations.FirstOrDefault(x => x.Language.Code == "sq")?.Title
                ?? album.NewsArticle?.Translations.FirstOrDefault(x => x.Language.Code == "sq")?.Title
                ?? album.PitfEdition?.Translations.FirstOrDefault(x => x.Language.Code == "sq")?.Title;
            return new AdminGalleryAlbumDto(
                album.Id,
                album.Translations.FirstOrDefault(x => x.Language.Code == "sq")?.Title ?? "Galeri",
                album.Translations.FirstOrDefault(x => x.Language.Code == "en")?.Title ?? "Gallery",
                album.AlbumType.ToString(),
                related,
                album.EventDate,
                album.IsPublished,
                album.IsVisibleInGeneralGallery,
                album.UpdatedAt,
                album.GalleryAlbumMedia.OrderBy(x => x.DisplayOrder).Select(media =>
                    new AdminGalleryMediaDto(
                        media.MediaAssetId,
                        media.MediaAsset.FileUrl,
                        media.MediaAsset.FileName,
                        media.MediaAsset.Translations.FirstOrDefault()?.AltText,
                        media.DisplayOrder,
                        media.IsCover,
                        media.IsFeatured,
                        media.MediaAsset.PhotographerCredit)).ToList());
        }).ToList();

        return Ok(new AdminGalleryListDto(items, page, pageSize, total, published, drafts, totalImages));
    }

    [HttpPost("general/media")]
    public async Task<IActionResult> AddGeneralMedia(AddGeneralGalleryMediaRequest request, CancellationToken token)
    {
        var media = await db.MediaAssets.FirstOrDefaultAsync(x => x.Id == request.MediaAssetId && x.IsActive && x.MimeType.StartsWith("image/"), token);
        if (media is null) return ValidationProblem("Choose an active image.");
        var album = await db.GalleryAlbums.Include(x => x.Translations).Include(x => x.GalleryAlbumMedia)
            .FirstOrDefaultAsync(x => x.AlbumType == GalleryAlbumType.General, token);
        if (album is null)
        {
            var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token);
            album = new GalleryAlbum
            {
                AlbumType = GalleryAlbumType.General,
                IsVisibleInGeneralGallery = true,
                IsPublished = true,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow,
                Translations = languages.Select(language => new GalleryAlbumTranslation
                {
                    LanguageId = language.Id,
                    Title = language.Code == "sq" ? "Fotot e teatrit" : "Theatre pictures",
                    Slug = language.Code == "sq" ? "fotot-e-teatrit" : "theatre-pictures"
                }).ToList()
            };
            db.GalleryAlbums.Add(album);
        }
        if (album.GalleryAlbumMedia.Any(x => x.MediaAssetId == media.Id))
            return Conflict(new ProblemDetails { Title = "Picture already added", Detail = "This picture is already in the public gallery.", Status = 409 });
        album.GalleryAlbumMedia.Add(new GalleryAlbumMedia
        {
            MediaAssetId = media.Id,
            DisplayOrder = album.GalleryAlbumMedia.Count,
            IsCover = album.GalleryAlbumMedia.Count == 0,
            IsFeatured = false
        });
        album.CoverMediaAssetId ??= media.Id;
        album.IsVisibleInGeneralGallery = true;
        album.IsPublished = true;
        album.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpDelete("general/media/{mediaId:int}")]
    public async Task<IActionResult> RemoveGeneralMedia(int mediaId, CancellationToken token)
    {
        var album = await db.GalleryAlbums.Include(x => x.GalleryAlbumMedia)
            .FirstOrDefaultAsync(x => x.AlbumType == GalleryAlbumType.General && x.GalleryAlbumMedia.Any(m => m.MediaAssetId == mediaId), token);
        if (album is null) return NotFound();
        var item = album.GalleryAlbumMedia.First(x => x.MediaAssetId == mediaId);
        db.GalleryAlbumMedia.Remove(item);
        var remaining = album.GalleryAlbumMedia.Where(x => x.MediaAssetId != mediaId).OrderBy(x => x.DisplayOrder).ToList();
        for (var index = 0; index < remaining.Count; index++) remaining[index].DisplayOrder = index;
        if (album.CoverMediaAssetId == mediaId) album.CoverMediaAssetId = remaining.FirstOrDefault()?.MediaAssetId;
        album.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpPut("general/order")]
    public async Task<IActionResult> ReorderGeneralMedia(ReorderGeneralGalleryRequest request, CancellationToken token)
    {
        var album = await db.GalleryAlbums.Include(x => x.GalleryAlbumMedia)
            .FirstOrDefaultAsync(x => x.AlbumType == GalleryAlbumType.General, token);
        if (album is null) return NotFound();
        var currentIds = album.GalleryAlbumMedia.Select(x => x.MediaAssetId).Order().ToArray();
        if (!currentIds.SequenceEqual(request.MediaAssetIds.Distinct().Order()))
            return ValidationProblem("The order must contain every general-gallery picture exactly once.");
        for (var index = 0; index < request.MediaAssetIds.Count; index++)
            album.GalleryAlbumMedia.First(x => x.MediaAssetId == request.MediaAssetIds[index]).DisplayOrder = index;
        album.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpPut("general/media/{mediaId:int}/featured")]
    public async Task<IActionResult> FeatureGeneralMedia(int mediaId, FeatureGeneralGalleryMediaRequest request, CancellationToken token)
    {
        var item = await db.GalleryAlbumMedia.Include(x => x.GalleryAlbum)
            .FirstOrDefaultAsync(x => x.GalleryAlbum.AlbumType == GalleryAlbumType.General && x.MediaAssetId == mediaId, token);
        if (item is null) return NotFound();
        item.IsFeatured = request.IsFeatured;
        item.GalleryAlbum.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token);
        return NoContent();
    }
}
