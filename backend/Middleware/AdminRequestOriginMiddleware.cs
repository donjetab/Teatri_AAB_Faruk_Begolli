using Microsoft.AspNetCore.Mvc;

namespace Theatre.Api.Middleware;

public sealed class AdminRequestOriginMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/admin") || SafeMethods.Contains(context.Request.Method)
            || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var allowed = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(context.Request.Headers.Referer.FirstOrDefault(), UriKind.Absolute, out var referer))
            origin = referer.GetLeftPart(UriPartial.Authority);

        if (string.IsNullOrWhiteSpace(origin) || !allowed.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 403,
                Title = "Request rejected",
                Detail = "The request did not come from an approved administrator website. Refresh the admin panel and try again."
            });
            return;
        }
        await next(context);
    }
}
