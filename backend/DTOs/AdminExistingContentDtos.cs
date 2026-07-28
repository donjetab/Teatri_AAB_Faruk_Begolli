namespace Theatre.Api.DTOs;

public sealed record AdminExistingContentItemDto(
    int Id, string TitleSq, string TitleEn, string SlugSq, string SlugEn,
    string Status, bool IsFeatured, DateTimeOffset UpdatedAt, DateTimeOffset? PublishedAt,
    string? ImageUrl, string? SecondaryText, int RelatedCount);

public sealed record AdminExistingContentListDto(
    IReadOnlyList<AdminExistingContentItemDto> Items, int Page, int PageSize,
    int TotalCount, int PublishedCount, int DraftCount);
