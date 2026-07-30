namespace Theatre.Api.DTOs;

public sealed record NewsListItemDto(
    int Id,
    string Title,
    string Slug,
    string Summary,
    DateTimeOffset PublishedAt,
    string? CoverUrl,
    string? CoverMimeType,
    string? CardThumbnailUrl,
    bool IsExternal,
    string? ExternalUrl,
    bool IsFallbackTranslation);

public sealed record NewsMediaDto(
    int Id,
    string Url,
    string MimeType,
    string AltText,
    string? Caption,
    int DisplayOrder,
    bool IsCover);

public sealed record NewsDetailDto(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string Content,
    DateTimeOffset PublishedAt,
    string? CoverUrl,
    string? CoverMimeType,
    bool IsExternal,
    string? ExternalUrl,
    string? ExternalSourceName,
    bool IsFallbackTranslation,
    IReadOnlyList<NewsMediaDto> Media,
    IReadOnlyList<NewsExternalLinkDto> RelatedLinks);

public sealed record NewsExternalLinkDto(
    int Id, string Title, string Url, string? SourceName, DateTimeOffset? PublishedAt, int DisplayOrder);
