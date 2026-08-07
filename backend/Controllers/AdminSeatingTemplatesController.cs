using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin"), Route("api/admin/seating-templates")]
public sealed class AdminSeatingTemplatesController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SeatingTemplateDto>>> List(CancellationToken token) => Ok((await Query().ToListAsync(token)).Select(Map));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SeatingTemplateDto>> Get(int id, CancellationToken token)
    { var entity = await Query().SingleOrDefaultAsync(x => x.Id == id, token); return entity is null ? NotFound() : Ok(Map(entity)); }

    [HttpPost]
    [Authorize(Policy = "ManageTheatreSchemas")]
    public async Task<ActionResult<SeatingTemplateDto>> Create(SaveSeatingTemplateRequest request, CancellationToken token)
    {
        if (!await db.Locations.AnyAsync(x => x.Id == request.LocationId, token)) return BadRequest(new ProblemDetails { Title = "Venue not found" });
        var entity = new SeatingTemplate { CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow };
        Apply(entity, request); db.SeatingTemplates.Add(entity); await db.SaveChangesAsync(token); Audit("TheatreTemplateCreated", entity.Id, null, new { request.Name, request.LocationId, SeatCount = request.Sections.Sum(s => s.Rows.Sum(r => r.Seats.Length)) }); await db.SaveChangesAsync(token);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, await Load(entity.Id, token));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManageTheatreSchemas")]
    public async Task<ActionResult<SeatingTemplateDto>> Update(int id, SaveSeatingTemplateRequest request, CancellationToken token)
    {
        var entity = await db.SeatingTemplates.Include(x => x.Sections).ThenInclude(x => x.Rows).ThenInclude(x => x.Seats).SingleOrDefaultAsync(x => x.Id == id, token); if (entity is null) return NotFound();
        var previous = new { entity.Name, SeatCount = entity.Sections.Sum(s => s.Rows.Sum(r => r.Seats.Count)) }; db.SeatingTemplateSections.RemoveRange(entity.Sections); entity.Sections.Clear(); Apply(entity, request); entity.UpdatedAt = clock.UtcNow; Audit("TheatreTemplateEdited", id, previous, new { request.Name, SeatCount = request.Sections.Sum(s => s.Rows.Sum(r => r.Seats.Length)) }); await db.SaveChangesAsync(token);
        return Ok(await Load(id, token));
    }

    [HttpPost("{id:int}/reset")]
    [Authorize(Policy = "ManageTheatreSchemas")]
    public async Task<ActionResult<SeatingTemplateDto>> Reset(int id, CancellationToken token)
    {
        var entity = await db.SeatingTemplates.Include(x => x.Location).ThenInclude(x => x.Translations).Include(x => x.Sections).ThenInclude(x => x.Rows).ThenInclude(x => x.Seats).SingleOrDefaultAsync(x => x.Id == id, token); if (entity is null) return NotFound();
        var venue = entity.Location.Translations.FirstOrDefault(x => x.LanguageId == 1)?.Name ?? entity.Location.Translations.FirstOrDefault()?.Name ?? "";
        var key = DefaultSeatingLayouts.Identify(venue, entity.Name) ?? throw new ValidationException("This template has no saved theatre default.");
        var canonical = DefaultSeatingLayouts.Create(key, entity.LocationId, clock.UtcNow);
        db.SeatingTemplateSections.RemoveRange(entity.Sections); entity.Sections.Clear(); Copy(entity, canonical); entity.UpdatedAt = clock.UtcNow; Audit("TheatreTemplateReset", id, null, new { Default = key.ToString() }); await db.SaveChangesAsync(token);
        return Ok(await Load(id, token));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManageTheatreSchemas")]
    public async Task<IActionResult> Delete(int id, CancellationToken token) { var entity = await db.SeatingTemplates.FindAsync([id], token); if (entity is null) return NotFound(); if (await db.ShowPerformances.AnyAsync(x => x.SeatingTemplateId == id, token)) { entity.IsActive = false; entity.UpdatedAt = clock.UtcNow; } else db.SeatingTemplates.Remove(entity); Audit("TheatreTemplateRemoved", id, new { entity.Name, entity.IsActive }, null); await db.SaveChangesAsync(token); return NoContent(); }

    private IQueryable<SeatingTemplate> Query() => db.SeatingTemplates.AsNoTracking().Include(x => x.Location).ThenInclude(x => x.Translations).Include(x => x.Sections).ThenInclude(x => x.Rows).ThenInclude(x => x.Seats).OrderBy(x => x.LocationId).ThenBy(x => x.Name);
    private async Task<SeatingTemplateDto> Load(int id, CancellationToken token) => Map(await Query().SingleAsync(x => x.Id == id, token));
    private static void Apply(SeatingTemplate entity, SaveSeatingTemplateRequest request)
    {
        entity.LocationId = request.LocationId; entity.Name = request.Name.Trim(); entity.IsDefault = request.IsDefault; entity.IsActive = request.IsActive; entity.CanvasWidth = request.CanvasWidth; entity.CanvasHeight = request.CanvasHeight; entity.StageLabel = Clean(request.StageLabel); entity.StageX = request.StageX; entity.StageY = request.StageY; entity.StageWidth = request.StageWidth; entity.StageHeight = request.StageHeight;
        foreach (var section in request.Sections.OrderBy(x => x.DisplayOrder)) { var s = new SeatingTemplateSection { Name = section.Name.Trim(), DisplayOrder = section.DisplayOrder }; foreach (var row in section.Rows.OrderBy(x => x.DisplayOrder)) { var r = new SeatingTemplateRow { Label = row.Label.Trim(), DisplayOrder = row.DisplayOrder }; foreach (var seat in row.Seats.OrderBy(x => x.DisplayOrder)) r.Seats.Add(new SeatingTemplateSeat { Label = seat.Label.Trim(), DisplayOrder = seat.DisplayOrder, PositionX = seat.X, PositionY = seat.Y, Rotation = seat.Rotation, IsActive = seat.IsActive }); s.Rows.Add(r); } entity.Sections.Add(s); }
    }
    private static void Copy(SeatingTemplate target, SeatingTemplate source) { target.Name = source.Name; target.IsDefault = true; target.IsActive = true; target.CanvasWidth = source.CanvasWidth; target.CanvasHeight = source.CanvasHeight; target.StageLabel = source.StageLabel; target.StageX = source.StageX; target.StageY = source.StageY; target.StageWidth = source.StageWidth; target.StageHeight = source.StageHeight; foreach (var section in source.Sections) target.Sections.Add(section); }
    private static SeatingTemplateDto Map(SeatingTemplate x) => new(x.Id, x.LocationId, x.Location.Translations.FirstOrDefault(t => t.LanguageId == 1)?.Name ?? x.Location.Translations.FirstOrDefault()?.Name ?? $"Venue {x.LocationId}", x.Name, x.IsDefault, x.IsActive, x.CanvasWidth, x.CanvasHeight, x.StageLabel, x.StageX, x.StageY, x.StageWidth, x.StageHeight, x.Sections.OrderBy(s => s.DisplayOrder).Select(s => new SaveTemplateSectionRequest(s.Name, s.DisplayOrder, s.Rows.OrderBy(r => r.DisplayOrder).Select(r => new SaveTemplateRowRequest(r.Label, r.DisplayOrder, r.Seats.OrderBy(a => a.DisplayOrder).Select(a => new SaveTemplateSeatRequest(a.Id, a.Label, a.DisplayOrder, a.PositionX, a.PositionY, a.Rotation, a.IsActive)).ToArray())).ToArray())).ToList());
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void Audit(string action, int id, object? previous, object? current) => db.ReservationAdminAudits.Add(new ReservationAdminAudit { AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId) ? adminId : null, ActionType = action, EntityType = "SeatingTemplate", EntityId = id.ToString(), PreviousValuesJson = previous is null ? null : JsonSerializer.Serialize(previous), NewValuesJson = current is null ? null : JsonSerializer.Serialize(current), CreatedAt = clock.UtcNow });
}
