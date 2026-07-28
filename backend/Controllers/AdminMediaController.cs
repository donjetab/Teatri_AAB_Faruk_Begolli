using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

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
    public async Task<ActionResult<AdminMediaListDto>> List([FromQuery] string? search, [FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.MediaAssets.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.FileName.Contains(search.Trim()));
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.MimeType.StartsWith(type));
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.UploadedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AdminMediaDto(
            x.Id, x.FileUrl, x.FileName, x.MimeType, x.Width, x.Height, x.FileSize, x.IsActive, x.UploadedAt,
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.AltText).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.AltText).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Caption).FirstOrDefault(),
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Caption).FirstOrDefault(),
            db.Shows.Count(s => s.PosterMediaAssetId == x.Id || s.FeaturedMediaAssetId == x.Id)
            + db.GalleryAlbumMedia.Count(g => g.MediaAssetId == x.Id)
            + db.NewsArticles.Count(n => n.CoverMediaAssetId == x.Id)
            + db.PitfEditions.Count(p => p.CoverMediaAssetId == x.Id || p.LogoMediaAssetId == x.Id))).ToListAsync(token);
        return Ok(new AdminMediaListDto(items, page, pageSize, total));
    }

    [HttpPost]
    public async Task<ActionResult<AdminMediaDto>> Upload(IFormFile file, CancellationToken token)
    {
        if (file.Length == 0 || file.Length > 50 * 1024 * 1024) return ValidationProblem("File must be between 1 byte and 50 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.TryGetValue(file.ContentType, out var extensions) || !extensions.Contains(extension))
            return ValidationProblem("The file type, extension, or MIME type is not allowed.");
        var folder = Path.Combine(environment.WebRootPath, "uploads", "admin", clock.UtcNow.ToString("yyyy-MM"));
        Directory.CreateDirectory(folder);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, storedName);
        await using (var stream = System.IO.File.Create(path)) await file.CopyToAsync(stream, token);
        var asset = new MediaAsset { FileName = Path.GetFileName(file.FileName), FileUrl = $"/uploads/admin/{clock.UtcNow:yyyy-MM}/{storedName}",
            MimeType = file.ContentType, FileSize = file.Length, UploadedAt = clock.UtcNow, IsActive = true };
        db.MediaAssets.Add(asset); await db.SaveChangesAsync(token);
        return CreatedAtAction(nameof(List), new { search = asset.FileName }, new AdminMediaDto(asset.Id, asset.FileUrl, asset.FileName, asset.MimeType, null, null, asset.FileSize, true, asset.UploadedAt, null, null, null, null, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminMediaDto>> Update(int id, UpdateAdminMediaRequest request, CancellationToken token)
    {
        var asset = await db.MediaAssets.Include(x => x.Translations).ThenInclude(x => x.Language).FirstOrDefaultAsync(x => x.Id == id, token);
        if (asset is null) return NotFound();
        asset.FileName = Path.GetFileName(request.FileName.Trim());
        var languages = await db.Languages.ToDictionaryAsync(x => x.Code, token);
        SetTranslation(asset, languages["sq"], request.AltTextSq, request.CaptionSq);
        SetTranslation(asset, languages["en"], request.AltTextEn, request.CaptionEn);
        await db.SaveChangesAsync(token);
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var asset = await db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id, token);
        if (asset is null) return NotFound();
        var used = await db.Shows.AnyAsync(x => x.PosterMediaAssetId == id || x.FeaturedMediaAssetId == id, token)
            || await db.NewsArticles.AnyAsync(x => x.CoverMediaAssetId == id, token)
            || await db.PitfEditions.AnyAsync(x => x.CoverMediaAssetId == id || x.LogoMediaAssetId == id, token)
            || await db.GalleryAlbumMedia.AnyAsync(x => x.MediaAssetId == id, token);
        if (used) return Conflict(new ProblemDetails { Title = "Media is in use", Detail = "Remove or replace all content references before deleting this asset.", Status = 409 });
        asset.IsActive = false; await db.SaveChangesAsync(token); return NoContent();
    }

    private static void SetTranslation(MediaAsset asset, Language language, string? alt, string? caption)
    {
        var item = asset.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
        if (item is null) { item = new MediaAssetTranslation { LanguageId = language.Id, Language = language }; asset.Translations.Add(item); }
        item.AltText = Clean(alt); item.Caption = Clean(caption);
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
