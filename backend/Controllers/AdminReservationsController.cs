using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin"), Route("api/admin/reservations")]
public sealed class AdminReservationsController(AppDbContext db, IReservationService service, IPhoneNumberNormalizer phones, IClock clock) : ControllerBase
{
    [HttpGet("performances/{performanceId:int}/layout")]
    public async Task<ActionResult<SeatingTemplateDto>> Layout(int performanceId, CancellationToken token)
    {
        var layout = await db.PerformanceSeatingLayouts.AsNoTracking().Include(x => x.Performance).ThenInclude(x => x.Show).ThenInclude(x => x.Translations).Include(x => x.Seats).SingleOrDefaultAsync(x => x.PerformanceId == performanceId, token); if (layout is null) return NotFound();
        var sections = layout.Seats.GroupBy(x => new { x.SectionName, x.SectionOrder }).OrderBy(x => x.Key.SectionOrder).Select(section => new SaveTemplateSectionRequest(section.Key.SectionName, section.Key.SectionOrder, section.GroupBy(x => new { x.RowLabel, x.RowOrder }).OrderBy(x => x.Key.RowOrder).Select(row => new SaveTemplateRowRequest(row.Key.RowLabel, row.Key.RowOrder, row.OrderBy(x => x.SeatOrder).Select(x => new SaveTemplateSeatRequest(x.Id, x.SeatLabel, x.SeatOrder, x.PositionX, x.PositionY, x.Rotation, x.IsActive)).ToArray())).ToArray())).ToList();
        var title = layout.Performance.Show.Translations.FirstOrDefault(x => x.LanguageId == 1)?.Title ?? $"Performance {performanceId}";
        return Ok(new SeatingTemplateDto(layout.Id, 0, title, title, false, true, layout.CanvasWidth, layout.CanvasHeight, layout.StageLabel, layout.StageX, layout.StageY, layout.StageWidth, layout.StageHeight, sections));
    }

    [HttpPut("performances/{performanceId:int}/layout")]
    [Authorize(Policy = "ManageTheatreSchemas")]
    public async Task<ActionResult<SeatingTemplateDto>> SaveLayout(int performanceId, SaveSeatingTemplateRequest request, CancellationToken token)
    {
        var layout = await db.PerformanceSeatingLayouts.Include(x => x.Seats).SingleOrDefaultAsync(x => x.PerformanceId == performanceId, token); if (layout is null) return NotFound();
        var incoming = request.Sections.SelectMany(s => s.Rows.SelectMany(r => r.Seats.Select(seat => new { Section = s, Row = r, Seat = seat }))).ToList(); var incomingIds = incoming.Where(x => x.Seat.Id.HasValue).Select(x => x.Seat.Id!.Value).ToHashSet(); var removedIds = layout.Seats.Where(x => !incomingIds.Contains(x.Id)).Select(x => x.Id).ToArray();
        if (await db.SeatAllocations.AnyAsync(x => removedIds.Contains(x.PerformanceSeatId) && x.IsActive, token)) throw new ConflictException("Release active allocations before removing those seats.");
        db.PerformanceSeats.RemoveRange(layout.Seats.Where(x => removedIds.Contains(x.Id)));
        foreach (var item in incoming) { var seat = item.Seat.Id.HasValue ? layout.Seats.SingleOrDefault(x => x.Id == item.Seat.Id) : null; if (seat is null) { seat = new PerformanceSeat { Layout = layout }; db.PerformanceSeats.Add(seat); } if (!item.Seat.IsActive && seat.Id != 0 && await db.SeatAllocations.AnyAsync(x => x.PerformanceSeatId == seat.Id && x.IsActive, token)) throw new ConflictException("Release the active allocation before disabling this seat."); seat.SectionName = item.Section.Name.Trim(); seat.SectionOrder = item.Section.DisplayOrder; seat.RowLabel = item.Row.Label.Trim(); seat.RowOrder = item.Row.DisplayOrder; seat.SeatLabel = item.Seat.Label.Trim(); seat.SeatOrder = item.Seat.DisplayOrder; seat.PositionX = item.Seat.X; seat.PositionY = item.Seat.Y; seat.Rotation = item.Seat.Rotation; seat.IsActive = item.Seat.IsActive; }
        layout.CanvasWidth = request.CanvasWidth; layout.CanvasHeight = request.CanvasHeight; layout.StageLabel = request.StageLabel; layout.StageX = request.StageX; layout.StageY = request.StageY; layout.StageWidth = request.StageWidth; layout.StageHeight = request.StageHeight; layout.UpdatedAt = clock.UtcNow; Audit("PerformanceSchemaEdited", "Performance", performanceId.ToString(), null, new { SeatCount = incoming.Count }); await db.SaveChangesAsync(token); return await Layout(performanceId, token);
    }
    [HttpGet]
    public async Task<ActionResult<ReservationListDto>> List([FromQuery] int? performanceId, [FromQuery] string? search, [FromQuery] string? seat, [FromQuery] string? section, [FromQuery] string? row, [FromQuery] DateOnly? reservationDate, [FromQuery] string? confirmationStatus, [FromQuery] string? status, [FromQuery] string? source, [FromQuery] string? sortBy, [FromQuery] string? sortDirection, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        var query = db.Reservations.AsNoTracking().AsQueryable();
        if (performanceId.HasValue) query = query.Where(x => x.PerformanceId == performanceId);
        if (!string.IsNullOrWhiteSpace(search)) { var q = search.Trim(); query = query.Where(x => x.Customer.FullName.Contains(q) || x.Customer.NormalizedPhone.Contains(q)); }
        if (!string.IsNullOrWhiteSpace(seat)) query = query.Where(x => x.SeatAllocations.Any(a => a.PerformanceSeat.SeatLabel.Contains(seat)));
        if (!string.IsNullOrWhiteSpace(section)) query = query.Where(x => x.SeatAllocations.Any(a => a.PerformanceSeat.SectionName.Contains(section)));
        if (!string.IsNullOrWhiteSpace(row)) query = query.Where(x => x.SeatAllocations.Any(a => a.PerformanceSeat.RowLabel.Contains(row)));
        if (reservationDate.HasValue) { var from = new DateTimeOffset(reservationDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); query = query.Where(x => x.ReservedAt >= from && x.ReservedAt < from.AddDays(1)); }
        if (Enum.TryParse<ConfirmationStatus>(confirmationStatus, true, out var confirmation)) query = query.Where(x => x.ConfirmationStatus == confirmation);
        if (Enum.TryParse<ReservationStatus>(status, true, out var state)) query = query.Where(x => x.Status == state);
        if (Enum.TryParse<ReservationSource>(source, true, out var origin)) query = query.Where(x => x.Source == origin);
        var total = await query.CountAsync(token); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 100); var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "customer" => descending ? query.OrderByDescending(x => x.Customer.FullName) : query.OrderBy(x => x.Customer.FullName),
            "phone" => descending ? query.OrderByDescending(x => x.Customer.NormalizedPhone) : query.OrderBy(x => x.Customer.NormalizedPhone),
            "seat" => descending ? query.OrderByDescending(x => x.SeatAllocations.Where(a => a.IsActive).Select(a => a.PerformanceSeat.SeatLabel).FirstOrDefault()) : query.OrderBy(x => x.SeatAllocations.Where(a => a.IsActive).Select(a => a.PerformanceSeat.SeatLabel).FirstOrDefault()),
            "section" => descending ? query.OrderByDescending(x => x.SeatAllocations.Where(a => a.IsActive).Select(a => a.PerformanceSeat.SectionName).FirstOrDefault()) : query.OrderBy(x => x.SeatAllocations.Where(a => a.IsActive).Select(a => a.PerformanceSeat.SectionName).FirstOrDefault()),
            "row" => descending ? query.OrderByDescending(x => x.SeatAllocations.Where(a => a.IsActive).Select(a => a.PerformanceSeat.RowLabel).FirstOrDefault()) : query.OrderBy(x => x.SeatAllocations.Where(a => a.IsActive).Select(a => a.PerformanceSeat.RowLabel).FirstOrDefault()),
            "confirmation" => descending ? query.OrderByDescending(x => x.ConfirmationStatus) : query.OrderBy(x => x.ConfirmationStatus),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "source" => descending ? query.OrderByDescending(x => x.Source) : query.OrderBy(x => x.Source),
            _ => descending ? query.OrderByDescending(x => x.ReservedAt) : query.OrderBy(x => x.ReservedAt)
        };
        var entities = await query.Include(x => x.Customer).Include(x => x.Performance).ThenInclude(x => x.Show).ThenInclude(x => x.Translations).Include(x => x.SeatAllocations).ThenInclude(x => x.PerformanceSeat).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return Ok(new ReservationListDto(entities.Select(Map).ToList(), total, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationListItemDto>> Get(int id, CancellationToken token)
    {
        var item = await db.Reservations.AsNoTracking().Include(x => x.Customer).Include(x => x.Performance).ThenInclude(x => x.Show).ThenInclude(x => x.Translations).Include(x => x.SeatAllocations).ThenInclude(x => x.PerformanceSeat).SingleOrDefaultAsync(x => x.Id == id, token);
        return item is null ? NotFound() : Ok(Map(item));
    }

    [HttpGet("performances/{performanceId:int}/seats")]
    public async Task<ActionResult<IReadOnlyList<SeatDto>>> Seats(int performanceId, CancellationToken token) => Ok(await db.PerformanceSeats.AsNoTracking().Where(x => x.Layout.PerformanceId == performanceId).OrderBy(x => x.SectionOrder).ThenBy(x => x.RowOrder).ThenBy(x => x.SeatOrder).Select(x => new SeatDto(x.Id, x.SectionName, x.RowLabel, x.SeatLabel, x.SectionOrder, x.RowOrder, x.SeatOrder, x.PositionX, x.PositionY, x.Rotation, x.IsActive,
        !x.IsActive ? "Disabled" : x.Allocations.Where(a => a.IsActive).Select(a => a.Type == SeatAllocationType.AdminBlock ? "AdminBlocked" : a.Reservation!.ConfirmationStatus == ConfirmationStatus.Confirmed ? "Confirmed" : a.Reservation.Source == ReservationSource.Admin ? "AdminReserved" : "Unconfirmed").FirstOrDefault() ?? "Available", x.Allocations.Where(a => a.IsActive).Select(a => a.ReservationId).FirstOrDefault(), x.Allocations.Where(a => a.IsActive).Select(a => (long?)a.Id).FirstOrDefault(), x.Allocations.Where(a => a.IsActive).Select(a => a.AdminComment).FirstOrDefault())).ToListAsync(token));

    [HttpPut("performances/{performanceId:int}/seats/{seatId:int}")]
    [Authorize(Policy = "ManageTheatreSchemas")]
    public async Task<IActionResult> UpdateSeat(int performanceId, int seatId, UpdatePerformanceSeatRequest request, CancellationToken token)
    {
        var seat = await db.PerformanceSeats.Include(x => x.Layout).SingleOrDefaultAsync(x => x.Id == seatId && x.Layout.PerformanceId == performanceId, token); if (seat is null) return NotFound();
        if (!request.IsActive && await db.SeatAllocations.AnyAsync(x => x.PerformanceSeatId == seatId && x.IsActive, token)) throw new ConflictException("Release the active reservation or block before disabling this seat.");
        seat.SectionName = request.Section.Trim(); seat.RowLabel = request.Row.Trim(); seat.SeatLabel = request.Label.Trim(); seat.SectionOrder = request.SectionOrder; seat.RowOrder = request.RowOrder; seat.SeatOrder = request.SeatOrder; seat.PositionX = request.X; seat.PositionY = request.Y; seat.Rotation = request.Rotation; seat.IsActive = request.IsActive; seat.Layout.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token); return NoContent();
    }

    [HttpPost]
    [Authorize(Policy = "ManageReservations")]
    public async Task<ActionResult<ReservationResultDto>> Create(AdminReservationRequest request, CancellationToken token)
    {
        Customer customer;
        if (request.CustomerId.HasValue) customer = await db.Customers.FindAsync([request.CustomerId.Value], token) ?? throw new ValidationException("Customer not found.");
        else if (request.Customer is not null) customer = await service.FindOrCreateCustomerAsync(request.Customer.FullName, request.Customer.Phone, request.Customer.CountryPrefix, request.Customer.Email, token);
        else throw new ValidationException("Select or create a customer.");
        var reservation = await service.CreateAsync(request.CustomerId.HasValue ? await PerformanceForSeats(request.SeatIds, token) : await PerformanceForSeats(request.SeatIds, token), customer, request.SeatIds, ReservationSource.Admin, request.Comment, token);
        Audit("ReservationCreated", "Reservation", reservation.Id.ToString(), null, new { reservation.CustomerId, reservation.PerformanceId, Seats = request.SeatIds }, reservation.Id, reservation.CustomerId); await db.SaveChangesAsync(token);
        return Ok(new ReservationResultDto(reservation.Id, reservation.Status.ToString(), reservation.ConfirmationStatus.ToString(), reservation.ReservedAt));
    }

    [HttpPost("performances/{performanceId:int}/blocks")]
    [Authorize(Policy = "BlockSeats")]
    public async Task<IActionResult> Block(int performanceId, AdminBlockSeatsRequest request, CancellationToken token)
    {
        var ids = request.SeatIds.Distinct().ToArray();
        if (await db.PerformanceSeats.CountAsync(x => ids.Contains(x.Id) && x.Layout.PerformanceId == performanceId && x.IsActive, token) != ids.Length) throw new ValidationException("One or more seats are invalid.");
        foreach (var id in ids) db.SeatAllocations.Add(new SeatAllocation { PerformanceSeatId = id, Type = SeatAllocationType.AdminBlock, IsActive = true, AdminComment = request.Comment?.Trim(), CreatedAt = clock.UtcNow });
        Audit("SeatsBlocked", "Performance", performanceId.ToString(), null, new { Seats = ids, request.Comment }); await db.SaveChangesAsync(token); return NoContent();
    }

    [HttpDelete("blocks/{allocationId:long}")]
    [Authorize(Policy = "BlockSeats")]
    public async Task<IActionResult> ReleaseBlock(long allocationId, CancellationToken token) { var item = await db.SeatAllocations.Include(x => x.PerformanceSeat).ThenInclude(x => x.Layout).SingleOrDefaultAsync(x => x.Id == allocationId && x.Type == SeatAllocationType.AdminBlock && x.IsActive, token); if (item is null) return NotFound(); item.IsActive = false; item.ReleasedAt = clock.UtcNow; Audit("AdminBlockReleased", "SeatAllocation", allocationId.ToString(), new { item.PerformanceSeatId, item.AdminComment }, null); await db.SaveChangesAsync(token); return NoContent(); }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "ManageReservations")]
    public async Task<IActionResult> Update(int id, UpdateReservationRequest request, CancellationToken token)
    {
        var item = await db.Reservations.Include(x => x.AuditEvents).SingleOrDefaultAsync(x => x.Id == id, token); if (item is null) return NotFound();
        int? adminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
        var before = new { item.ConfirmationStatus, item.Status, item.AdminComment, Seats = await db.SeatAllocations.Where(x => x.ReservationId == id && x.IsActive).Select(x => x.PerformanceSeatId).ToListAsync(token) };
        if (request.SeatIds is not null) await service.ReplaceSeatsAsync(item, request.SeatIds, adminId, token);
        if (request.AdminComment is not null) { item.AdminComment = request.AdminComment.Trim(); item.UpdatedAt = clock.UtcNow; }
        if (Enum.TryParse<ConfirmationStatus>(request.ConfirmationStatus, true, out var confirmation) && item.ConfirmationStatus != confirmation) { item.ConfirmationStatus = confirmation; item.AuditEvents.Add(new ReservationAuditEvent { EventType = $"Confirmation:{confirmation}", AdminUserId = adminId, CreatedAt = clock.UtcNow }); }
        if (Enum.TryParse<ReservationStatus>(request.Status, true, out var status) && item.Status != status) await service.SetStatusAsync(item, status, adminId, token); else await db.SaveChangesAsync(token);
        Audit("ReservationUpdated", "Reservation", id.ToString(), before, new { item.ConfirmationStatus, item.Status, item.AdminComment, request.SeatIds }, id, item.CustomerId); await db.SaveChangesAsync(token); return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManageReservations")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var item = await db.Reservations.SingleOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        db.SeatAllocations.RemoveRange(await db.SeatAllocations.Where(x => x.ReservationId == id).ToListAsync(token));
        db.ReservationAdminAudits.RemoveRange(await db.ReservationAdminAudits.Where(x => x.ReservationId == id).ToListAsync(token));
        db.Reservations.Remove(item);
        db.AdminActivities.Add(new AdminActivity { AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId) ? adminId : null, Action = "Deleted", EntityType = "Reservation", EntityId = id.ToString(), Summary = $"Permanently deleted reservation #{id}", CreatedAt = clock.UtcNow });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return NoContent();
    }

    [HttpPut("customers/{id:int}")]
    [Authorize(Policy = "ManageReservations")]
    public async Task<IActionResult> UpdateCustomer(int id, SaveCustomerRequest request, CancellationToken token) { var item = await db.Customers.FindAsync([id], token); if (item is null) return NotFound(); var before = new { item.FullName, item.NormalizedPhone, item.Email }; item.FullName = request.FullName.Trim(); item.NormalizedPhone = phones.Normalize(request.Phone, request.CountryPrefix); item.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(); item.UpdatedAt = clock.UtcNow; Audit("CustomerEdited", "Customer", id.ToString(), before, new { item.FullName, item.NormalizedPhone, item.Email }, customerId: id); await db.SaveChangesAsync(token); return NoContent(); }

    [HttpGet("customers")]
    public async Task<ActionResult<CustomerListDto>> Customers([FromQuery] string? search, [FromQuery] int? showId, [FromQuery] DateOnly? reservationFrom, [FromQuery] DateOnly? reservationTo, [FromQuery] string? sortBy, [FromQuery] string? sortDirection, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        var q = db.Customers.AsNoTracking().Where(x => x.Reservations.Any());
        if (!string.IsNullOrWhiteSpace(search)) { var value = search.Trim(); q = q.Where(x => x.FullName.Contains(value) || x.NormalizedPhone.Contains(value) || (x.Email != null && x.Email.Contains(value))); }
        if (showId.HasValue) q = q.Where(x => x.Reservations.Any(r => r.Performance.ShowId == showId));
        if (reservationFrom.HasValue) { var from = new DateTimeOffset(reservationFrom.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); q = q.Where(x => x.Reservations.Any(r => r.ReservedAt >= from)); }
        if (reservationTo.HasValue) { var to = new DateTimeOffset(reservationTo.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1); q = q.Where(x => x.Reservations.Any(r => r.ReservedAt < to)); }
        var total = await q.CountAsync(token); var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        q = sortBy?.ToLowerInvariant() switch { "phone" => desc ? q.OrderByDescending(x => x.NormalizedPhone) : q.OrderBy(x => x.NormalizedPhone), "email" => desc ? q.OrderByDescending(x => x.Email) : q.OrderBy(x => x.Email), "firstreservation" => desc ? q.OrderByDescending(x => x.Reservations.Min(r => r.ReservedAt)) : q.OrderBy(x => x.Reservations.Min(r => r.ReservedAt)), "lastreservation" => desc ? q.OrderByDescending(x => x.Reservations.Max(r => r.ReservedAt)) : q.OrderBy(x => x.Reservations.Max(r => r.ReservedAt)), _ => desc ? q.OrderByDescending(x => x.FullName) : q.OrderBy(x => x.FullName) };
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new CustomerListItemDto(x.Id, x.FullName, x.NormalizedPhone, x.Email, x.CreatedAt, x.Reservations.Min(r => (DateTimeOffset?)r.ReservedAt), x.Reservations.Max(r => (DateTimeOffset?)r.ReservedAt), x.Reservations.Count, x.Reservations.SelectMany(r => r.SeatAllocations).Count(), x.AnonymizedAt)).ToListAsync(token);
        return Ok(new CustomerListDto(items, total, page, pageSize));
    }

    [HttpGet("customers/{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> Customer(int id, CancellationToken token)
    {
        var item = await db.Customers.AsNoTracking().Include(x => x.Reservations).ThenInclude(x => x.Performance).ThenInclude(x => x.Location).ThenInclude(x => x!.Translations).Include(x => x.Reservations).ThenInclude(x => x.Performance).ThenInclude(x => x.Show).ThenInclude(x => x.Translations).Include(x => x.Reservations).ThenInclude(x => x.SeatAllocations).ThenInclude(x => x.PerformanceSeat).SingleOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        var audit = await AuditQuery().Where(x => x.CustomerId == id).ToListAsync(token);
        var history = item.Reservations.OrderByDescending(x => x.ReservedAt).Select(r => new CustomerReservationHistoryDto(r.Id, r.Performance.Show.Translations.FirstOrDefault(t => t.LanguageId == 1)?.Title ?? $"Show {r.Performance.ShowId}", r.Performance.StartDateTimeUtc, r.Performance.Location?.Translations.FirstOrDefault(t => t.LanguageId == 1)?.Name ?? r.Performance.Hall ?? "-", r.SeatAllocations.OrderBy(a => a.PerformanceSeat.SectionOrder).ThenBy(a => a.PerformanceSeat.RowOrder).ThenBy(a => a.PerformanceSeat.SeatOrder).Select(a => $"{a.PerformanceSeat.SectionName} {a.PerformanceSeat.RowLabel}-{a.PerformanceSeat.SeatLabel}").Distinct().ToList(), r.ConfirmationStatus.ToString(), r.Status.ToString(), r.Source.ToString(), r.AdminComment, r.ReservedAt)).ToList();
        return Ok(new CustomerDetailDto(item.Id, item.FullName, item.NormalizedPhone, item.Email, item.CreatedAt, item.AnonymizedAt, history, audit.Select(MapAudit).ToList()));
    }

    [HttpPost("customers/{id:int}/anonymize")]
    [Authorize(Policy = "ExportCustomerData")]
    public async Task<IActionResult> AnonymizeCustomer(int id, CancellationToken token)
    {
        var item = await db.Customers.FindAsync([id], token); if (item is null) return NotFound(); if (item.AnonymizedAt.HasValue) return NoContent();
        var before = new { item.FullName, item.NormalizedPhone, item.Email }; item.FullName = $"Anonymized customer {item.Id}"; item.NormalizedPhone = $"+000{item.Id:D7}"; item.Email = null; item.AnonymizedAt = item.UpdatedAt = clock.UtcNow;
        Audit("CustomerAnonymized", "Customer", id.ToString(), before, new { item.FullName, item.NormalizedPhone, item.AnonymizedAt }, customerId: id); await db.SaveChangesAsync(token); return NoContent();
    }

    [HttpDelete("customers/{id:int}")]
    [Authorize(Policy = "ManageReservations")]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken token)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, token);
        if (customer is null) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var reservationIds = await db.Reservations.Where(x => x.CustomerId == id).Select(x => x.Id).ToArrayAsync(token);
        db.SeatAllocations.RemoveRange(await db.SeatAllocations.Where(x => x.ReservationId.HasValue && reservationIds.Contains(x.ReservationId.Value)).ToListAsync(token));
        db.ReservationAdminAudits.RemoveRange(await db.ReservationAdminAudits.Where(x => x.CustomerId == id || (x.ReservationId.HasValue && reservationIds.Contains(x.ReservationId.Value))).ToListAsync(token));
        db.Reservations.RemoveRange(await db.Reservations.Where(x => x.CustomerId == id).ToListAsync(token));
        db.Customers.Remove(customer);
        db.AdminActivities.Add(new AdminActivity { AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId) ? adminId : null, Action = "Deleted", EntityType = "Customer", EntityId = id.ToString(), Summary = $"Permanently deleted customer #{id} and {reservationIds.Length} reservation(s)", CreatedAt = clock.UtcNow });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return NoContent();
    }

    [HttpGet("customers/export")]
    [Authorize(Policy = "ExportCustomerData")]
    public async Task<FileContentResult> ExportCustomers([FromQuery] string? search, [FromQuery] int? showId, [FromQuery] DateOnly? reservationFrom, [FromQuery] DateOnly? reservationTo, [FromQuery] int? customerId, CancellationToken token)
    {
        var q = db.Customers.AsNoTracking().Where(x => x.Reservations.Any()); if (customerId.HasValue) q = q.Where(x => x.Id == customerId); if (!string.IsNullOrWhiteSpace(search)) { var value = search.Trim(); q = q.Where(x => x.FullName.Contains(value) || x.NormalizedPhone.Contains(value) || (x.Email != null && x.Email.Contains(value))); }
        if (showId.HasValue) q = q.Where(x => x.Reservations.Any(r => r.Performance.ShowId == showId));
        if (reservationFrom.HasValue) { var from = new DateTimeOffset(reservationFrom.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); q = q.Where(x => x.Reservations.Any(r => r.ReservedAt >= from)); }
        if (reservationTo.HasValue) { var to = new DateTimeOffset(reservationTo.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1); q = q.Where(x => x.Reservations.Any(r => r.ReservedAt < to)); }
        var items = await q.Include(x => x.Reservations).ThenInclude(x => x.Performance).ThenInclude(x => x.Show).ThenInclude(x => x.Translations).Include(x => x.Reservations).ThenInclude(x => x.SeatAllocations).ThenInclude(x => x.PerformanceSeat).OrderBy(x => x.FullName).ToListAsync(token);
        var csv = new StringBuilder("CustomerId,Customer,Phone,Email,FirstReservation,LastReservation,Reservations,Seats,Play,PerformanceDate,ReservationStatus,Confirmation,Source,Comment\r\n"); foreach (var customer in items) foreach (var reservation in customer.Reservations.DefaultIfEmpty()) csv.AppendLine(string.Join(',', new[] { customer.Id.ToString(), customer.FullName, customer.NormalizedPhone, customer.Email ?? "", customer.Reservations.Min(r => (DateTimeOffset?)r.ReservedAt)?.ToString("O") ?? "", customer.Reservations.Max(r => (DateTimeOffset?)r.ReservedAt)?.ToString("O") ?? "", customer.Reservations.Count.ToString(), reservation == null ? "" : string.Join("; ", reservation.SeatAllocations.Select(a => $"{a.PerformanceSeat.SectionName} {a.PerformanceSeat.RowLabel}-{a.PerformanceSeat.SeatLabel}")), reservation?.Performance.Show.Translations.FirstOrDefault()?.Title ?? "", reservation?.Performance.StartDateTimeUtc.ToString("O") ?? "", reservation?.Status.ToString() ?? "", reservation?.ConfirmationStatus.ToString() ?? "", reservation?.Source.ToString() ?? "", reservation?.AdminComment ?? "" }.Select(Csv)));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", customerId.HasValue ? $"customer-{customerId}-history.csv" : "customers.csv");
    }

    [HttpGet("{id:int}/audit")]
    [Authorize(Policy = "ViewReservationAudit")]
    public async Task<ActionResult<IReadOnlyList<AdminAuditDto>>> ReservationAudit(int id, CancellationToken token) => Ok((await AuditQuery().Where(x => x.ReservationId == id).ToListAsync(token)).Select(MapAudit));

    [HttpGet("export")]
    [Authorize(Policy = "ExportCustomerData")]
    public async Task<FileContentResult> Export([FromQuery] int? performanceId, [FromQuery] bool confirmedOnly, [FromQuery] bool seatBased, [FromQuery] string? search, [FromQuery] string? seat, [FromQuery] string? section, [FromQuery] string? row, [FromQuery] DateOnly? reservationDate, [FromQuery] string? confirmationStatus, [FromQuery] string? status, [FromQuery] string? source, CancellationToken token)
    {
        var q = db.Reservations.AsNoTracking().Include(x => x.Customer).Include(x => x.Performance).ThenInclude(x => x.Show).ThenInclude(x => x.Translations).Include(x => x.SeatAllocations).ThenInclude(x => x.PerformanceSeat).AsQueryable();
        if (performanceId.HasValue) q = q.Where(x => x.PerformanceId == performanceId); if (confirmedOnly) q = q.Where(x => x.ConfirmationStatus == ConfirmationStatus.Confirmed && x.Status == ReservationStatus.Active);
        if (!string.IsNullOrWhiteSpace(search)) { var value = search.Trim(); q = q.Where(x => x.Customer.FullName.Contains(value) || x.Customer.NormalizedPhone.Contains(value)); }
        if (!string.IsNullOrWhiteSpace(seat)) q = q.Where(x => x.SeatAllocations.Any(a => a.PerformanceSeat.SeatLabel.Contains(seat))); if (!string.IsNullOrWhiteSpace(section)) q = q.Where(x => x.SeatAllocations.Any(a => a.PerformanceSeat.SectionName.Contains(section))); if (!string.IsNullOrWhiteSpace(row)) q = q.Where(x => x.SeatAllocations.Any(a => a.PerformanceSeat.RowLabel.Contains(row)));
        if (reservationDate.HasValue) { var from = new DateTimeOffset(reservationDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); q = q.Where(x => x.ReservedAt >= from && x.ReservedAt < from.AddDays(1)); }
        if (Enum.TryParse<ConfirmationStatus>(confirmationStatus, true, out var confirmation)) q = q.Where(x => x.ConfirmationStatus == confirmation); if (Enum.TryParse<ReservationStatus>(status, true, out var state)) q = q.Where(x => x.Status == state); if (Enum.TryParse<ReservationSource>(source, true, out var origin)) q = q.Where(x => x.Source == origin);
        var rows = (await q.OrderByDescending(x => x.ReservedAt).ToListAsync(token)).Select(Map).ToList();
        var exportRows = new List<(string[] Cells, int Style)>();
        foreach (var x in rows)
        {
            var seats = x.SeatDetails.Where(s => s.IsActive).ToList();
            if (seats.Count == 0) seats = x.SeatDetails.ToList();
            if (seatBased)
            {
                foreach (var selectedSeat in seats)
                    exportRows.Add(ReservationExportRow(x, selectedSeat.Section, selectedSeat.Row, selectedSeat.Label));
            }
            else
            {
                exportRows.Add(ReservationExportRow(x,
                    string.Join("; ", seats.Select(s => s.Section).Distinct()),
                    string.Join("; ", seats.Select(s => s.Row).Distinct()),
                    string.Join("; ", seats.Select(s => s.Label))));
            }
        }
        var suffix = seatBased ? "seat-based" : "reservation-based";
        return File(CreateReservationWorkbook(exportRows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"reservations-{suffix}.xlsx");
    }

    private static (string[] Cells, int Style) ReservationExportRow(ReservationListItemDto x, string section, string row, string seat)
    {
        var style = x.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ? 3
            : x.Status.Equals("Released", StringComparison.OrdinalIgnoreCase) ? 5
            : x.ConfirmationStatus.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ? 2
            : x.ConfirmationStatus.Equals("Unconfirmed", StringComparison.OrdinalIgnoreCase) ? 4 : 0;
        return (new[] { x.Id.ToString(), x.CustomerName, x.Phone, x.Email ?? "", x.ShowTitle,
            x.PerformanceDate.ToLocalTime().ToString("dd MMM yyyy, HH:mm"), section, row, seat,
            x.ReservedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm"), x.ConfirmationStatus, x.Status,
            x.Source == "PublicWebsite" ? "Public website" : "Admin-created", x.AdminComment ?? "" }, style);
    }

    private static byte[] CreateReservationWorkbook(IReadOnlyList<(string[] Cells, int Style)> rows)
    {
        var headers = new[] { "Reservation ID", "Customer", "Phone", "Email", "Play", "Performance date", "Section", "Row", "Seat(s)", "Reserved at", "Confirmation", "Status", "Source", "Admin comment" };
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Reservations");
        for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];

        var header = sheet.Range(1, 1, 1, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#78141D");
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 24;

        var fills = new Dictionary<int, (string Background, string Text)>
        {
            [2] = ("#DCFCE7", "#166534"),
            [3] = ("#FEE2E2", "#B91C1C"),
            [4] = ("#FEF3C7", "#92400E"),
            [5] = ("#F3F4F6", "#6B7280")
        };
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var excelRow = rowIndex + 2;
            for (var column = 0; column < rows[rowIndex].Cells.Length; column++)
                sheet.Cell(excelRow, column + 1).Value = CleanExcelText(rows[rowIndex].Cells[column]);
            if (fills.TryGetValue(rows[rowIndex].Style, out var colors))
            {
                var range = sheet.Range(excelRow, 1, excelRow, headers.Length);
                range.Style.Fill.BackgroundColor = XLColor.FromHtml(colors.Background);
                range.Style.Font.FontColor = XLColor.FromHtml(colors.Text);
                if (rows[rowIndex].Style == 3) range.Style.Font.Bold = true;
                if (rows[rowIndex].Style == 5) range.Style.Font.Italic = true;
            }
        }

        var lastRow = Math.Max(1, rows.Count + 1);
        var report = sheet.Range(1, 1, lastRow, headers.Length);
        report.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        report.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        report.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D1D5DB");
        report.Style.Border.InsideBorderColor = XLColor.FromHtml("#D1D5DB");
        report.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        report.Style.Alignment.WrapText = true;
        report.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        var widths = new[] { 14d, 24d, 18d, 28d, 30d, 24d, 15d, 12d, 25d, 24d, 16d, 14d, 18d, 38d };
        for (var column = 0; column < widths.Length; column++) sheet.Column(column + 1).Width = widths[column];
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.Margins.SetLeft(0.25).SetRight(0.25).SetTop(0.5).SetBottom(0.5);

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static string CleanExcelText(string value) => new(value.Where(character => character is '\t' or '\n' or '\r' || character >= ' ').Take(32767).ToArray());

    private async Task<int> PerformanceForSeats(int[] ids, CancellationToken token) { var performances = await db.PerformanceSeats.Where(x => ids.Contains(x.Id)).Select(x => x.Layout.PerformanceId).Distinct().ToListAsync(token); if (performances.Count != 1) throw new ValidationException("All seats must belong to one performance."); return performances[0]; }
    private IQueryable<ReservationAdminAudit> AuditQuery() => db.ReservationAdminAudits.AsNoTracking().Include(x => x.AdminUser).OrderByDescending(x => x.CreatedAt);
    private static AdminAuditDto MapAudit(ReservationAdminAudit x) => new(x.Id, x.AdminUserId, x.AdminUser?.DisplayName, x.ActionType, x.EntityType, x.EntityId, x.PreviousValuesJson, x.NewValuesJson, x.CreatedAt);
    private void Audit(string action, string entityType, string entityId, object? previous, object? current, int? reservationId = null, int? customerId = null) => db.ReservationAdminAudits.Add(new ReservationAdminAudit { AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId) ? adminId : null, ReservationId = reservationId, CustomerId = customerId, ActionType = action, EntityType = entityType, EntityId = entityId, PreviousValuesJson = previous is null ? null : JsonSerializer.Serialize(previous), NewValuesJson = current is null ? null : JsonSerializer.Serialize(current), CreatedAt = clock.UtcNow });
    private static ReservationListItemDto Map(Reservation x)
    {
        var allocations = x.SeatAllocations.OrderBy(a => a.PerformanceSeat.SectionOrder).ThenBy(a => a.PerformanceSeat.RowOrder).ThenBy(a => a.PerformanceSeat.SeatOrder).ToList();
        var displayed = allocations.Where(a => a.IsActive).ToList(); if (displayed.Count == 0) displayed = allocations.GroupBy(a => a.PerformanceSeatId).Select(a => a.OrderByDescending(v => v.CreatedAt).First()).ToList();
        return new(x.Id, x.CustomerId, x.Customer.FullName, x.Customer.NormalizedPhone, x.Customer.Email, x.PerformanceId, x.Performance.Show.Translations.FirstOrDefault(t => t.LanguageId == 1)?.Title ?? x.Performance.Show.Translations.FirstOrDefault()?.Title ?? $"Show {x.Performance.ShowId}", x.Performance.StartDateTimeUtc, displayed.Select(a => $"{a.PerformanceSeat.SectionName} {a.PerformanceSeat.RowLabel}-{a.PerformanceSeat.SeatLabel}").Distinct().ToList(), displayed.Select(a => new ReservationSeatDto(a.PerformanceSeatId, a.PerformanceSeat.SectionName, a.PerformanceSeat.RowLabel, a.PerformanceSeat.SeatLabel, a.IsActive)).ToList(), allocations.Where(a => a.IsActive).Select(a => a.PerformanceSeatId).Distinct().ToList(), x.ReservedAt, x.ConfirmationStatus.ToString(), x.Status.ToString(), x.Source.ToString(), x.AdminComment);
    }
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
