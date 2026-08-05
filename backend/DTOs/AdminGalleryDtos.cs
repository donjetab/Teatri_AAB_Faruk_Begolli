namespace Theatre.Api.DTOs;

using System.ComponentModel.DataAnnotations;

public sealed record AdminGalleryMediaDto(
    int Id, string FileUrl, string FileName, string? AltText,
    int DisplayOrder, bool IsCover, bool IsFeatured, string? PhotographerCredit);

public sealed record AdminGalleryAlbumDto(
    int Id, string TitleSq, string TitleEn, string AlbumType,
    string? RelatedContent, DateOnly? EventDate, bool IsPublished,
    bool IsVisibleInGeneralGallery, DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminGalleryMediaDto> Media);

public sealed record AdminGalleryListDto(
    IReadOnlyList<AdminGalleryAlbumDto> Items, int Page, int PageSize,
    int TotalCount, int PublishedCount, int DraftCount, int TotalImages);

public sealed record AddGeneralGalleryMediaRequest([Range(1, int.MaxValue)] int MediaAssetId);
public sealed record ReorderGeneralGalleryRequest(IReadOnlyList<int> MediaAssetIds);
public sealed record FeatureGeneralGalleryMediaRequest(bool IsFeatured);
