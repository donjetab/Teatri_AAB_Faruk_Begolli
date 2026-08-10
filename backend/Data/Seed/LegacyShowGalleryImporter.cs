using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Models;

namespace Theatre.Api.Data.Seed;

public static class LegacyShowGalleryImporter
{
    public static async Task ImportAsync(AppDbContext db, IWebHostEnvironment environment, CancellationToken token = default)
    {
        var shows = await db.Shows
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.Translations)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.GalleryAlbumMedia)
            .OrderBy(x => x.Id)
            .ToListAsync(token);
        if (shows.Count == 0) return;

        var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token);
        var nextOrder = (await db.GalleryAlbums
            .Where(x => x.AlbumType == GalleryAlbumType.Show)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync(token) ?? -1) + 1;

        foreach (var show in shows)
        {
            var sq = show.Translations.FirstOrDefault(x => x.Language.Code == "sq") ?? show.Translations.FirstOrDefault();
            var en = show.Translations.FirstOrDefault(x => x.Language.Code == "en") ?? sq;
            if (sq is null) continue;

            var album = show.GalleryAlbums.FirstOrDefault(x => x.AlbumType == GalleryAlbumType.Show);
            if (album is null)
            {
                album = new GalleryAlbum
                {
                    AlbumType = GalleryAlbumType.Show,
                    ShowId = show.Id,
                    IsPublished = true,
                    IsVisibleInGeneralGallery = true,
                    DisplayOrder = nextOrder++,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Translations = languages.Select(language => new GalleryAlbumTranslation
                    {
                        LanguageId = language.Id,
                        Title = $"{(language.Code == "en" ? en?.Title : sq.Title)} Gallery",
                        Slug = $"show-{show.Id}-gallery-{language.Code}"
                    }).ToList()
                };
                show.GalleryAlbums.Add(album);
            }

            album.IsPublished = true;
            var posterItems = album.GalleryAlbumMedia
                .Where(x => show.PosterMediaAssetId.HasValue && x.MediaAssetId == show.PosterMediaAssetId.Value)
                .ToList();
            if (posterItems.Count > 0)
                db.GalleryAlbumMedia.RemoveRange(posterItems);

            var sourceDirectory = Path.Combine(environment.WebRootPath, "uploads", "shows", sq.Slug);
            if (!Directory.Exists(sourceDirectory)) continue;

            foreach (var source in Directory.EnumerateFiles(sourceDirectory).Where(IsImage).OrderBy(Path.GetFileName))
            {
                await using var input = File.OpenRead(source);
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
                var media = await db.MediaAssets.FirstOrDefaultAsync(x => x.IsActive && x.ContentHash == hash, token);
                if (media is null)
                {
                    var extension = Path.GetExtension(source).ToLowerInvariant();
                    media = new MediaAsset
                    {
                        FileName = Path.GetFileName(source),
                        FileUrl = $"/uploads/shows/{sq.Slug}/{Path.GetFileName(source)}",
                        MimeType = Mime(extension),
                        FileSize = input.Length,
                        ContentHash = hash,
                        UploadedAt = DateTimeOffset.UtcNow,
                        IsActive = true
                    };
                    db.MediaAssets.Add(media);
                }
                if (show.PosterMediaAssetId.HasValue && media.Id == show.PosterMediaAssetId.Value)
                    continue;
                if (album.GalleryAlbumMedia.All(x => x.MediaAsset != media && (media.Id == 0 || x.MediaAssetId != media.Id)))
                    album.GalleryAlbumMedia.Add(new GalleryAlbumMedia
                    {
                        MediaAsset = media,
                        DisplayOrder = album.GalleryAlbumMedia.Count,
                        IsCover = album.GalleryAlbumMedia.Count == 0,
                        IsFeatured = album.GalleryAlbumMedia.Count == 0
                    });
            }
            album.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(token);
    }

    private static bool IsImage(string path) => new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    private static string Mime(string extension) => extension switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
}
