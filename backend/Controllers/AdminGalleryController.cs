using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/gallery")]
public sealed class AdminGalleryController(AppDbContext db) : ControllerBase
{
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
                        media.IsCover)).ToList());
        }).ToList();

        return Ok(new AdminGalleryListDto(items, page, pageSize, total, published, drafts, totalImages));
    }
}
