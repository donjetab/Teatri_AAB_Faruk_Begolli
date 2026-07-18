using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/{languageCode:regex(^(sq|en)$)}/shows")]
[Produces("application/json")]
public sealed class ShowsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShowListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ShowListItemDto>>> Get(
        string languageCode,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var languageIds = await db.Languages
            .Where(x => x.Code == languageCode || x.IsDefault)
            .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);

        if (!languageIds.TryGetValue(languageCode, out var requestedLanguageId))
        {
            return NotFound();
        }

        var defaultLanguageId = await db.Languages
            .Where(x => x.IsDefault)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);

        var shows = await db.Shows
            .AsNoTracking()
            .Where(x => x.Status == ShowStatus.Published)
            .Select(x => new
            {
                x.Id,
                x.PremiereDate,
                PosterUrl = x.PosterMediaAsset == null ? null : x.PosterMediaAsset.FileUrl,
                Requested = x.Translations.FirstOrDefault(t => t.LanguageId == requestedLanguageId),
                Fallback = x.Translations.FirstOrDefault(t => t.LanguageId == defaultLanguageId),
                Director = x.Credits
                    .Where(c => c.CreditType.Code == "director")
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => c.Person.FullName)
                    .FirstOrDefault(),
                NextPerformance = x.Performances
                    .Where(p => p.Status == PerformanceStatus.Scheduled && p.StartDateTimeUtc > now)
                    .OrderBy(p => p.StartDateTimeUtc)
                    .Select(p => new
                    {
                        p.StartDateTimeUtc,
                        p.TicketUrl
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return Ok(shows
            .Where(x => x.Requested != null || x.Fallback != null)
            .OrderByDescending(x => x.PremiereDate)
            .Select(x =>
            {
                var translation = x.Requested ?? x.Fallback!;
                return new ShowListItemDto(
                    x.Id,
                    translation.Title,
                    translation.Slug.EndsWith("-en", StringComparison.Ordinal)
                        ? translation.Slug[..^3]
                        : translation.Slug,
                    translation.FullDescription,
                    x.PosterUrl,
                    x.Director,
                    x.PremiereDate,
                    x.NextPerformance == null ? null : x.NextPerformance.StartDateTimeUtc,
                    x.NextPerformance == null ? null : x.NextPerformance.TicketUrl);
            })
            .ToList());
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ShowDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShowDetailDto>> GetBySlug(
        string languageCode,
        string slug,
        CancellationToken cancellationToken)
    {
        var languages = await db.Languages
            .Where(x => x.Code == languageCode || x.IsDefault)
            .ToListAsync(cancellationToken);
        var requestedLanguage = languages.FirstOrDefault(x => x.Code == languageCode);
        var fallbackLanguage = languages.FirstOrDefault(x => x.IsDefault);

        if (requestedLanguage is null || fallbackLanguage is null)
        {
            return NotFound();
        }

        var show = await db.Shows
            .AsNoTracking()
            .Include(x => x.PosterMediaAsset)
            .Include(x => x.Translations)
            .Include(x => x.Credits)
                .ThenInclude(x => x.Person)
            .Include(x => x.Credits)
                .ThenInclude(x => x.CreditType)
                    .ThenInclude(x => x.Translations)
            .Include(x => x.Performances)
            .FirstOrDefaultAsync(x =>
                x.Status == ShowStatus.Published &&
                x.Translations.Any(t => t.Slug == slug || t.Slug == slug + "-en"),
                cancellationToken);

        if (show is null)
        {
            return NotFound();
        }

        var translation = show.Translations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id)
            ?? show.Translations.FirstOrDefault(x => x.LanguageId == fallbackLanguage.Id);
        if (translation is null)
        {
            return NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var nextPerformance = show.Performances
            .Where(x => x.Status == PerformanceStatus.Scheduled && x.StartDateTimeUtc > now)
            .OrderBy(x => x.StartDateTimeUtc)
            .FirstOrDefault();
        var credits = show.Credits
            .OrderBy(x => x.DisplayOrder)
            .GroupBy(x => new
            {
                x.CreditTypeId,
                x.CreditType.Code,
                Name = x.CreditType.Translations
                    .FirstOrDefault(t => t.LanguageId == requestedLanguage.Id)?.Name
                    ?? x.CreditType.Translations
                        .FirstOrDefault(t => t.LanguageId == fallbackLanguage.Id)!.Name
            })
            .Select(x => new ShowCreditDto(
                x.Key.Code,
                x.Key.Name,
                x.OrderBy(c => c.DisplayOrder).Select(c => c.Person.FullName).ToList()))
            .ToList();

        return Ok(new ShowDetailDto(
            show.Id,
            translation.Title,
            translation.Slug.EndsWith("-en", StringComparison.Ordinal)
                ? translation.Slug[..^3]
                : translation.Slug,
            translation.FullDescription,
            show.PosterMediaAsset?.FileUrl,
            show.PremiereDate,
            credits,
            nextPerformance?.StartDateTimeUtc,
            nextPerformance?.TicketUrl));
    }
}
