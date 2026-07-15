namespace Theatre.Api.DTOs;

public sealed record HomeResponseDto(
    string LanguageCode,
    string TheatreName,
    bool IsFallbackTranslation,
    string HeroSlogan,
    MediaAssetDto? HeroBackground,
    string AboutPreview,
    MediaAssetDto? AboutImage,
    TheatreStatisticsDto Statistics,
    IReadOnlyList<UpcomingShowDto> UpcomingShows,
    PitfFeaturedDto? PitfFeatured,
    MediaAssetDto? ReservationBanner,
    string ReservationTitle,
    string ReservationText,
    string? ReservationUrl,
    string Address,
    string Phone,
    string Email,
    string? FacebookUrl,
    string? InstagramUrl);

public sealed record MediaAssetDto(
    int Id,
    string Url,
    string AltText,
    string? Caption,
    int? Width,
    int? Height);

public sealed record TheatreStatisticsDto(
    int FoundedYear,
    int PerformancesCount,
    int SpectatorsCount);

public sealed record UpcomingShowDto(
    int Id,
    string Title,
    string Slug,
    bool IsFallbackTranslation,
    string? PosterUrl,
    string? Director,
    DateTimeOffset NearestPerformanceDateUtc,
    string PerformanceStatus,
    string? TicketUrl);

public sealed record PitfFeaturedDto(
    int Id,
    int EditionNumber,
    int Year,
    string Title,
    string Slug,
    bool IsFallbackTranslation,
    string ShortDescription,
    MediaAssetDto? Image);
