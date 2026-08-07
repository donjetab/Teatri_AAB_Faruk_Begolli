using Theatre.Api.Models;

namespace Theatre.Api.Services;

public sealed record ReservationRuleResult(bool IsAvailable, string? Message);

public static class ReservationRules
{
    public static ReservationRuleResult Evaluate(ShowPerformance performance, DateTimeOffset now, int requestedSeats = 0)
    {
        if (!performance.ReservationsEnabled) return new(false, Message(performance, "Reservations are currently paused."));
        if (performance.ReservationOpensAtUtc is { } opens && now < opens) return new(false, Message(performance, $"Reservations open on {opens:u}."));
        if (performance.ReservationClosesAtUtc is { } closes && now > closes) return new(false, Message(performance, "Reservations are closed."));
        if (requestedSeats > 0 && performance.MaxSeatsPerReservation is { } max && requestedSeats > max)
            return new(false, $"A maximum of {max} seats is allowed per reservation.");
        return new(true, null);
    }

    public static void EnsureAvailable(ShowPerformance performance, DateTimeOffset now, int requestedSeats)
    {
        var result = Evaluate(performance, now, requestedSeats);
        if (!result.IsAvailable) throw new ValidationException(result.Message!);
    }

    private static string Message(ShowPerformance performance, string fallback) =>
        string.IsNullOrWhiteSpace(performance.ReservationUnavailableMessage) ? fallback : performance.ReservationUnavailableMessage.Trim();
}
