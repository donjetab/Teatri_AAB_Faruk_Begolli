using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record AdminShowTranslationDto(
    [Required, RegularExpression("^(sq|en)$")] string LanguageCode,
    [Required, MaxLength(240)] string Title,
    [Required, MaxLength(240), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] string Slug,
    [Required, MaxLength(700)] string ShortDescription,
    [Required] string FullDescription,
    [MaxLength(220)] string? MetaTitle,
    [MaxLength(320)] string? MetaDescription);

public sealed record AdminShowListItemDto(
    int Id, string TitleSq, string TitleEn, string SlugSq, string Category, string Status, string LifecycleStatus,
    int? ProductionYear, DateOnly? PremiereDate, bool IsFeatured, int PerformanceCount,
    DateTimeOffset UpdatedAt, string? PosterUrl);

public sealed record AdminShowDetailDto(
    int Id, int ShowCategoryId, int? PosterMediaAssetId, int? FeaturedMediaAssetId,
    int? DurationMinutes, int? ProductionYear, int? AgeRecommendation, string? OriginalLanguage,
    string? TrailerUrl, string? VideoUrl, DateOnly? PremiereDate, string Status,
    string LifecycleStatus, bool IsFeatured, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt, IReadOnlyList<AdminShowTranslationDto> Translations);

public sealed record SaveAdminShowRequest(
    [Range(1, int.MaxValue)] int ShowCategoryId,
    int? PosterMediaAssetId,
    int? FeaturedMediaAssetId,
    [Range(1, 600)] int? DurationMinutes,
    [Range(1900, 2200)] int? ProductionYear,
    [Range(0, 21)] int? AgeRecommendation,
    [MaxLength(80)] string? OriginalLanguage,
    [Url] string? TrailerUrl,
    [Url] string? VideoUrl,
    DateOnly? PremiereDate,
    [Required] string LifecycleStatus,
    bool IsFeatured,
    [MinLength(2)] IReadOnlyList<AdminShowTranslationDto> Translations);

public sealed record AdminShowListResponseDto(
    IReadOnlyList<AdminShowListItemDto> Items, int Page, int PageSize, int TotalCount,
    IReadOnlyList<AdminLookupDto> Categories, IReadOnlyList<int> Years);

public sealed record AdminLookupDto(int Id, string Label);

public sealed record AdminShowCreditDto(
    int? Id, int? PersonId, [Required, MaxLength(180)] string PersonName,
    [Range(1, int.MaxValue)] int CreditTypeId, string CreditTypeCode,
    string RoleSq, string RoleEn, [MaxLength(180)] string? CharacterName, int DisplayOrder);

public sealed record SaveAdminShowCreditsRequest(IReadOnlyList<AdminShowCreditDto> Credits);

public sealed record AdminShowCreditsResponseDto(
    IReadOnlyList<AdminShowCreditDto> Credits, IReadOnlyList<AdminCreditTypeDto> CreditTypes);

public sealed record AdminCreditTypeDto(int Id, string Code, string LabelSq, string LabelEn);
