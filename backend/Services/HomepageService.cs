using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;

namespace Theatre.Api.Services;

public interface IHomepageService
{
    Task<HomeResponseDto?> GetHomeAsync(string languageCode, CancellationToken cancellationToken);
}

public sealed class HomepageService(AppDbContext db, IClock clock) : IHomepageService
{
    private const string FallbackLanguageCode = "sq";

    public async Task<HomeResponseDto?> GetHomeAsync(string languageCode, CancellationToken cancellationToken)
    {
        var languageIds = await GetLanguageIdsAsync(languageCode, cancellationToken);
        if (languageIds.RequestedLanguageId is null)
        {
            return null;
        }

        var home = await GetTheatreInformationAsync(languageIds.RequestedLanguageId.Value, languageIds.FallbackLanguageId, cancellationToken);
        if (home is null)
        {
            return null;
        }

        var upcomingShows = await GetUpcomingShowsAsync(languageIds.RequestedLanguageId.Value, languageIds.FallbackLanguageId, cancellationToken);
        var pitfFeatured = await GetPitfFeaturedAsync(languageIds.RequestedLanguageId.Value, languageIds.FallbackLanguageId, cancellationToken);

        return home with
        {
            UpcomingShows = upcomingShows,
            PitfFeatured = pitfFeatured
        };
    }

    private async Task<(int? RequestedLanguageId, int FallbackLanguageId)> GetLanguageIdsAsync(string languageCode, CancellationToken cancellationToken)
    {
        var languages = await db.Languages
            .Where(x => x.IsActive && (x.Code == languageCode || x.Code == FallbackLanguageCode))
            .Select(x => new { x.Id, x.Code })
            .ToListAsync(cancellationToken);

        var fallbackLanguageId = languages.FirstOrDefault(x => x.Code == FallbackLanguageCode)?.Id;
        if (fallbackLanguageId is null)
        {
            throw new InvalidOperationException("Fallback language 'sq' is not configured.");
        }

        var requestedLanguageId = languages.FirstOrDefault(x => x.Code == languageCode)?.Id;
        return (requestedLanguageId, fallbackLanguageId.Value);
    }

    private async Task<HomeResponseDto?> GetTheatreInformationAsync(int languageId, int fallbackLanguageId, CancellationToken cancellationToken)
    {
        var query = await db.TheatreInformation
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.FoundedYear,
                x.PerformancesCount,
                x.SpectatorsCount,
                x.ReservationUrl,
                x.Address,
                x.Phone,
                x.Email,
                x.FacebookUrl,
                x.InstagramUrl,
                Translation = x.Translations
                    .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                    .OrderByDescending(t => t.LanguageId == languageId)
                    .Select(t => new
                    {
                        t.LanguageId,
                        t.TheatreName,
                        t.HeroSlogan,
                        t.AboutShort,
                        t.PitfShortDescription,
                        t.ReservationCallToActionTitle,
                        t.ReservationCallToActionText,
                        t.AddressDisplayText
                    })
                    .FirstOrDefault(),
                Hero = x.HeroBackgroundMediaAsset == null ? null : new MediaProjection(
                    x.HeroBackgroundMediaAsset.Id,
                    x.HeroBackgroundMediaAsset.FileUrl,
                    x.HeroBackgroundMediaAsset.Width,
                    x.HeroBackgroundMediaAsset.Height,
                    x.HeroBackgroundMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.AltText)
                        .FirstOrDefault() ?? string.Empty,
                    x.HeroBackgroundMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.Caption)
                        .FirstOrDefault()),
                About = x.AboutPreviewMediaAsset == null ? null : new MediaProjection(
                    x.AboutPreviewMediaAsset.Id,
                    x.AboutPreviewMediaAsset.FileUrl,
                    x.AboutPreviewMediaAsset.Width,
                    x.AboutPreviewMediaAsset.Height,
                    x.AboutPreviewMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.AltText)
                        .FirstOrDefault() ?? string.Empty,
                    x.AboutPreviewMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.Caption)
                        .FirstOrDefault()),
                Reservation = x.ReservationBannerMediaAsset == null ? null : new MediaProjection(
                    x.ReservationBannerMediaAsset.Id,
                    x.ReservationBannerMediaAsset.FileUrl,
                    x.ReservationBannerMediaAsset.Width,
                    x.ReservationBannerMediaAsset.Height,
                    x.ReservationBannerMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.AltText)
                        .FirstOrDefault() ?? string.Empty,
                    x.ReservationBannerMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.Caption)
                        .FirstOrDefault())
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (query?.Translation is null)
        {
            return null;
        }

        return new HomeResponseDto(
            languageId == fallbackLanguageId ? FallbackLanguageCode : "en",
            query.Translation.TheatreName,
            query.Translation.LanguageId != languageId,
            query.Translation.HeroSlogan,
            ToDto(query.Hero),
            query.Translation.AboutShort,
            ToDto(query.About),
            new TheatreStatisticsDto(query.FoundedYear, query.PerformancesCount, query.SpectatorsCount),
            [],
            null,
            ToDto(query.Reservation),
            query.Translation.ReservationCallToActionTitle,
            query.Translation.ReservationCallToActionText,
            query.ReservationUrl,
            string.IsNullOrWhiteSpace(query.Translation.AddressDisplayText) ? query.Address : query.Translation.AddressDisplayText,
            query.Phone,
            query.Email,
            query.FacebookUrl,
            query.InstagramUrl);
    }

    private async Task<IReadOnlyList<UpcomingShowDto>> GetUpcomingShowsAsync(int languageId, int fallbackLanguageId, CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow.ToUniversalTime();

        var upcomingPerformances = await db.ShowPerformances
            .Where(x => x.Show.Status == ShowStatus.Published)
            .Where(x => x.Status != PerformanceStatus.Cancelled && x.Status != PerformanceStatus.Completed)
            .Where(x => x.StartDateTimeUtc > nowUtc)
            .OrderBy(x => x.StartDateTimeUtc)
            .Select(x => new
            {
                ShowId = x.Show.Id,
                x.StartDateTimeUtc,
                x.Status,
                x.TicketUrl,
                PosterUrl = x.Show.PosterMediaAsset == null ? null : x.Show.PosterMediaAsset.FileUrl,
                Translation = x.Show.Translations
                    .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                    .OrderByDescending(t => t.LanguageId == languageId)
                    .Select(t => new { t.LanguageId, t.Title, t.Slug })
                    .FirstOrDefault(),
                Director = x.Show.Credits
                    .Where(c => c.CreditType.Code == "director")
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => c.Person.FullName)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return upcomingPerformances
            .Where(x => x.Translation is not null)
            .GroupBy(x => x.ShowId)
            .Select(x => x.First())
            .Take(3)
            .Select(x => new UpcomingShowDto(
                x.ShowId,
                x.Translation!.Title,
                x.Translation.Slug,
                x.Translation.LanguageId != languageId,
                x.PosterUrl,
                x.Director,
                x.StartDateTimeUtc.ToUniversalTime(),
                x.Status.ToString(),
                x.TicketUrl))
            .ToList();
    }

    private async Task<PitfFeaturedDto?> GetPitfFeaturedAsync(int languageId, int fallbackLanguageId, CancellationToken cancellationToken)
    {
        var pitf = await db.PitfEditions
            .Where(x => x.IsPublished && x.IsFeatured)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.EditionNumber)
            .Select(x => new
            {
                x.Id,
                x.EditionNumber,
                x.Year,
                Translation = x.Translations
                    .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                    .OrderByDescending(t => t.LanguageId == languageId)
                    .Select(t => new { t.LanguageId, t.Title, t.Slug, t.ShortDescription })
                    .FirstOrDefault(),
                Image = x.CoverMediaAsset == null ? null : new MediaProjection(
                    x.CoverMediaAsset.Id,
                    x.CoverMediaAsset.FileUrl,
                    x.CoverMediaAsset.Width,
                    x.CoverMediaAsset.Height,
                    x.CoverMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.AltText)
                        .FirstOrDefault() ?? string.Empty,
                    x.CoverMediaAsset.Translations
                        .Where(t => t.LanguageId == languageId || t.LanguageId == fallbackLanguageId)
                        .OrderByDescending(t => t.LanguageId == languageId)
                        .Select(t => t.Caption)
                        .FirstOrDefault())
            })
            .FirstOrDefaultAsync(cancellationToken);

        return pitf?.Translation is null
            ? null
            : new PitfFeaturedDto(
                pitf.Id,
                pitf.EditionNumber,
                pitf.Year,
                pitf.Translation.Title,
                pitf.Translation.Slug,
                pitf.Translation.LanguageId != languageId,
                pitf.Translation.ShortDescription,
                ToDto(pitf.Image));
    }

    private static MediaAssetDto? ToDto(MediaProjection? media)
    {
        return media is null
            ? null
            : new MediaAssetDto(media.Id, media.Url, media.AltText, media.Caption, media.Width, media.Height);
    }

    private sealed record MediaProjection(int Id, string Url, int? Width, int? Height, string AltText, string? Caption);
}
