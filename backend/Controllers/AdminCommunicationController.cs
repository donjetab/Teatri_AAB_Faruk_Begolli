using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin")]
public sealed class AdminCommunicationController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet("messages")]
    public async Task<ActionResult<AdminContactMessageListDto>> Messages([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ContactMessages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.Name.Contains(term) || x.Email.Contains(term) || x.Subject.Contains(term)); }
        if (Enum.TryParse<ContactMessageStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        var total = await query.CountAsync(token);
        var unread = await db.ContactMessages.CountAsync(x => x.Status == ContactMessageStatus.New, token);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminContactMessageDto(x.Id, x.Name, x.Email, x.Subject, x.Message, x.LanguageCode,
                x.Status.ToString(), x.InternalNotes, x.CreatedAt, x.UpdatedAt)).ToListAsync(token);
        return Ok(new AdminContactMessageListDto(items, page, pageSize, total, unread));
    }

    [HttpGet("messages/{id:int}")]
    public async Task<ActionResult<AdminContactMessageDto>> Message(int id, CancellationToken token)
    {
        var item = await db.ContactMessages.AsNoTracking().Where(x => x.Id == id).Select(x => new AdminContactMessageDto(
            x.Id, x.Name, x.Email, x.Subject, x.Message, x.LanguageCode, x.Status.ToString(), x.InternalNotes, x.CreatedAt, x.UpdatedAt)).FirstOrDefaultAsync(token);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("messages/{id:int}")]
    public async Task<ActionResult<AdminContactMessageDto>> UpdateMessage(int id, UpdateContactMessageRequest request, CancellationToken token)
    {
        var item = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        if (!Enum.TryParse<ContactMessageStatus>(request.Status, true, out var status)) return ValidationProblem("Invalid message status.");
        item.Status = status; item.IsRead = status != ContactMessageStatus.New; item.InternalNotes = Clean(request.InternalNotes); item.UpdatedAt = clock.UtcNow;
        Activity("Updated", "ContactMessage", id.ToString(), $"Changed message status to {status}");
        await db.SaveChangesAsync(token); return await Message(id, token);
    }

    [HttpDelete("messages/{id:int}")]
    public async Task<IActionResult> DeleteSpam(int id, CancellationToken token)
    {
        var item = await db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        if (item.Status != ContactMessageStatus.Spam) return Conflict(new ProblemDetails { Title = "Mark as spam first", Detail = "Only messages marked as spam can be permanently deleted.", Status = 409 });
        db.ContactMessages.Remove(item); Activity("Deleted spam", "ContactMessage", id.ToString(), "Deleted spam contact submission");
        await db.SaveChangesAsync(token); return NoContent();
    }

    [HttpGet("subscribers")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<AdminSubscriberListDto>> Subscribers([FromQuery] string? search, [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken token = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.NewsletterSubscribers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Email.Contains(search.Trim()));
        if (active.HasValue) query = query.Where(x => x.IsActive == active);
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.SubscribedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminSubscriberDto(x.Id, x.Email, x.PreferredLanguageCode, x.IsActive, x.SubscribedAt, x.UnsubscribedAt, x.Source)).ToListAsync(token);
        return Ok(new AdminSubscriberListDto(items, page, pageSize, total));
    }

    [HttpGet("subscribers/export")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> ExportSubscribers(CancellationToken token)
    {
        var items = await db.NewsletterSubscribers.AsNoTracking().OrderBy(x => x.Email).ToListAsync(token);
        var csv = new StringBuilder("Email,Status,Language,Subscribed,Unsubscribed,Source\r\n");
        foreach (var x in items) csv.AppendLine(string.Join(",", Csv(x.Email), x.IsActive ? "Active" : "Unsubscribed", Csv(x.PreferredLanguageCode), x.SubscribedAt.ToString("O"), x.UnsubscribedAt?.ToString("O") ?? "", Csv(x.Source)));
        Activity("Exported", "NewsletterSubscribers", null, $"Exported {items.Count} subscriber records");
        await db.SaveChangesAsync(token);
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", $"theatre-subscribers-{clock.UtcNow:yyyy-MM-dd}.csv");
    }

    [HttpDelete("subscribers/{id:int}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> DeleteSubscriber(int id, CancellationToken token)
    {
        var item = await db.NewsletterSubscribers.FirstOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        db.NewsletterSubscribers.Remove(item); Activity("Deleted", "NewsletterSubscriber", id.ToString(), "Deleted subscriber data");
        await db.SaveChangesAsync(token); return NoContent();
    }

    private void Activity(string action, string type, string? id, string summary) => db.AdminActivities.Add(new AdminActivity {
        AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
        Action = action, EntityType = type, EntityId = id, Summary = summary, CreatedAt = clock.UtcNow });
    private static string Csv(string? value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\"";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
