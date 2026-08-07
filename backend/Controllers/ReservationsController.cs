using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Route("api/{languageCode:regex(^(sq|en)$)}/reservations")]
public sealed class ReservationsController(AppDbContext db, IReservationService reservations, IClock clock) : ControllerBase
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(5);

    [HttpGet("performances/{performanceId:int}/seats")]
    public async Task<ActionResult<PublicSeatingDto>> Seats(string languageCode, int performanceId, [FromQuery] Guid? holdToken, CancellationToken token)
    {
        var performance = await db.ShowPerformances.AsNoTracking().Where(x => x.Id == performanceId && x.IsPublished && x.ReservationMode == ReservationMode.Internal)
            .Select(x => new { Entity = x, x.Id, x.StartDateTimeUtc, Layout = x.SeatingLayout!, Title = x.Show.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.Title).FirstOrDefault() ?? x.Show.Translations.Select(t => t.Title).FirstOrDefault()!, Venue = x.Location == null ? x.Hall : x.Location.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.Name).FirstOrDefault() ?? x.Location.Translations.Select(t => t.Name).FirstOrDefault() }).SingleOrDefaultAsync(token);
        if (performance is null) return NotFound();
        var now = clock.UtcNow;
        var seats = await db.PerformanceSeats.AsNoTracking().Where(x => x.Layout.PerformanceId == performanceId).OrderBy(x => x.SectionOrder).ThenBy(x => x.RowOrder).ThenBy(x => x.SeatOrder)
            .Select(x => new SeatDto(x.Id, x.SectionName, x.RowLabel, x.SeatLabel, x.SectionOrder, x.RowOrder, x.SeatOrder, x.PositionX, x.PositionY, x.Rotation, x.IsActive,
                !x.IsActive ? "Disabled" : x.Allocations.Any(a => a.IsActive) || x.Holds.Any(h => h.ExpiresAt > now && (!holdToken.HasValue || h.HoldToken != holdToken.Value)) ? "Unavailable" : "Available", null, null, null)).ToListAsync(token);
        var rules = ReservationRules.Evaluate(performance.Entity, now);
        return Ok(new PublicSeatingDto(performance.Id, performance.Title, performance.StartDateTimeUtc, performance.Venue ?? (languageCode == "sq" ? "Lokacioni do të njoftohet" : "Venue to be announced"), performance.Layout.CanvasWidth, performance.Layout.CanvasHeight, performance.Layout.StageLabel, performance.Layout.StageX, performance.Layout.StageY, performance.Layout.StageWidth, performance.Layout.StageHeight, rules.IsAvailable, performance.Entity.MaxSeatsPerReservation, rules.Message, seats));
    }

    [HttpPost("performances/{performanceId:int}/holds")]
    public async Task<ActionResult<SeatHoldDto>> Hold(string languageCode, int performanceId, HoldSeatsRequest request, CancellationToken token)
    {
        var ids = request.SeatIds.Distinct().ToArray(); if (ids.Length == 0) throw new ValidationException("Select at least one seat.");
        var performance = await db.ShowPerformances.SingleOrDefaultAsync(x => x.Id == performanceId && x.IsPublished && x.ReservationMode == ReservationMode.Internal && x.Status == PerformanceStatus.Scheduled, token);
        if (performance is null) return NotFound();
        ReservationRules.EnsureAvailable(performance, clock.UtcNow, ids.Length);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var now = clock.UtcNow; var holdToken = request.HoldToken is { } supplied && supplied != Guid.Empty ? supplied : Guid.NewGuid();
        db.PerformanceSeatHolds.RemoveRange(await db.PerformanceSeatHolds.Where(x => x.ExpiresAt <= now || x.HoldToken == holdToken).ToListAsync(token)); await db.SaveChangesAsync(token);
        if (await db.PerformanceSeats.CountAsync(x => ids.Contains(x.Id) && x.Layout.PerformanceId == performanceId && x.IsActive, token) != ids.Length) throw new ValidationException("One or more selected seats are invalid.");
        if (await db.SeatAllocations.AnyAsync(x => ids.Contains(x.PerformanceSeatId) && x.IsActive, token) || await db.PerformanceSeatHolds.AnyAsync(x => ids.Contains(x.PerformanceSeatId) && x.ExpiresAt > now, token)) throw new ConflictException("One or more selected seats have just become unavailable.");
        var expiresAt = now.Add(HoldDuration); foreach (var id in ids) db.PerformanceSeatHolds.Add(new PerformanceSeatHold { Id = Guid.NewGuid(), HoldToken = holdToken, PerformanceSeatId = id, CreatedAt = now, ExpiresAt = expiresAt });
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return Ok(new SeatHoldDto(holdToken, expiresAt));
    }

    [HttpDelete("holds/{holdToken:guid}")]
    public async Task<IActionResult> ReleaseHold(string languageCode, Guid holdToken, CancellationToken token)
    {
        var holds = await db.PerformanceSeatHolds.Where(x => x.HoldToken == holdToken).ToListAsync(token); db.PerformanceSeatHolds.RemoveRange(holds); await db.SaveChangesAsync(token); return NoContent();
    }

    [HttpPost("performances/{performanceId:int}")]
    public async Task<ActionResult<ReservationResultDto>> Create(string languageCode, int performanceId, CreatePublicReservationRequest request, CancellationToken token)
    {
        if (!await db.ShowPerformances.AnyAsync(x => x.Id == performanceId && x.IsPublished && x.ReservationMode == ReservationMode.Internal && x.Status == PerformanceStatus.Scheduled, token)) return NotFound();
        if (request.HoldToken == Guid.Empty) throw new ValidationException("The seat hold is missing. Select your seats again.");
        var ids = request.SeatIds.Distinct().ToArray(); await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token); var now = clock.UtcNow;
        var holds = await db.PerformanceSeatHolds.Where(x => x.HoldToken == request.HoldToken && ids.Contains(x.PerformanceSeatId) && x.PerformanceSeat.Layout.PerformanceId == performanceId && x.ExpiresAt > now).ToListAsync(token);
        if (holds.Count != ids.Length) throw new ConflictException("Your temporary seat hold expired or the selection changed. Please select the seats again.");
        db.PerformanceSeatHolds.RemoveRange(holds);
        var customer = await reservations.FindOrCreateCustomerAsync(request.FullName, request.Phone, request.CountryPrefix, request.Email, token); var reservation = await reservations.CreateAsync(performanceId, customer, ids, ReservationSource.PublicWebsite, null, token);
        await transaction.CommitAsync(token); return Created(string.Empty, new ReservationResultDto(reservation.Id, reservation.Status.ToString(), reservation.ConfirmationStatus.ToString(), reservation.ReservedAt));
    }
}
