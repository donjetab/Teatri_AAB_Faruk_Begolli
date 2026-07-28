using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Models;

namespace Theatre.Api.Data.Seed;

internal static partial class NewsImportSeeder
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".jfif"
    };

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".jfif"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".mp4"] = "video/mp4",
        [".mov"] = "video/quicktime",
        [".webm"] = "video/webm"
    };

    public static async Task SeedAsync(
        AppDbContext db,
        IWebHostEnvironment environment,
        IReadOnlyDictionary<string, Language> languages,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var importRoot = Path.Combine(Directory.GetParent(environment.ContentRootPath)!.FullName, "news-import");
        if (!Directory.Exists(importRoot))
        {
            return;
        }

        var existingArticles = await db.NewsArticles
            .Include(x => x.Translations)
            .Include(x => x.GalleryAlbums)
                .ThenInclude(x => x.Translations)
            .Include(x => x.GalleryAlbums)
                .ThenInclude(x => x.GalleryAlbumMedia)
                    .ThenInclude(x => x.MediaAsset)
            .ToListAsync(cancellationToken);
        var articlesBySlug = existingArticles
            .SelectMany(article => article.Translations.Select(translation => new
            {
                translation.Slug,
                Article = article
            }))
            .ToDictionary(x => x.Slug, x => x.Article, StringComparer.OrdinalIgnoreCase);
        var mediaByUrl = await db.MediaAssets
            .Include(x => x.Translations)
            .ToDictionaryAsync(x => x.FileUrl, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var directory in Directory.EnumerateDirectories(importRoot).OrderBy(x => x))
        {
            var folderName = Path.GetFileName(directory);
            var textPath = Path.Combine(directory, "news.txt");
            if (!File.Exists(textPath))
            {
                continue;
            }

            var fields = ParseFields(await File.ReadAllTextAsync(textPath, cancellationToken));
            var title = GetValue(fields, "TITLE");
            var dateText = GetValue(fields, "DATE");
            if (IsMissing(title) || !DateOnly.TryParseExact(dateText, "yyyy-MM-dd", out var publishedDate))
            {
                continue;
            }

            var slug = folderName;
            var externalUrl = CleanExternalUrl(GetValue(fields, "EXTERNAL LINK"));
            var isNewArticle = !articlesBySlug.TryGetValue(slug, out var article);
            if (isNewArticle)
            {
                article = new NewsArticle { CreatedAt = now };
                db.NewsArticles.Add(article);
                articlesBySlug[slug] = article;
            }

            article!.ArticleType = string.IsNullOrWhiteSpace(externalUrl)
                ? NewsArticleType.Authored
                : NewsArticleType.External;
            article.ExternalUrl = string.IsNullOrWhiteSpace(externalUrl) ? null : externalUrl;
            article.ExternalSourceName = string.IsNullOrWhiteSpace(externalUrl)
                ? null
                : GetExternalSourceName(externalUrl);
            article.IsPublished = true;
            article.PublishedAt = new DateTimeOffset(
                publishedDate.ToDateTime(new TimeOnly(12, 0)),
                TimeSpan.Zero);
            article.UpdatedAt = now;

            var summary = GetValue(fields, "SUMMARY");
            var body = GetValue(fields, "BODY");
            var articleTranslation = article.Translations
                .FirstOrDefault(x => x.LanguageId == languages["sq"].Id);
            if (articleTranslation is null)
            {
                articleTranslation = new NewsArticleTranslation { LanguageId = languages["sq"].Id };
                article.Translations.Add(articleTranslation);
            }
            articleTranslation.Title = title;
            articleTranslation.Slug = slug;
            articleTranslation.Summary = IsMissing(summary) ? title : summary;
            articleTranslation.Content = IsMissing(body) ? string.Empty : body;
            articleTranslation.MetaTitle = title;
            articleTranslation.MetaDescription = IsMissing(summary) ? title : Truncate(summary, 320);

            var importedMedia = new List<MediaAsset>();
            foreach (var sourcePath in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                         .Where(path => !path.EndsWith("news.txt", StringComparison.OrdinalIgnoreCase))
                         .Where(path => MimeTypes.ContainsKey(Path.GetExtension(path)))
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(sourcePath);
                var relativeMediaPath = Path.GetRelativePath(directory, sourcePath);
                var relativeUrl = string.Join(
                    '/',
                    relativeMediaPath
                        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Select(Uri.EscapeDataString));
                var fileUrl = $"/uploads/dev/news/{folderName}/{relativeUrl}";
                var destinationPath = Path.Combine(
                    environment.WebRootPath,
                    "uploads",
                    "dev",
                    "news",
                    folderName,
                    relativeMediaPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath, overwrite: true);

                if (!mediaByUrl.TryGetValue(fileUrl, out var asset))
                {
                    asset = new MediaAsset
                    {
                        FileUrl = fileUrl,
                        UploadedAt = now
                    };
                    db.MediaAssets.Add(asset);
                    mediaByUrl[fileUrl] = asset;
                }

                asset.FileName = fileName;
                asset.MimeType = MimeTypes[Path.GetExtension(sourcePath)];
                asset.FileSize = new FileInfo(sourcePath).Length;
                var dimensions = GetImageDimensions(sourcePath);
                asset.Width = dimensions?.Width;
                asset.Height = dimensions?.Height;
                asset.IsActive = true;
                EnsureMediaTranslation(asset, languages["sq"].Id, title);
                importedMedia.Add(asset);
            }

            var coverField = GetValue(fields, "COVER");
            article.CoverMediaAsset = ResolveCover(importedMedia, coverField);

            if (importedMedia.Count > 0)
            {
                var album = article.GalleryAlbums
                    .FirstOrDefault(x => x.AlbumType == GalleryAlbumType.NewsArticle);
                if (album is null)
                {
                    album = new GalleryAlbum
                    {
                        AlbumType = GalleryAlbumType.NewsArticle,
                        CreatedAt = now
                    };
                    article.GalleryAlbums.Add(album);
                }
                album.EventDate = publishedDate;
                album.CoverMediaAsset = article.CoverMediaAsset;
                album.IsVisibleInGeneralGallery = true;
                album.IsPublished = true;
                album.UpdatedAt = now;

                var albumTranslation = album.Translations
                    .FirstOrDefault(x => x.LanguageId == languages["sq"].Id);
                if (albumTranslation is null)
                {
                    albumTranslation = new GalleryAlbumTranslation { LanguageId = languages["sq"].Id };
                    album.Translations.Add(albumTranslation);
                }
                albumTranslation.Title = title;
                albumTranslation.Description = IsMissing(summary) ? null : summary;
                albumTranslation.Slug = $"{slug}-media";

                for (var index = 0; index < importedMedia.Count; index++)
                {
                    var importedAsset = importedMedia[index];
                    var albumMedia = album.GalleryAlbumMedia
                        .FirstOrDefault(x => x.MediaAsset.FileUrl.Equals(
                            importedAsset.FileUrl,
                            StringComparison.OrdinalIgnoreCase));
                    if (albumMedia is null)
                    {
                        albumMedia = new GalleryAlbumMedia { MediaAsset = importedAsset };
                        album.GalleryAlbumMedia.Add(albumMedia);
                    }
                    albumMedia.DisplayOrder = index;
                    albumMedia.IsCover = importedAsset == article.CoverMediaAsset;
                    albumMedia.IsFeatured = index == 0;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, string> ParseFields(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matches = FieldRegex().Matches(content);
        for (var index = 0; index < matches.Count; index++)
        {
            var valueStart = matches[index].Index + matches[index].Length;
            var valueEnd = index + 1 < matches.Count ? matches[index + 1].Index : content.Length;
            result[matches[index].Groups["name"].Value] = content[valueStart..valueEnd].Trim();
        }
        return result;
    }

    private static string GetValue(IReadOnlyDictionary<string, string> fields, string name) =>
        fields.TryGetValue(name, out var value) ? value.Trim() : string.Empty;

    private static bool IsMissing(string value) =>
        string.IsNullOrWhiteSpace(value) || value == "//";

    private static string? CleanExternalUrl(string value)
    {
        if (IsMissing(value))
        {
            return null;
        }

        var cleaned = value.Trim().Trim('[', ']');
        return Uri.TryCreate(cleaned, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
                ? cleaned
                : null;
    }

    private static string? GetExternalSourceName(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase)
            : null;

    private static MediaAsset? ResolveCover(IEnumerable<MediaAsset> media, string coverField)
    {
        var images = media.Where(x => ImageExtensions.Contains(Path.GetExtension(x.FileName))).ToList();
        if (images.Count == 0)
        {
            return null;
        }

        if (!IsMissing(coverField) && !coverField.StartsWith('['))
        {
            var requested = Path.GetFileName(coverField);
            var exact = images.FirstOrDefault(x =>
                x.FileName.Equals(requested, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }
        }

        return images
                   .Where(x => x.Width.HasValue && x.Height.HasValue && x.Height > x.Width)
                   .OrderByDescending(x =>
                       Path.GetFileNameWithoutExtension(x.FileName)
                           .Contains("poster", StringComparison.OrdinalIgnoreCase))
                   .ThenByDescending(x => (double)x.Height!.Value / x.Width!.Value)
                   .FirstOrDefault()
               ?? images.FirstOrDefault(x =>
                   Path.GetFileNameWithoutExtension(x.FileName)
                       .Contains("cover", StringComparison.OrdinalIgnoreCase)
                   || Path.GetFileNameWithoutExtension(x.FileName)
                       .Contains("poster", StringComparison.OrdinalIgnoreCase))
               ?? images[0];
    }

    private static (int Width, int Height)? GetImageDimensions(string path)
    {
        var extension = Path.GetExtension(path);
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) && stream.Length >= 24)
        {
            stream.Position = 16;
            return (ReadBigEndianInt32(reader), ReadBigEndianInt32(reader));
        }

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jfif", StringComparison.OrdinalIgnoreCase))
        {
            if (reader.ReadByte() != 0xff || reader.ReadByte() != 0xd8)
            {
                return null;
            }

            while (stream.Position + 4 < stream.Length)
            {
                if (reader.ReadByte() != 0xff)
                {
                    continue;
                }

                var marker = reader.ReadByte();
                while (marker == 0xff)
                {
                    marker = reader.ReadByte();
                }
                if (marker is 0xd8 or 0xd9)
                {
                    continue;
                }

                var segmentLength = ReadBigEndianUInt16(reader);
                if (segmentLength < 2 || stream.Position + segmentLength - 2 > stream.Length)
                {
                    return null;
                }

                if (marker is >= 0xc0 and <= 0xc3
                    or >= 0xc5 and <= 0xc7
                    or >= 0xc9 and <= 0xcb
                    or >= 0xcd and <= 0xcf)
                {
                    reader.ReadByte();
                    var height = ReadBigEndianUInt16(reader);
                    var width = ReadBigEndianUInt16(reader);
                    return (width, height);
                }

                stream.Position += segmentLength - 2;
            }
        }

        return null;
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return bytes.Length == 4
            ? (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]
            : 0;
    }

    private static ushort ReadBigEndianUInt16(BinaryReader reader)
    {
        var high = reader.ReadByte();
        var low = reader.ReadByte();
        return (ushort)((high << 8) | low);
    }

    private static void EnsureMediaTranslation(MediaAsset asset, int languageId, string altText)
    {
        var translation = asset.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new MediaAssetTranslation { LanguageId = languageId };
            asset.Translations.Add(translation);
        }
        translation.AltText = altText;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    [GeneratedRegex(@"(?m)^(?<name>[A-Z][A-Z ]+):\s*\r?\n")]
    private static partial Regex FieldRegex();
}
