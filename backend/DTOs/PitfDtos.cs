using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record PitfPageDto(
    string Title, string Description, string ButtonText, string? ButtonUrl,
    string? ImageUrl, IReadOnlyList<PitfEditionDto> Editions);

public sealed record PitfEditionDto(
    int Id, int EditionNumber, int Year, string Name, string? ImageUrl,
    string? DestinationUrl);

public sealed record AdminPitfTranslationDto(
    [Required] string LanguageCode, [Required, MaxLength(220)] string Title,
    [Required, MaxLength(700)] string Description, [Required, MaxLength(120)] string ButtonText);

public sealed record AdminPitfEditionDto(
    int? Id, [Range(1, int.MaxValue)] int EditionNumber, [Range(2000, 2200)] int Year,
    int? CoverMediaAssetId, string? CoverUrl, string? DestinationUrl,
    bool IsPublished, IReadOnlyList<AdminPitfEditionTranslationDto> Translations);

public sealed record AdminPitfEditionTranslationDto(
    [Required] string LanguageCode, [Required, MaxLength(220)] string Name);

public sealed record AdminPitfPageDto(
    int? ImageMediaAssetId, string? ImageUrl, string? ButtonUrl,
    IReadOnlyList<AdminPitfTranslationDto> Translations,
    IReadOnlyList<AdminPitfEditionDto> Editions);

public sealed record SaveAdminPitfPageRequest(
    int? ImageMediaAssetId, string? ButtonUrl,
    IReadOnlyList<AdminPitfTranslationDto> Translations);

public sealed record SaveAdminPitfEditionRequest(
    [Range(1, int.MaxValue)] int EditionNumber, [Range(2000, 2200)] int Year,
    int? CoverMediaAssetId, string? DestinationUrl,
    bool IsPublished, IReadOnlyList<AdminPitfEditionTranslationDto> Translations);
