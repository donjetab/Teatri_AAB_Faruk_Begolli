namespace Theatre.Api.DTOs;

public sealed record HomeResponseDto(
    string LanguageCode,
    string TheatreName,
    bool IsFallbackTranslation,
    string HeroSlogan,
    string HeroSupportingText,
    string HeroButtonText,
    string PrimaryButtonLink,
    MediaAssetDto? HeroBackground,
    string AboutTitle,
    string AboutPreview,
    string AboutButtonText,
    string AboutButtonLink,
    MediaAssetDto? AboutImage,
    TheatreStatisticsDto Statistics,
    IReadOnlyList<UpcomingShowDto> UpcomingShows,
    PitfFeaturedDto? PitfFeatured,
    MediaAssetDto? ReservationBanner,
    string ReservationTitle,
    string ReservationText,
    string ReservationButtonText,
    string? ReservationUrl,
    string PitfFeatureTitle,
    string PitfFeatureDescription,
    string PitfFeatureButtonText,
    string PitfDestinationUrl,
    MediaAssetDto? PitfFeatureImage,
    bool HeroIsVisible,
    bool ReservationBannerIsVisible,
    bool PitfFeatureIsVisible,
    string Address,
    string Phone,
    string Email,
    string? FacebookUrl,
    string? InstagramUrl,
    string? FacebookDisplayName,
    string? InstagramDisplayName,
    string? LogoUrl,
    string? FooterLogoUrl,
    string FooterCopyrightText);

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
    int PerformanceId,
    string Title,
    string Slug,
    bool IsFallbackTranslation,
    string? PosterUrl,
    string? Director,
    DateTimeOffset NearestPerformanceDateUtc,
    string PerformanceStatus,
    string? TicketUrl,
    string ReservationMode,
    string? InternalReservationUrl);

public sealed record PitfFeaturedDto(
    int Id,
    int EditionNumber,
    int Year,
    string Title,
    string Slug,
    bool IsFallbackTranslation,
    string ShortDescription,
    MediaAssetDto? Image);
