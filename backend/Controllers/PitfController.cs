using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/{languageCode:regex(^(sq|en)$)}/pitf")]
public sealed class PitfController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PitfPageDto>> Get(string languageCode, CancellationToken token)
    {
        var language = await db.Languages.SingleOrDefaultAsync(x => x.Code == languageCode, token);
        var fallback = await db.Languages.SingleOrDefaultAsync(x => x.IsDefault, token);
        if (language is null || fallback is null) return NotFound();

        var information = await db.TheatreInformation.AsNoTracking()
            .Include(x => x.PitfPageMediaAsset)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(token);
        if (information is null) return NotFound();
        var text = information.Translations.FirstOrDefault(x => x.LanguageId == language.Id)
            ?? information.Translations.First(x => x.LanguageId == fallback.Id);

        var editions = await db.PitfEditions.AsNoTracking()
            .OrderByDescending(x => x.EditionNumber).ThenByDescending(x => x.Year)
            .Select(x => new
            {
                x.Id, x.EditionNumber, x.Year, x.DestinationUrl,
                ImageUrl = x.CoverMediaAsset == null ? null : x.CoverMediaAsset.FileUrl,
                Requested = x.Translations.FirstOrDefault(t => t.LanguageId == language.Id),
                Fallback = x.Translations.FirstOrDefault(t => t.LanguageId == fallback.Id)
            }).ToListAsync(token);

        return Ok(new PitfPageDto(
            text.PitfPageTitle,
            text.PitfPageDescription,
            text.PitfPageButtonText,
            information.PitfPageButtonUrl,
            information.PitfPageMediaAsset?.FileUrl,
            editions.Where(x => x.Requested != null || x.Fallback != null)
                .Select(x => new PitfEditionDto(
                    x.Id, x.EditionNumber, x.Year, (x.Requested ?? x.Fallback)!.Title,
                    x.ImageUrl, x.DestinationUrl)).ToList()));
    }
}
