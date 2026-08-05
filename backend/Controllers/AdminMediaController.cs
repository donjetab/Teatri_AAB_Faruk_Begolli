using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;
using System.Security.Cryptography;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/media")]
[RequestSizeLimit(52_428_800)]
public sealed class AdminMediaController(AppDbContext db, IWebHostEnvironment environment, IClock clock) : ControllerBase
{
    private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"], ["image/png"] = [".png"], ["image/webp"] = [".webp"],
        ["image/svg+xml"] = [".svg"], ["video/mp4"] = [".mp4"], ["application/pdf"] = [".pdf"],
        ["application/msword"] = [".doc"], ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [".docx"]
    };

    [HttpGet]
    public async Task<ActionResult<AdminMediaListDto>> List([FromQuery] string? search, [FromQuery] string? type, [FromQuery] bool? unused, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.MediaAssets.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.FileName.Contains(search.Trim()));
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.MimeType.StartsWith(type));
        if (unused == true) query = query.Where(x =>
            !db.Shows.Any(s => s.PosterMediaAssetId == x.Id || s.FeaturedMediaAssetId == x.Id)
            && !db.People.Any(p => p.ProfileMediaAssetId == x.Id)
            && !db.GalleryAlbumMedia.Any(g => g.MediaAssetId == x.Id)
            && !db.GalleryAlbums.Any(g => g.CoverMediaAssetId == x.Id)
            && !db.NewsArticles.Any(n => n.CoverMediaAssetId == x.Id || n.CardThumbnailMediaAssetId == x.Id)
            && !db.PitfEditions.Any(p => p.CoverMediaAssetId == x.Id || p.LogoMediaAssetId == x.Id)
            && !db.TheatreInformation.Any(t => t.HeroBackgroundMediaAssetId == x.Id || t.AboutPreviewMediaAssetId == x.Id
                || t.ReservationBannerMediaAssetId == x.Id || t.PitfFeatureMediaAssetId == x.Id || t.PitfPageMediaAssetId == x.Id
                || t.LogoMediaAssetId == x.Id || t.FaviconMediaAssetId == x.Id || t.SocialSharingMediaAssetId == x.Id));
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.UploadedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AdminMediaDto(
            x.Id, x.FileUrl, x.FileName, x.MimeType, x.Width, x.Height, x.FileSize, x.IsActive, x.UploadedAt,
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.AltText).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.AltText).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Caption).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Caption).FirstOrDefault(), x.PhotographerCredit,
            db.Shows.Count(s => s.PosterMediaAssetId == x.Id || s.FeaturedMediaAssetId == x.Id)
            + db.People.Count(p => p.ProfileMediaAssetId == x.Id)
            + db.GalleryAlbumMedia.Count(g => g.MediaAssetId == x.Id)
            + db.GalleryAlbums.Count(g => g.CoverMediaAssetId == x.Id)
            + db.NewsArticles.Count(n => n.CoverMediaAssetId == x.Id || n.CardThumbnailMediaAssetId == x.Id)
            + db.PitfEditions.Count(p => p.CoverMediaAssetId == x.Id || p.LogoMediaAssetId == x.Id)
            + db.TheatreInformation.Count(t => t.HeroBackgroundMediaAssetId == x.Id || t.AboutPreviewMediaAssetId == x.Id
                || t.ReservationBannerMediaAssetId == x.Id || t.PitfFeatureMediaAssetId == x.Id || t.PitfPageMediaAssetId == x.Id
                || t.LogoMediaAssetId == x.Id || t.FaviconMediaAssetId == x.Id || t.SocialSharingMediaAssetId == x.Id))).ToListAsync(token);
        return Ok(new AdminMediaListDto(items, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminMediaDto>> Get(int id, CancellationToken token)
    {
        var item = await db.MediaAssets.AsNoTracking().Where(x => x.Id == id && x.IsActive).Select(x => new AdminMediaDto(
            x.Id, x.FileUrl, x.FileName, x.MimeType, x.Width, x.Height, x.FileSize, x.IsActive, x.UploadedAt,
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.AltText).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.AltText).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Caption).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Caption).FirstOrDefault(), x.PhotographerCredit,
            db.Shows.Count(s => s.PosterMediaAssetId == x.Id || s.FeaturedMediaAssetId == x.Id)
            + db.People.Count(p => p.ProfileMediaAssetId == x.Id)
            + db.GalleryAlbumMedia.Count(g => g.MediaAssetId == x.Id)
            + db.GalleryAlbums.Count(g => g.CoverMediaAssetId == x.Id)
            + db.NewsArticles.Count(n => n.CoverMediaAssetId == x.Id || n.CardThumbnailMediaAssetId == x.Id)
            + db.PitfEditions.Count(p => p.CoverMediaAssetId == x.Id || p.LogoMediaAssetId == x.Id)
            + db.TheatreInformation.Count(t => t.HeroBackgroundMediaAssetId == x.Id || t.AboutPreviewMediaAssetId == x.Id
                || t.ReservationBannerMediaAssetId == x.Id || t.PitfFeatureMediaAssetId == x.Id || t.PitfPageMediaAssetId == x.Id
                || t.LogoMediaAssetId == x.Id || t.FaviconMediaAssetId == x.Id || t.SocialSharingMediaAssetId == x.Id))).FirstOrDefaultAsync(token);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:int}/usage")]
    public async Task<ActionResult<IReadOnlyList<AdminMediaUsageDto>>> Usage(int id, CancellationToken token)
    {
        if (!await db.MediaAssets.AnyAsync(x => x.Id == id && x.IsActive, token)) return NotFound();
        var result = new List<AdminMediaUsageDto>();
        var shows = await db.Shows.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language)
            .Where(x => x.PosterMediaAssetId == id || x.FeaturedMediaAssetId == id).ToListAsync(token);
        result.AddRange(shows.Select(x => new AdminMediaUsageDto("Play", x.Id,
            x.Translations.FirstOrDefault(t => t.Language.Code == "sq")?.Title ?? $"Play #{x.Id}",
            x.PosterMediaAssetId == id && x.FeaturedMediaAssetId == id ? "Poster and featured image" : x.PosterMediaAssetId == id ? "Poster" : "Featured image", $"/admin/shows/{x.Id}")));
        var news = await db.NewsArticles.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language)
            .Where(x => x.CoverMediaAssetId == id || x.CardThumbnailMediaAssetId == id).ToListAsync(token);
        result.AddRange(news.Select(x => new AdminMediaUsageDto("News", x.Id,
            x.Translations.FirstOrDefault(t => t.Language.Code == "sq")?.Title ?? $"News #{x.Id}",
            x.CoverMediaAssetId == id && x.CardThumbnailMediaAssetId == id ? "Main media and card thumbnail" : x.CoverMediaAssetId == id ? "Main media" : "Card thumbnail", $"/admin/news/{x.Id}")));
        var galleries = await db.GalleryAlbumMedia.AsNoTracking().Include(x => x.GalleryAlbum).ThenInclude(x => x.Translations).ThenInclude(x => x.Language)
            .Where(x => x.MediaAssetId == id).ToListAsync(token);
        result.AddRange(galleries.Select(x => new AdminMediaUsageDto("Gallery", x.GalleryAlbumId,
            x.GalleryAlbum.Translations.FirstOrDefault(t => t.Language.Code == "sq")?.Title ?? $"Gallery #{x.GalleryAlbumId}",
            x.GalleryAlbum.AlbumType == GalleryAlbumType.General ? "Public theatre gallery" : "Content gallery",
            x.GalleryAlbum.AlbumType == GalleryAlbumType.Show && x.GalleryAlbum.ShowId.HasValue ? $"/admin/shows/{x.GalleryAlbum.ShowId}" : x.GalleryAlbum.AlbumType == GalleryAlbumType.NewsArticle && x.GalleryAlbum.NewsArticleId.HasValue ? $"/admin/news/{x.GalleryAlbum.NewsArticleId}" : "/admin/gallery")));
        var editions = await db.PitfEditions.AsNoTracking().Where(x => x.LogoMediaAssetId == id || x.CoverMediaAssetId == id).ToListAsync(token);
        result.AddRange(editions.Select(x => new AdminMediaUsageDto("PITF", x.Id, $"PITF {x.Year}", x.LogoMediaAssetId == id ? "Edition logo" : "Edition cover", "/admin/pitf")));
        var people = await db.People.AsNoTracking().Where(x => x.ProfileMediaAssetId == id).ToListAsync(token);
        result.AddRange(people.Select(x => new AdminMediaUsageDto("Person", x.Id, x.FullName, "Profile image", null)));
        var info = await db.TheatreInformation.AsNoTracking().FirstOrDefaultAsync(token);
        if (info is not null)
        {
            var roles = new List<string>();
            if (info.HeroBackgroundMediaAssetId == id) roles.Add("Homepage hero");
            if (info.AboutPreviewMediaAssetId == id) roles.Add("Homepage about");
            if (info.ReservationBannerMediaAssetId == id) roles.Add("Reservation banner");
            if (info.PitfFeatureMediaAssetId == id) roles.Add("Homepage PITF feature");
            if (info.PitfPageMediaAssetId == id) roles.Add("PITF page");
            if (info.LogoMediaAssetId == id) roles.Add("Website logo");
            if (info.FaviconMediaAssetId == id) roles.Add("Favicon");
            if (info.SocialSharingMediaAssetId == id) roles.Add("Social sharing image");
            if (roles.Count > 0) result.Add(new AdminMediaUsageDto("Website", info.Id, "Website information", string.Join(", ", roles), "/admin/website-information"));
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminMediaDto>> Upload(IFormFile file, CancellationToken token)
    {
        if (file.Length == 0 || file.Length > 50 * 1024 * 1024) return ValidationProblem("File must be between 1 byte and 50 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.TryGetValue(file.ContentType, out var extensions) || !extensions.Contains(extension))
            return ValidationProblem("The file type, extension, or MIME type is not allowed.");
        await using var input = file.OpenReadStream();
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
        var duplicate = await db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(x => x.IsActive && x.ContentHash == hash, token);
        if (duplicate is not null)
            return Conflict(new ProblemDetails { Title = "File already uploaded", Detail = $"This exact file already exists as media #{duplicate.Id} ({duplicate.FileName}). Reuse that item instead.", Status = 409 });
        input.Position = 0;
        var folder = Path.Combine(environment.WebRootPath, "uploads", "admin", clock.UtcNow.ToString("yyyy-MM"));
        Directory.CreateDirectory(folder);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, storedName);
        await using (var stream = System.IO.File.Create(path)) await input.CopyToAsync(stream, token);
        var asset = new MediaAsset { FileName = Path.GetFileName(file.FileName), FileUrl = $"/uploads/admin/{clock.UtcNow:yyyy-MM}/{storedName}",
            MimeType = file.ContentType, FileSize = file.Length, ContentHash = hash, UploadedAt = clock.UtcNow, IsActive = true };
        db.MediaAssets.Add(asset); await db.SaveChangesAsync(token);
        return CreatedAtAction(nameof(List), new { search = asset.FileName }, new AdminMediaDto(asset.Id, asset.FileUrl, asset.FileName, asset.MimeType, null, null, asset.FileSize, true, asset.UploadedAt, null, null, null, null, null, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminMediaDto>> Update(int id, UpdateAdminMediaRequest request, CancellationToken token)
    {
        var asset = await db.MediaAssets.Include(x => x.Translations).ThenInclude(x => x.Language).FirstOrDefaultAsync(x => x.Id == id, token);
        if (asset is null) return NotFound();
        asset.FileName = Path.GetFileName(request.FileName.Trim());
        asset.PhotographerCredit = Clean(request.PhotographerCredit);
        var languages = await db.Languages.ToDictionaryAsync(x => x.Code, token);
        SetTranslation(asset, languages["sq"], request.AltTextSq, request.CaptionSq);
        SetTranslation(asset, languages["en"], request.AltTextEn, request.CaptionEn);
        await db.SaveChangesAsync(token);
        return Ok();
    }

    [HttpPut("{id:int}/file")]
    public async Task<ActionResult<AdminMediaDto>> ReplaceFile(int id, IFormFile file, CancellationToken token)
    {
        var asset = await db.MediaAssets.Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == id && x.IsActive, token);
        if (asset is null) return NotFound();
        if (file.Length == 0 || file.Length > 50 * 1024 * 1024) return ValidationProblem("File must be between 1 byte and 50 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.TryGetValue(file.ContentType, out var extensions) || !extensions.Contains(extension))
            return ValidationProblem("The file type, extension, or MIME type is not allowed.");
        await using var input = file.OpenReadStream();
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
        if (await db.MediaAssets.AnyAsync(x => x.Id != id && x.IsActive && x.ContentHash == hash, token))
            return Conflict(new ProblemDetails { Title = "File already uploaded", Detail = "Reuse the existing Media Library item instead of replacing this asset with a duplicate.", Status = 409 });
        input.Position = 0;
        var folder = Path.Combine(environment.WebRootPath, "uploads", "admin", clock.UtcNow.ToString("yyyy-MM"));
        Directory.CreateDirectory(folder);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, storedName);
        await using (var output = System.IO.File.Create(path)) await input.CopyToAsync(output, token);
        asset.FileUrl = $"/uploads/admin/{clock.UtcNow:yyyy-MM}/{storedName}";
        asset.FileName = Path.GetFileName(file.FileName);
        asset.MimeType = file.ContentType;
        asset.FileSize = file.Length;
        asset.ContentHash = hash;
        asset.Width = null;
        asset.Height = null;
        await db.SaveChangesAsync(token);
        return Ok(new AdminMediaDto(asset.Id, asset.FileUrl, asset.FileName, asset.MimeType, asset.Width, asset.Height,
            asset.FileSize, asset.IsActive, asset.UploadedAt,
            asset.Translations.FirstOrDefault(x => x.LanguageId == 1)?.AltText,
            asset.Translations.FirstOrDefault(x => x.LanguageId == 2)?.AltText,
            asset.Translations.FirstOrDefault(x => x.LanguageId == 1)?.Caption,
            asset.Translations.FirstOrDefault(x => x.LanguageId == 2)?.Caption,
            asset.PhotographerCredit, 0));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var asset = await db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id, token);
        if (asset is null) return NotFound();
        var used = await db.Shows.AnyAsync(x => x.PosterMediaAssetId == id || x.FeaturedMediaAssetId == id, token)
            || await db.People.AnyAsync(x => x.ProfileMediaAssetId == id, token)
            || await db.NewsArticles.AnyAsync(x => x.CoverMediaAssetId == id || x.CardThumbnailMediaAssetId == id, token)
            || await db.PitfEditions.AnyAsync(x => x.CoverMediaAssetId == id || x.LogoMediaAssetId == id, token)
            || await db.GalleryAlbums.AnyAsync(x => x.CoverMediaAssetId == id, token)
            || await db.GalleryAlbumMedia.AnyAsync(x => x.MediaAssetId == id, token)
            || await db.TheatreInformation.AnyAsync(x => x.HeroBackgroundMediaAssetId == id
                || x.AboutPreviewMediaAssetId == id || x.ReservationBannerMediaAssetId == id
                || x.PitfFeatureMediaAssetId == id || x.PitfPageMediaAssetId == id
                || x.LogoMediaAssetId == id || x.FaviconMediaAssetId == id || x.SocialSharingMediaAssetId == id, token);
        if (used) return Conflict(new ProblemDetails { Title = "Media is in use", Detail = "Remove or replace all content references before deleting this asset.", Status = 409 });
        asset.IsActive = false;
        await db.SaveChangesAsync(token);
        DeleteManagedFile(asset.FileUrl);
        return NoContent();
    }

    private static void SetTranslation(MediaAsset asset, Language language, string? alt, string? caption)
    {
        var item = asset.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
        if (item is null) { item = new MediaAssetTranslation { LanguageId = language.Id, Language = language }; asset.Translations.Add(item); }
        item.AltText = Clean(alt); item.Caption = Clean(caption);
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void DeleteManagedFile(string fileUrl)
    {
        if (!fileUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return;
        var webRoot = Path.GetFullPath(environment.WebRootPath);
        var relative = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.GetFullPath(Path.Combine(webRoot, relative));
        if (physicalPath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(physicalPath))
            System.IO.File.Delete(physicalPath);
    }
}
