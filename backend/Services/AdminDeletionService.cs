using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;

namespace Theatre.Api.Services;

public interface IAdminDeletionService
{
    Task DeletePerformancesAsync(IReadOnlyCollection<int> performanceIds, CancellationToken token);
}

public sealed class AdminDeletionService(AppDbContext db) : IAdminDeletionService
{
    public async Task DeletePerformancesAsync(IReadOnlyCollection<int> performanceIds, CancellationToken token)
    {
        var ids = performanceIds.Distinct().ToArray();
        if (ids.Length == 0) return;

        var reservationIds = db.Reservations.Where(x => ids.Contains(x.PerformanceId)).Select(x => x.Id);
        await db.ReservationAdminAudits.Where(x => x.ReservationId.HasValue && reservationIds.Contains(x.ReservationId.Value)).ExecuteDeleteAsync(token);
        await db.SeatAllocations.Where(x => (x.ReservationId.HasValue && reservationIds.Contains(x.ReservationId.Value)) || ids.Contains(x.PerformanceSeat.Layout.PerformanceId)).ExecuteDeleteAsync(token);
        await db.ReservationAuditEvents.Where(x => reservationIds.Contains(x.ReservationId)).ExecuteDeleteAsync(token);
        await db.Reservations.Where(x => ids.Contains(x.PerformanceId)).ExecuteDeleteAsync(token);
        await db.PerformanceSeatHolds.Where(x => ids.Contains(x.PerformanceSeat.Layout.PerformanceId)).ExecuteDeleteAsync(token);
        await db.PerformanceSeats.Where(x => ids.Contains(x.Layout.PerformanceId)).ExecuteDeleteAsync(token);
        await db.PerformanceSeatingLayouts.Where(x => ids.Contains(x.PerformanceId)).ExecuteDeleteAsync(token);
        await db.ShowPerformances.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(token);
    }
}
