namespace Theatre.Api.DTOs;

public sealed record ShowListItemDto(
    int Id,
    string Title,
    string Slug,
    string Synopsis,
    string? PosterUrl,
    string? Director,
    DateOnly? PremiereDate,
    DateTimeOffset? NextPerformanceDateUtc,
    string? ReservationUrl);

public sealed record ShowDetailDto(
    int Id,
    string Title,
    string Slug,
    string Synopsis,
    string? PosterUrl,
    DateOnly? PremiereDate,
    IReadOnlyList<ShowCreditDto> Credits,
    DateTimeOffset? NextPerformanceDateUtc,
    string? ReservationUrl);

public sealed record ShowCreditDto(
    string TypeCode,
    string TypeName,
    IReadOnlyList<string> People);
