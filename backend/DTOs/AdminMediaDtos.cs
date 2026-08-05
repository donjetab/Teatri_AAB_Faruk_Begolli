namespace Theatre.Api.DTOs;

public sealed record AdminMediaDto(int Id, string FileUrl, string FileName, string MimeType, int? Width, int? Height,
    long? FileSize, bool IsActive, DateTimeOffset UploadedAt, string? AltTextSq, string? AltTextEn,
    string? CaptionSq, string? CaptionEn, string? PhotographerCredit, int UsageCount);
public sealed record AdminMediaListDto(IReadOnlyList<AdminMediaDto> Items, int Page, int PageSize, int TotalCount);
public sealed record UpdateAdminMediaRequest(string FileName, string? AltTextSq, string? AltTextEn, string? CaptionSq, string? CaptionEn, string? PhotographerCredit);
public sealed record AdminMediaUsageDto(string ContentType, int? ContentId, string Title, string Role, string? AdminPath);
