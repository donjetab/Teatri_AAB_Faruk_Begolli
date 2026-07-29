using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/{languageCode:regex(^(sq|en)$)}/performances")]
[Produces("application/json")]
public sealed class PerformancesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicPerformanceDto>>> GetUpcoming(string languageCode, CancellationToken token)
    {
        var requestedLanguageId = await db.Languages.Where(x => x.Code == languageCode && x.IsActive)
            .Select(x => (int?)x.Id).FirstOrDefaultAsync(token);
        var defaultLanguageId = await db.Languages.Where(x => x.IsDefault).Select(x => x.Id).FirstAsync(token);
        if (!requestedLanguageId.HasValue) return NotFound();

        var fallbackPhone = await db.TheatreInformation.AsNoTracking().Select(x => x.Phone).FirstOrDefaultAsync(token);
        var now = DateTimeOffset.UtcNow;
        var items = await db.ShowPerformances.AsNoTracking()
            .Where(x => (x.IsPublished || x.Status == PerformanceStatus.Postponed || x.Status == PerformanceStatus.Cancelled)
                && (x.StartDateTimeUtc >= now || x.Status == PerformanceStatus.Postponed || x.Status == PerformanceStatus.Cancelled)
                && x.Show.Status == ShowStatus.Published && x.Status != PerformanceStatus.Completed)
            .OrderBy(x => x.StartDateTimeUtc)
            .Select(x => new PublicPerformanceDto(
                x.Id, x.ShowId,
                x.Show.Translations.Where(t => t.LanguageId == requestedLanguageId.Value).Select(t => t.Title).FirstOrDefault()
                    ?? x.Show.Translations.Where(t => t.LanguageId == defaultLanguageId).Select(t => t.Title).FirstOrDefault() ?? $"Show {x.ShowId}",
                x.Show.Translations.Where(t => t.LanguageId == requestedLanguageId.Value).Select(t => t.Slug).FirstOrDefault()
                    ?? x.Show.Translations.Where(t => t.LanguageId == defaultLanguageId).Select(t => t.Slug).FirstOrDefault() ?? string.Empty,
                x.Show.PosterMediaAsset == null ? null : x.Show.PosterMediaAsset.FileUrl,
                x.StartDateTimeUtc,
                x.Location == null ? null
                    : x.Location.Translations.Where(t => t.LanguageId == requestedLanguageId.Value).Select(t => t.Name).FirstOrDefault()
                        ?? x.Location.Translations.Where(t => t.LanguageId == defaultLanguageId).Select(t => t.Name).FirstOrDefault(),
                x.Location == null ? null
                    : x.Location.Translations.Where(t => t.LanguageId == requestedLanguageId.Value).Select(t => t.Address).FirstOrDefault()
                        ?? x.Location.Translations.Where(t => t.LanguageId == defaultLanguageId).Select(t => t.Address).FirstOrDefault(),
                x.Hall, x.Status.ToString(), x.TicketUrl, x.ContactPhone ?? fallbackPhone))
            .ToListAsync(token);
        return Ok(items);
    }
}
