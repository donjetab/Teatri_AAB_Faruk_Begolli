using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record AdminNewsTranslationDto(string LanguageCode, string Title, string Slug, string Summary,
    string Content, string? MetaTitle, string? MetaDescription);
public sealed record AdminNewsDetailDto(int Id, string ArticleType, int? CoverMediaAssetId, string? CoverUrl,
    string? ExternalUrl, string? ExternalSourceName, bool IsPublished, bool IsFeatured,
    DateTimeOffset? PublishedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminNewsTranslationDto> Translations);
public sealed record SaveAdminNewsRequest([Required] string ArticleType, int? CoverMediaAssetId,
    [Url] string? ExternalUrl, [MaxLength(200)] string? ExternalSourceName, bool IsPublished,
    bool IsFeatured, DateTimeOffset? PublishedAt, [MinLength(2)] IReadOnlyList<AdminNewsTranslationDto> Translations);
