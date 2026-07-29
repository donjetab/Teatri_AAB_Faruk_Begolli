using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/performances")]
public sealed class AdminPerformancesController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet("venues")]
    public async Task<ActionResult<IReadOnlyList<AdminVenueDto>>> GetVenues(CancellationToken token)
    {
        var venues = await db.Locations.AsNoTracking().OrderBy(x => x.Id).Select(x => new AdminVenueDto(
            x.Id,
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault() ?? "",
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Name).FirstOrDefault() ?? "",
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Address).FirstOrDefault() ?? "",
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Address).FirstOrDefault() ?? "",
            x.IsActive,
            x.Performances.Count)).ToListAsync(token);
        return Ok(venues);
    }

    [HttpPut("venues/{id:int}")]
    public async Task<ActionResult<AdminVenueDto>> UpdateVenue(int id, CreateAdminVenueRequest request, CancellationToken token)
    {
        var venue = await db.Locations.Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.Performances).FirstOrDefaultAsync(x => x.Id == id, token);
        if (venue is null) return NotFound();
        if (await db.LocationTranslations.AnyAsync(x => x.LocationId != id && x.Name == request.NameSq.Trim(), token))
            return Conflict(new ProblemDetails { Title = "Venue already exists", Detail = "Another venue already uses this Albanian name.", Status = 409 });
        foreach (var translation in venue.Translations)
        {
            translation.Name = translation.Language.Code == "sq" ? request.NameSq.Trim() : request.NameEn.Trim();
            translation.Address = translation.Language.Code == "sq" ? request.AddressSq.Trim() : request.AddressEn.Trim();
        }
        await db.SaveChangesAsync(token);
        return Ok(new AdminVenueDto(venue.Id, request.NameSq.Trim(), request.NameEn.Trim(), request.AddressSq.Trim(), request.AddressEn.Trim(), venue.IsActive, venue.Performances.Count));
    }

    [HttpDelete("venues/{id:int}")]
    public async Task<IActionResult> DeleteVenue(int id, CancellationToken token)
    {
        var venue = await db.Locations.Include(x => x.Performances).FirstOrDefaultAsync(x => x.Id == id, token);
        if (venue is null) return NotFound();
        if (venue.Performances.Count > 0)
            return Conflict(new ProblemDetails { Title = "Venue is in use", Detail = "This venue cannot be deleted while performances reference it. You can still edit its name and address.", Status = 409 });
        db.Locations.Remove(venue);
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpGet("conflicts")]
    public async Task<ActionResult<IReadOnlyList<AdminPerformanceConflictDto>>> Conflicts(
        [FromQuery] DateTimeOffset startDateTimeUtc,
        [FromQuery] int? locationId,
        [FromQuery] string? customVenue,
        [FromQuery] int? excludeId,
        CancellationToken token)
    {
        if (!locationId.HasValue && string.IsNullOrWhiteSpace(customVenue)) return Ok(Array.Empty<AdminPerformanceConflictDto>());
        var from = startDateTimeUtc.ToUniversalTime().AddMinutes(-30);
        var to = startDateTimeUtc.ToUniversalTime().AddMinutes(30);
        var normalizedVenue = Clean(customVenue);
        var query = db.ShowPerformances.AsNoTracking()
            .Where(x => x.StartDateTimeUtc >= from && x.StartDateTimeUtc <= to
                && x.Status != PerformanceStatus.Cancelled && x.Status != PerformanceStatus.Completed);
        if (excludeId.HasValue) query = query.Where(x => x.Id != excludeId.Value);
        query = locationId.HasValue
            ? query.Where(x => x.LocationId == locationId.Value)
            : query.Where(x => x.LocationId == null && x.Hall != null && x.Hall == normalizedVenue);
        var items = await query.OrderBy(x => x.StartDateTimeUtc)
            .Select(x => new AdminPerformanceConflictDto(
                x.Id,
                x.Show.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? $"Show {x.ShowId}",
                x.StartDateTimeUtc,
                x.Location == null ? null : x.Location.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault(),
                x.Hall))
            .ToListAsync(token);
        return Ok(items);
    }

    [HttpPost("venues")]
    public async Task<ActionResult<AdminLookupDto>> CreateVenue(CreateAdminVenueRequest request, CancellationToken token)
    {
        var name = request.NameSq.Trim();
        if (await db.LocationTranslations.AnyAsync(x => x.Name == name, token))
            return Conflict(new ProblemDetails { Title = "Venue already exists", Detail = "Choose the existing venue from the dropdown.", Status = 409 });

        var languages = await db.Languages.Where(x => x.IsActive && (x.Code == "sq" || x.Code == "en")).ToListAsync(token);
        if (languages.Count != 2) return Problem("The Albanian and English languages must be configured before adding a venue.");

        var venue = new Location { IsActive = true };
        foreach (var language in languages)
            venue.Translations.Add(new LocationTranslation
            {
                LanguageId = language.Id,
                Name = language.Code == "sq" ? request.NameSq.Trim() : request.NameEn.Trim(),
                Address = language.Code == "sq" ? request.AddressSq.Trim() : request.AddressEn.Trim()
            });
        db.Locations.Add(venue);
        db.AdminActivities.Add(new AdminActivity
        {
            AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
            Action = "Created",
            EntityType = "Venue",
            Summary = $"Created venue {name}",
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(token);
        return CreatedAtAction(nameof(List), new AdminLookupDto(venue.Id, name));
    }

    [HttpGet]
    public async Task<ActionResult<AdminPerformanceListDto>> List(
        [FromQuery] int? showId, [FromQuery] int? locationId, [FromQuery] string? status,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ShowPerformances.AsNoTracking().AsQueryable();
        if (showId.HasValue) query = query.Where(x => x.ShowId == showId);
        if (locationId.HasValue) query = query.Where(x => x.LocationId == locationId);
        if (Enum.TryParse<PerformanceStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        if (from.HasValue) query = query.Where(x => x.StartDateTimeUtc >= from);
        if (to.HasValue) query = query.Where(x => x.StartDateTimeUtc <= to);
        var total = await query.CountAsync(token);
        var entities = await query.Include(x => x.Show).ThenInclude(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.Location).ThenInclude(x => x!.Translations).ThenInclude(x => x.Language)
            .OrderBy(x => x.StartDateTimeUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        var items = entities.Select(ToDto).ToList();
        var shows = await db.Shows.AsNoTracking().Where(x => x.Status != ShowStatus.Archived).OrderByDescending(x => x.UpdatedAt)
            .Select(x => new AdminLookupDto(x.Id, x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? $"Show {x.Id}")).ToListAsync(token);
        var locations = await db.Locations.AsNoTracking().Where(x => x.IsActive).Select(x => new AdminLookupDto(x.Id,
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault() ?? $"Venue {x.Id}")).ToListAsync(token);
        return Ok(new AdminPerformanceListDto(items, page, pageSize, total, shows, locations));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminPerformanceDto>> Get(int id, CancellationToken token)
    {
        var entity = await db.ShowPerformances.AsNoTracking().Include(x => x.Show).ThenInclude(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.Location).ThenInclude(x => x!.Translations).ThenInclude(x => x.Language).FirstOrDefaultAsync(x => x.Id == id, token);
        var item = entity is null ? null : ToDto(entity);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AdminPerformanceDto>> Create(SaveAdminPerformanceRequest request, CancellationToken token)
    {
        var error = await Validate(request, allowPastStart: false, token: token); if (error is not null) return BadRequest(error);
        var now = clock.UtcNow;
        var entity = new ShowPerformance { CreatedAt = now, UpdatedAt = now };
        Apply(entity, request); db.ShowPerformances.Add(entity);
        AddActivity("Created", entity, "Created performance");
        await db.SaveChangesAsync(token);
        return await Get(entity.Id, token);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminPerformanceDto>> Update(int id, SaveAdminPerformanceRequest request, CancellationToken token)
    {
        var entity = await db.ShowPerformances.FirstOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return NotFound();
        var error = await Validate(request, allowPastStart: true, token: token); if (error is not null) return BadRequest(error);
        Apply(entity, request); entity.UpdatedAt = clock.UtcNow;
        AddActivity("Updated", entity, "Updated performance");
        await db.SaveChangesAsync(token);
        return await Get(entity.Id, token);
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<AdminPerformanceDto>> Duplicate(int id, CancellationToken token)
    {
        var source = await db.ShowPerformances.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, token);
        if (source is null) return NotFound();
        var now = clock.UtcNow;
        var copy = new ShowPerformance { ShowId = source.ShowId, LocationId = source.LocationId, Hall = source.Hall,
            StartDateTimeUtc = source.StartDateTimeUtc.AddDays(7), EndDateTimeUtc = source.EndDateTimeUtc?.AddDays(7),
            TicketUrl = source.TicketUrl, ContactPhone = source.ContactPhone, Status = PerformanceStatus.Scheduled,
            IsPublished = false, InternalNotes = source.InternalNotes, CreatedAt = now, UpdatedAt = now };
        db.ShowPerformances.Add(copy); AddActivity("Duplicated", copy, "Duplicated performance");
        await db.SaveChangesAsync(token); return await Get(copy.Id, token);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var entity = await db.ShowPerformances.FirstOrDefaultAsync(x => x.Id == id, token);
        if (entity is null) return NotFound();
        db.ShowPerformances.Remove(entity); AddActivity("Deleted", entity, "Deleted draft performance");
        await db.SaveChangesAsync(token); return NoContent();
    }

    private async Task<ValidationProblemDetails?> Validate(SaveAdminPerformanceRequest request, bool allowPastStart, CancellationToken token)
    {
        var errors = new Dictionary<string, string[]>();
        if (!await db.Shows.AnyAsync(x => x.Id == request.ShowId, token)) errors["ShowId"] = ["The selected play does not exist."];
        if (!allowPastStart && request.StartDateTimeUtc < clock.UtcNow)
            errors["StartDateTimeUtc"] = ["A new performance cannot be scheduled in the past."];
        if (request.LocationId.HasValue && !await db.Locations.AnyAsync(x => x.Id == request.LocationId, token)) errors["LocationId"] = ["The selected venue does not exist."];
        if (!Enum.TryParse<PerformanceStatus>(request.Status, true, out _)) errors["Status"] = ["Invalid performance status."];
        if (!string.Equals(request.Status, nameof(PerformanceStatus.SoldOut), StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.TicketUrl)
            && string.IsNullOrWhiteSpace(request.ContactPhone))
            errors["BookingContact"] = ["Add a ticket URL or contact phone unless the performance is sold out."];
        if (request.EndDateTimeUtc.HasValue && request.EndDateTimeUtc <= request.StartDateTimeUtc) errors["EndDateTimeUtc"] = ["End time must be after start time."];
        return errors.Count == 0 ? null : new ValidationProblemDetails(errors);
    }

    private static void Apply(ShowPerformance entity, SaveAdminPerformanceRequest request)
    {
        entity.ShowId = request.ShowId; entity.LocationId = request.LocationId; entity.Hall = Clean(request.Hall);
        entity.StartDateTimeUtc = request.StartDateTimeUtc.ToUniversalTime(); entity.EndDateTimeUtc = request.EndDateTimeUtc?.ToUniversalTime();
        entity.TicketUrl = Clean(request.TicketUrl); entity.ContactPhone = Clean(request.ContactPhone);
        entity.Status = Enum.Parse<PerformanceStatus>(request.Status, true); entity.IsPublished = request.IsPublished;
        entity.InternalNotes = Clean(request.InternalNotes);
    }
    private void AddActivity(string action, ShowPerformance entity, string summary) => db.AdminActivities.Add(new AdminActivity {
        AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
        Action = action, EntityType = "Performance", EntityId = entity.Id == 0 ? null : entity.Id.ToString(), Summary = summary, CreatedAt = clock.UtcNow });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AdminPerformanceDto ToDto(ShowPerformance x) => new(x.Id, x.ShowId,
        x.Show.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? $"Show {x.ShowId}",
        x.LocationId, x.Location == null ? null : x.Location.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault(),
        x.Hall, x.StartDateTimeUtc, x.EndDateTimeUtc, x.TicketUrl, x.ContactPhone, x.Status.ToString(), x.IsPublished,
        x.InternalNotes, x.CreatedAt, x.UpdatedAt);
}
