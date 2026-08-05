using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Models;

namespace Theatre.Api.Data.Seed;

public static class LegacyGalleryImporter
{
    public static async Task ImportAsync(AppDbContext db, IWebHostEnvironment environment, CancellationToken token = default)
    {
        var configured = environment.ContentRootFileProvider.GetFileInfo("../frontend/src/assets/teatri").PhysicalPath;
        var sourceDirectory = Path.GetFullPath(configured ?? Path.Combine(environment.ContentRootPath, "..", "frontend", "src", "assets", "teatri"));
        if (!Directory.Exists(sourceDirectory)) return;

        var album = await db.GalleryAlbums.Include(x => x.Translations).Include(x => x.GalleryAlbumMedia)
            .FirstOrDefaultAsync(x => x.AlbumType == GalleryAlbumType.General, token);
        if (album is null)
        {
            var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token);
            album = new GalleryAlbum
            {
                AlbumType = GalleryAlbumType.General, IsVisibleInGeneralGallery = true, IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
                Translations = languages.Select(language => new GalleryAlbumTranslation
                {
                    LanguageId = language.Id,
                    Title = language.Code == "sq" ? "Fotot e teatrit" : "Theatre pictures",
                    Slug = language.Code == "sq" ? "fotot-e-teatrit" : "theatre-pictures"
                }).ToList()
            };
            db.GalleryAlbums.Add(album);
        }

        var destination = Path.Combine(environment.WebRootPath, "uploads", "gallery", "theatre");
        Directory.CreateDirectory(destination);
        foreach (var source in Directory.EnumerateFiles(sourceDirectory).Where(IsImage).OrderBy(Path.GetFileName))
        {
            await using var input = File.OpenRead(source);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
            var media = await db.MediaAssets.FirstOrDefaultAsync(x => x.IsActive && x.ContentHash == hash, token);
            if (media is null)
            {
                input.Position = 0;
                var extension = Path.GetExtension(source).ToLowerInvariant();
                var storedName = $"{Guid.NewGuid():N}{extension}";
                await using (var output = File.Create(Path.Combine(destination, storedName))) await input.CopyToAsync(output, token);
                media = new MediaAsset
                {
                    FileName = Path.GetFileName(source), FileUrl = $"/uploads/gallery/theatre/{storedName}",
                    MimeType = Mime(extension), FileSize = input.Length, ContentHash = hash,
                    UploadedAt = DateTimeOffset.UtcNow, IsActive = true
                };
                db.MediaAssets.Add(media);
            }
            if (album.GalleryAlbumMedia.All(x => x.MediaAsset != media && (media.Id == 0 || x.MediaAssetId != media.Id)))
                album.GalleryAlbumMedia.Add(new GalleryAlbumMedia { MediaAsset = media, DisplayOrder = album.GalleryAlbumMedia.Count, IsCover = album.GalleryAlbumMedia.Count == 0 });
        }
        album.IsPublished = true;
        album.IsVisibleInGeneralGallery = true;
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(token);
    }

    private static bool IsImage(string path) => new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    private static string Mime(string extension) => extension switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
}
