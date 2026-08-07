using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.Models;

namespace Theatre.Api.Services;

public interface IPhoneNumberNormalizer { string Normalize(string phone, string? countryPrefix = null); }
public sealed partial class PhoneNumberNormalizer : IPhoneNumberNormalizer
{
    [GeneratedRegex("[^0-9+]")] private static partial Regex InvalidCharacters();
    public string Normalize(string phone, string? countryPrefix = null)
    {
        var value = InvalidCharacters().Replace(phone.Trim(), "");
        if (value.StartsWith("00")) value = "+" + value[2..];
        if (!value.StartsWith('+'))
        {
            var prefix = string.IsNullOrWhiteSpace(countryPrefix) ? "+383" : countryPrefix.Trim();
            if (!prefix.StartsWith('+')) prefix = "+" + prefix;
            value = prefix + value.TrimStart('0');
        }
        if (value.Count(c => c == '+') != 1 || value[0] != '+' || value.Length is < 8 or > 16 || value[1..].Any(c => !char.IsDigit(c)))
            throw new ValidationException("Enter a valid international phone number.");
        return value;
    }
}

public interface IReservationService
{
    Task CloneTemplateAsync(ShowPerformance performance, int templateId, CancellationToken token);
    Task<Customer> FindOrCreateCustomerAsync(string fullName, string phone, string? countryPrefix, string? email, CancellationToken token);
    Task<Reservation> CreateAsync(int performanceId, Customer customer, IReadOnlyCollection<int> seatIds, ReservationSource source, string? comment, CancellationToken token);
    Task ReplaceSeatsAsync(Reservation reservation, IReadOnlyCollection<int> seatIds, int? adminUserId, CancellationToken token);
    Task SetStatusAsync(Reservation reservation, ReservationStatus status, int? adminUserId, CancellationToken token);
}

public sealed class ReservationService(AppDbContext db, IPhoneNumberNormalizer phones, IClock clock) : IReservationService
{
    public async Task CloneTemplateAsync(ShowPerformance performance, int templateId, CancellationToken token)
    {
        var template = await db.SeatingTemplates.AsNoTracking().Where(x => x.Id == templateId && x.IsActive)
            .Include(x => x.Sections).ThenInclude(x => x.Rows).ThenInclude(x => x.Seats).SingleOrDefaultAsync(token)
            ?? throw new ValidationException("The selected seating template does not exist.");
        var layout = new PerformanceSeatingLayout { Performance = performance, SourceTemplateId = template.Id, CanvasWidth = template.CanvasWidth, CanvasHeight = template.CanvasHeight, StageLabel = template.StageLabel, StageX = template.StageX, StageY = template.StageY, StageWidth = template.StageWidth, StageHeight = template.StageHeight, CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow };
        foreach (var section in template.Sections)
        foreach (var row in section.Rows)
        foreach (var seat in row.Seats)
            layout.Seats.Add(new PerformanceSeat { SectionName = section.Name, RowLabel = row.Label, SeatLabel = seat.Label, SectionOrder = section.DisplayOrder, RowOrder = row.DisplayOrder, SeatOrder = seat.DisplayOrder, PositionX = seat.PositionX, PositionY = seat.PositionY, Rotation = seat.Rotation, IsActive = seat.IsActive });
        db.PerformanceSeatingLayouts.Add(layout);
    }

    public async Task<Customer> FindOrCreateCustomerAsync(string fullName, string phone, string? countryPrefix, string? email, CancellationToken token)
    {
        var normalized = phones.Normalize(phone, countryPrefix);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.NormalizedPhone == normalized, token);
        if (customer is not null) return customer;
        customer = new Customer { FullName = fullName.Trim(), NormalizedPhone = normalized, Email = CleanEmail(email), CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow };
        db.Customers.Add(customer);
        return customer;
    }

    public async Task<Reservation> CreateAsync(int performanceId, Customer customer, IReadOnlyCollection<int> seatIds, ReservationSource source, string? comment, CancellationToken token)
    {
        var ids = seatIds.Distinct().ToArray();
        if (ids.Length == 0) throw new ValidationException("Select at least one seat.");
        if (source == ReservationSource.PublicWebsite)
        {
            var performance = await db.ShowPerformances.SingleOrDefaultAsync(x => x.Id == performanceId, token)
                ?? throw new ValidationException("The performance does not exist.");
            ReservationRules.EnsureAvailable(performance, clock.UtcNow, ids.Length);
        }
        var validCount = await db.PerformanceSeats.CountAsync(x => ids.Contains(x.Id) && x.IsActive && x.Layout.PerformanceId == performanceId, token);
        if (validCount != ids.Length) throw new ValidationException("One or more selected seats are invalid for this performance.");
        var reservation = new Reservation { PerformanceId = performanceId, Customer = customer, ReservedAt = clock.UtcNow, UpdatedAt = clock.UtcNow, Source = source, Status = ReservationStatus.Active, ConfirmationStatus = ConfirmationStatus.Unconfirmed, AdminComment = Clean(comment) };
        foreach (var id in ids) reservation.SeatAllocations.Add(new SeatAllocation { PerformanceSeatId = id, Type = SeatAllocationType.Reservation, IsActive = true, CreatedAt = clock.UtcNow });
        reservation.AuditEvents.Add(new ReservationAuditEvent { EventType = "Created", Details = $"Reserved {ids.Length} seat(s)", CreatedAt = clock.UtcNow });
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(token);
        return reservation;
    }

    public async Task ReplaceSeatsAsync(Reservation reservation, IReadOnlyCollection<int> seatIds, int? adminUserId, CancellationToken token)
    {
        var ids = seatIds.Distinct().ToArray();
        if (ids.Length == 0) throw new ValidationException("Select at least one seat.");
        if (await db.PerformanceSeats.CountAsync(x => ids.Contains(x.Id) && x.IsActive && x.Layout.PerformanceId == reservation.PerformanceId, token) != ids.Length) throw new ValidationException("One or more seats are invalid.");
        var current = await db.SeatAllocations.Where(x => x.ReservationId == reservation.Id && x.IsActive).ToListAsync(token);
        foreach (var allocation in current.Where(x => !ids.Contains(x.PerformanceSeatId))) { allocation.IsActive = false; allocation.ReleasedAt = clock.UtcNow; }
        foreach (var id in ids.Except(current.Select(x => x.PerformanceSeatId))) db.SeatAllocations.Add(new SeatAllocation { ReservationId = reservation.Id, PerformanceSeatId = id, Type = SeatAllocationType.Reservation, IsActive = true, CreatedAt = clock.UtcNow });
        reservation.UpdatedAt = clock.UtcNow;
        reservation.AuditEvents.Add(new ReservationAuditEvent { EventType = "SeatsChanged", Details = string.Join(',', ids), AdminUserId = adminUserId, CreatedAt = clock.UtcNow });
        await db.SaveChangesAsync(token);
    }

    public async Task SetStatusAsync(Reservation reservation, ReservationStatus status, int? adminUserId, CancellationToken token)
    {
        var allocations = await db.SeatAllocations.Where(x => x.ReservationId == reservation.Id).ToListAsync(token);
        if (status == ReservationStatus.Active)
        {
            var lastRelease = allocations.Where(x => x.ReleasedAt.HasValue).Max(x => x.ReleasedAt);
            var latestIds = allocations.Where(x => x.ReleasedAt == lastRelease).Select(x => x.PerformanceSeatId).Distinct().ToArray();
            if (latestIds.Length == 0) throw new ValidationException("This reservation has no previously released seats to reactivate.");
            foreach (var id in latestIds) db.SeatAllocations.Add(new SeatAllocation { ReservationId = reservation.Id, PerformanceSeatId = id, Type = SeatAllocationType.Reservation, IsActive = true, CreatedAt = clock.UtcNow });
        }
        else foreach (var allocation in allocations.Where(x => x.IsActive)) { allocation.IsActive = false; allocation.ReleasedAt = clock.UtcNow; }
        reservation.Status = status; reservation.UpdatedAt = clock.UtcNow;
        reservation.AuditEvents.Add(new ReservationAuditEvent { EventType = $"Status:{status}", AdminUserId = adminUserId, CreatedAt = clock.UtcNow });
        await db.SaveChangesAsync(token);
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? CleanEmail(string? value) => Clean(value)?.ToLowerInvariant();
}
