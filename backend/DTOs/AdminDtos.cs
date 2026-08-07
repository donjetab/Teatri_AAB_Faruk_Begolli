using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record AdminSessionDto(int Id, string Email, string DisplayName, string Role);
public sealed record AdminLoginRequest([Required, EmailAddress] string Email, [Required] string Password, bool RememberMe);
public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(12)] string NewPassword);
public sealed record DashboardMetricDto(string Key, int Value, string Label);
public sealed record DashboardItemDto(int Id, string Title, string? Subtitle, DateTimeOffset? Date, string Status);
public sealed record AdminDashboardDto(
    IReadOnlyList<DashboardMetricDto> Metrics,
    IReadOnlyList<DashboardItemDto> UpcomingPerformances,
    IReadOnlyList<DashboardItemDto> RecentlyEditedPlays,
    IReadOnlyList<DashboardItemDto> RecentNews,
    IReadOnlyList<DashboardItemDto> RecentMessages,
    IReadOnlyList<DashboardItemDto> RecentActivity);

public sealed record LocalizedWebsiteInformationDto(
    string LanguageCode, string TheatreName, string AddressDisplayText, string FooterCopyrightText);

public sealed record WebsiteInformationDto(
    int Id, string Address, string Phone, string Email, string? FacebookUrl, string? InstagramUrl,
    string? FacebookDisplayName, string? InstagramDisplayName, string? ReservationUrl,
    int? LogoMediaAssetId, string? LogoUrl,
    int? FooterLogoMediaAssetId, string? FooterLogoUrl,
    int? FaviconMediaAssetId, string? FaviconUrl,
    int? SocialSharingMediaAssetId, string? SocialSharingImageUrl,
    IReadOnlyList<LocalizedWebsiteInformationDto> Translations, DateTimeOffset UpdatedAt);

public sealed record UpdateWebsiteInformationRequest(
    [Required, MaxLength(300)] string Address,
    [Required, Phone, MaxLength(80)] string Phone,
    [Required, EmailAddress, MaxLength(180)] string Email,
    [Url] string? FacebookUrl, [Url] string? InstagramUrl, string? FacebookDisplayName, string? InstagramDisplayName, string? ReservationUrl,
    int? LogoMediaAssetId, int? FooterLogoMediaAssetId, int? FaviconMediaAssetId, int? SocialSharingMediaAssetId,
    [MinLength(2)] IReadOnlyList<LocalizedWebsiteInformationDto> Translations);

public sealed record LocalizedHomepageDto(
    string LanguageCode, string HeroSlogan, string HeroSupportingText, string HeroButtonText,
    string AboutTitle, string AboutShort, string AboutButtonText, string ReservationTitle,
    string ReservationText, string ReservationButtonText, string PitfTitle, string PitfDescription, string PitfButtonText);

public sealed record AdminHomepageDto(
    int Id, int? HeroMediaAssetId, int? AboutMediaAssetId, int? ReservationMediaAssetId, int? PitfMediaAssetId,
    bool HeroIsVisible, bool ReservationBannerIsVisible, bool PitfFeatureIsVisible, int LatestNewsCount,
    string? PrimaryButtonLink, string? AboutButtonLink, string? ReservationUrl, string? PitfDestinationUrl,
    IReadOnlyList<LocalizedHomepageDto> Translations, DateTimeOffset UpdatedAt);

public sealed record TranslationIssueDto(string ContentType, int ContentId, string Title, string MissingLanguage, string Status, DateTimeOffset UpdatedAt);
public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
