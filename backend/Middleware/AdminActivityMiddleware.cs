using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Middleware;

public sealed class AdminActivityMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> WriteMethods =
        [HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

    public async Task InvokeAsync(HttpContext context, AppDbContext db, IClock clock)
    {
        var shouldAudit = context.User.Identity?.IsAuthenticated == true
            && context.Request.Path.StartsWithSegments("/api/admin")
            && !context.Request.Path.StartsWithSegments("/api/admin/auth")
            && WriteMethods.Contains(context.Request.Method);

        await next(context);

        if (!shouldAudit || context.Response.StatusCode is < 200 or >= 300) return;

        // Detailed controller logs take priority. This fallback covers every admin
        // write endpoint that has not created its own activity entry.
        if (db.ChangeTracker.Entries<AdminActivity>().Any()) return;

        var actorId = int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        var path = context.Request.Path.Value ?? "/api/admin";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var entityType = segments.Length > 2 ? Humanize(segments[2]) : "Administration";
        var entityId = segments.Skip(3).FirstOrDefault(segment => int.TryParse(segment, out _));
        var action = context.Request.Method switch
        {
            "POST" => "Created or changed",
            "PUT" or "PATCH" => "Updated",
            "DELETE" => "Deleted or removed",
            _ => "Changed"
        };

        db.AdminActivities.Add(new AdminActivity
        {
            AdminUserId = actorId,
            AdminDisplayName = context.User.FindFirstValue(ClaimTypes.Name),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Summary = $"{context.Request.Method} {path}",
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(context.RequestAborted);
    }

    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
}
