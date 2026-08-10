namespace Theatre.Api.DTOs;

public sealed record PublicPerformanceDto(
    int Id, int ShowId, string ShowTitle, string ShowSlug, string? PosterUrl,
    DateTimeOffset StartDateTimeUtc, string? Venue, string? VenueAddress, string? Hall, string Status,
    string? TicketUrl, string? ContactPhone, string ReservationMode, string? InternalReservationUrl,
    bool IsGuestPerformance);
