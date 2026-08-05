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

        var requestPath = context.Request.Path.Value ?? "/api/admin";
        var requestSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var requestResource = requestSegments.Length > 2 ? requestSegments[2] : "administration";
        var requestId = requestSegments.Skip(3).FirstOrDefault(segment => int.TryParse(segment, out _));
        string? affectedTitle = null;
        if (shouldAudit && requestResource == "media" && int.TryParse(requestId, out var mediaId))
            affectedTitle = await db.MediaAssets.AsNoTracking().Where(x => x.Id == mediaId).Select(x => x.FileName).FirstOrDefaultAsync(context.RequestAborted);

        await next(context);

        if (!shouldAudit || context.Response.StatusCode is < 200 or >= 300) return;

        // Detailed controller logs take priority. This fallback covers every admin
        // write endpoint that has not created its own activity entry.
        if (db.ChangeTracker.Entries<AdminActivity>().Any()) return;
        if (affectedTitle is null && requestResource == "media")
            affectedTitle = db.ChangeTracker.Entries<MediaAsset>().Select(entry => entry.Entity.FileName).FirstOrDefault();

        var actorId = int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        var path = requestPath;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resource = segments.Length > 2 ? segments[2] : "administration";
        var subresource = segments.Length > 3 && !int.TryParse(segments[3], out _) ? segments[3] : null;
        var entityType = EntityName(resource, subresource);
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
            Summary = affectedTitle is null
                ? $"{action} {entityType.ToLowerInvariant()}."
                : $"{action} {entityType.ToLowerInvariant()} ‘{affectedTitle}’.",
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(context.RequestAborted);
    }

    private static string Humanize(string value) => string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

    private static string EntityName(string resource, string? subresource) => (resource, subresource) switch
    {
        ("pages", _) => "Static Page",
        ("pitf", "editions") => "PITF Edition",
        ("pitf", _) => "PITF Page",
        ("media", _) => "Media File",
        ("gallery", _) => "General Gallery",
        ("navigation", _) => "Navigation and Footer",
        ("content", "homepage") => "Homepage",
        ("content", "website-information") => "Website Information",
        ("shows", _) => "Play",
        ("news", _) => "News Article",
        ("performances", _) => "Performance",
        ("messages", _) => "Contact Message",
        ("subscribers", _) => "Newsletter Subscriber",
        _ => Humanize(subresource ?? resource)
    };
}
