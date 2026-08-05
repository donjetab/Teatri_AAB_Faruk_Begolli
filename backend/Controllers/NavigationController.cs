using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Route("api/navigation")]
public sealed class NavigationController(AppDbContext db) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<NavigationConfigurationDto>> Get(CancellationToken token)
    {
        var json = await db.TheatreInformation.AsNoTracking().Select(x => x.NavigationConfigurationJson).FirstOrDefaultAsync(token);
        return Ok(NavigationConfiguration.Read(json));
    }
}

[ApiController, Authorize(Policy = "Admin"), Route("api/admin/navigation")]
public sealed class AdminNavigationController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NavigationConfigurationDto>> Get(CancellationToken token)
    {
        var json = await db.TheatreInformation.AsNoTracking().Select(x => x.NavigationConfigurationJson).FirstOrDefaultAsync(token);
        return Ok(NavigationConfiguration.Read(json));
    }

    [HttpPut]
    public async Task<ActionResult<NavigationConfigurationDto>> Update(NavigationConfigurationDto request, CancellationToken token)
    {
        var keys = request.Items.Select(x => x.RouteKey).ToArray();
        if (keys.Length != NavigationConfiguration.RouteKeys.Length || keys.Distinct().Count() != keys.Length || keys.Any(x => !NavigationConfiguration.RouteKeys.Contains(x)))
            return ValidationProblem("The navigation must contain each supported website page exactly once.");
        if (request.Translations.Count != 2 || request.Translations.Any(x => x.LanguageCode is not ("sq" or "en")) || request.Translations.Any(x => NavigationConfiguration.RouteKeys.Any(key => !x.Labels.TryGetValue(key, out var label) || string.IsNullOrWhiteSpace(label))))
            return ValidationProblem("Complete Albanian and English labels are required for every navigation item.");

        var normalized = request with {
            Items = request.Items.OrderBy(x => x.SortOrder).Select((x, index) => x with { SortOrder = index }).ToList(),
            Translations = request.Translations.Select(x => x with { Labels = x.Labels.ToDictionary(p => p.Key, p => p.Value.Trim()) }).ToList()
        };
        var info = await db.TheatreInformation.FirstOrDefaultAsync(token);
        if (info is null) return NotFound();
        info.NavigationConfigurationJson = NavigationConfiguration.Write(normalized);
        info.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token);
        return Ok(normalized);
    }
}
