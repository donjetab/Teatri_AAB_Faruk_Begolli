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

public sealed record CreateAdminVenueRequest(
    [Required, MaxLength(180)] string NameSq,
    [Required, MaxLength(180)] string NameEn,
    [Required, MaxLength(300)] string AddressSq,
    [Required, MaxLength(300)] string AddressEn);

public sealed record AdminVenueDto(
    int Id, string NameSq, string NameEn, string AddressSq, string AddressEn,
    bool IsActive, int PerformanceCount);

public sealed record AdminPerformanceListDto(
    IReadOnlyList<AdminPerformanceDto> Items, int Page, int PageSize, int TotalCount,
    IReadOnlyList<AdminLookupDto> Shows, IReadOnlyList<AdminLookupDto> Locations);

public sealed record AdminPerformanceConflictDto(
    int Id, string ShowTitle, DateTimeOffset StartDateTimeUtc, string? Venue, string? Hall);
