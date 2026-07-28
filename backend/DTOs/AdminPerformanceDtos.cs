using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record AdminPerformanceDto(
    int Id, int ShowId, string ShowTitle, int? LocationId, string? Venue, string? Hall,
    DateTimeOffset StartDateTimeUtc, DateTimeOffset? EndDateTimeUtc, string? TicketUrl,
    string? ContactPhone, string Status, bool IsPublished, string? InternalNotes,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record SaveAdminPerformanceRequest(
    [Range(1, int.MaxValue)] int ShowId, int? LocationId, [MaxLength(180)] string? Hall,
    DateTimeOffset StartDateTimeUtc, DateTimeOffset? EndDateTimeUtc, [Url] string? TicketUrl,
    [Phone, MaxLength(80)] string? ContactPhone, [Required] string Status,
    bool IsPublished, [MaxLength(2000)] string? InternalNotes);

public sealed record AdminPerformanceListDto(
    IReadOnlyList<AdminPerformanceDto> Items, int Page, int PageSize, int TotalCount,
    IReadOnlyList<AdminLookupDto> Shows, IReadOnlyList<AdminLookupDto> Locations);
