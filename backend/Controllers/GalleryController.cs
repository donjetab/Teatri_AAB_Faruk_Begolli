using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.Models;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/{languageCode:regex(^(sq|en)$)}/gallery")]
public sealed class GalleryController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicGalleryImageDto>>> Get(string languageCode, CancellationToken token)
    {
        var images = await db.GalleryAlbums.AsNoTracking()
            .Where(x => x.IsPublished && (
                (x.AlbumType == GalleryAlbumType.General && x.IsVisibleInGeneralGallery) ||
                (x.AlbumType == GalleryAlbumType.Show && x.IsVisibleInGeneralGallery && x.Show != null && x.Show.Status == ShowStatus.Published)))
            .SelectMany(x => x.GalleryAlbumMedia)
            .Where(x => x.GalleryAlbum.Show == null || x.MediaAssetId != x.GalleryAlbum.Show.PosterMediaAssetId)
            .OrderBy(x => x.GalleryAlbum.AlbumType == GalleryAlbumType.General ? 0 : 1)
            .ThenBy(x => x.GalleryAlbum.DisplayOrder)
            .ThenByDescending(x => x.IsFeatured).ThenBy(x => x.DisplayOrder)
            .Select(x => new PublicGalleryImageDto(
                x.MediaAssetId,
                x.MediaAsset.FileUrl,
                x.MediaAsset.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.AltText).FirstOrDefault()))
            .ToListAsync(token);
        return Ok(images);
    }
}

public sealed record PublicGalleryImageDto(int Id, string FileUrl, string? AltText);
