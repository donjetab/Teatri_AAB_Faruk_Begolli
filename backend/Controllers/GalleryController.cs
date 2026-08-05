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
            .Where(x => x.AlbumType == GalleryAlbumType.General && x.IsPublished && x.IsVisibleInGeneralGallery)
            .SelectMany(x => x.GalleryAlbumMedia)
            .OrderByDescending(x => x.IsFeatured).ThenBy(x => x.DisplayOrder)
            .Select(x => new PublicGalleryImageDto(
                x.MediaAssetId,
                x.MediaAsset.FileUrl,
                x.MediaAsset.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.AltText).FirstOrDefault()))
            .ToListAsync(token);
        return Ok(images);
    }
}

public sealed record PublicGalleryImageDto(int Id, string FileUrl, string? AltText);
