using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Theatre.Api.Data.Seed;

public static class ContentSlugNormalizer
{
    public static async Task NormalizeAsync(AppDbContext db, CancellationToken token = default)
    {
        var shows = await db.ShowTranslations.OrderBy(x => x.LanguageId).ThenBy(x => x.ShowId).ToListAsync(token);
        var news = await db.NewsArticleTranslations.OrderBy(x => x.LanguageId).ThenBy(x => x.NewsArticleId).ToListAsync(token);

        // Move away from existing values first so translated slugs can safely trade places.
        foreach (var item in shows) item.Slug = $"pending-show-{item.Id}";
        foreach (var item in news) item.Slug = $"pending-news-{item.Id}";
        await db.SaveChangesAsync(token);

        Assign(shows, x => x.LanguageId, x => x.Title, (x, slug) => x.Slug = slug);
        Assign(news, x => x.LanguageId, x => x.Title, (x, slug) => x.Slug = slug);
        await db.SaveChangesAsync(token);
    }

    public static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(normalized.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark).ToArray());
        var slug = Regex.Replace(withoutMarks, "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slug)) slug = "content";
        return slug.Length <= 220 ? slug : slug[..220].TrimEnd('-');
    }

    private static void Assign<T>(IEnumerable<T> items, Func<T, int> language, Func<T, string> title, Action<T, string> setSlug)
    {
        foreach (var group in items.GroupBy(language))
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in group)
            {
                var baseSlug = Slugify(title(item));
                var slug = baseSlug;
                for (var suffix = 2; !used.Add(slug); suffix++)
                {
                    var ending = $"-{suffix}";
                    slug = $"{baseSlug[..Math.Min(baseSlug.Length, 220 - ending.Length)].TrimEnd('-')}{ending}";
                }
                setSlug(item, slug);
            }
        }
    }
}
