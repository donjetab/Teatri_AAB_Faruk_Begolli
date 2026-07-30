using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/pitf")]
public sealed class AdminPitfController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminPitfPageDto>> Get(CancellationToken token)
    {
        var info = await db.TheatreInformation.AsNoTracking()
            .Include(x => x.PitfPageMediaAsset).Include(x => x.Translations).ThenInclude(x => x.Language)
            .FirstAsync(token);
        var editions = await db.PitfEditions.AsNoTracking()
            .Include(x => x.CoverMediaAsset).Include(x => x.Translations).ThenInclude(x => x.Language)
            .OrderByDescending(x => x.EditionNumber).ThenByDescending(x => x.Year).ToListAsync(token);
        return Ok(ToDto(info, editions));
    }

    [HttpPut]
    public async Task<ActionResult<AdminPitfPageDto>> SavePage(SaveAdminPitfPageRequest request, CancellationToken token)
    {
        if (!IsOptionalUrlValid(request.ButtonUrl))
            return ValidationProblem("Enter a complete http, https, or ftp button URL, or leave it empty.");
        var info = await db.TheatreInformation.Include(x => x.Translations).ThenInclude(x => x.Language)
            .FirstAsync(token);
        if (request.ImageMediaAssetId.HasValue &&
            !await db.MediaAssets.AnyAsync(x => x.Id == request.ImageMediaAssetId && x.IsActive && x.MimeType.StartsWith("image/"), token))
            return ValidationProblem("Choose an active PITF page image.");
        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        foreach (var incoming in request.Translations)
        {
            if (!languages.TryGetValue(incoming.LanguageCode, out var language)) continue;
            var translation = info.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (translation is null) continue;
            translation.PitfPageTitle = incoming.Title.Trim();
            translation.PitfPageDescription = incoming.Description.Trim();
            translation.PitfPageButtonText = incoming.ButtonText.Trim();
        }
        info.PitfPageMediaAssetId = request.ImageMediaAssetId;
        info.PitfPageButtonUrl = Clean(request.ButtonUrl);
        info.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token);
        return await Get(token);
    }

    [HttpPost("editions")]
    public async Task<ActionResult<AdminPitfPageDto>> CreateEdition(SaveAdminPitfEditionRequest request, CancellationToken token)
    {
        if (await db.PitfEditions.AnyAsync(x => x.Year == request.Year && x.EditionNumber == request.EditionNumber, token))
            return Conflict(new ProblemDetails { Title = "Edition already exists", Detail = "This PITF year and edition number already exist.", Status = 409 });
        var edition = new PitfEdition { CreatedAt = clock.UtcNow };
        var error = await ApplyEdition(edition, request, token);
        if (error is not null) return error;
        db.PitfEditions.Add(edition);
        await db.SaveChangesAsync(token);
        return await Get(token);
    }

    [HttpPut("editions/{id:int}")]
    public async Task<ActionResult<AdminPitfPageDto>> SaveEdition(int id, SaveAdminPitfEditionRequest request, CancellationToken token)
    {
        var edition = await db.PitfEditions.Include(x => x.Translations).ThenInclude(x => x.Language)
            .FirstOrDefaultAsync(x => x.Id == id, token);
        if (edition is null) return NotFound();
        if (await db.PitfEditions.AnyAsync(x => x.Id != id && x.Year == request.Year && x.EditionNumber == request.EditionNumber, token))
            return Conflict(new ProblemDetails { Title = "Edition already exists", Detail = "This PITF year and edition number already exist.", Status = 409 });
        var error = await ApplyEdition(edition, request, token);
        if (error is not null) return error;
        await db.SaveChangesAsync(token);
        return await Get(token);
    }

    [HttpDelete("editions/{id:int}")]
    public async Task<IActionResult> DeleteEdition(int id, CancellationToken token)
    {
        var edition = await db.PitfEditions.FindAsync([id], token);
        if (edition is null) return NotFound();
        db.PitfEditions.Remove(edition);
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    private async Task<ActionResult?> ApplyEdition(PitfEdition edition, SaveAdminPitfEditionRequest request, CancellationToken token)
    {
        if (!IsOptionalUrlValid(request.DestinationUrl))
            return ValidationProblem("Enter a complete http, https, or ftp destination URL, or leave it empty.");
        if (request.CoverMediaAssetId.HasValue &&
            !await db.MediaAssets.AnyAsync(x => x.Id == request.CoverMediaAssetId && x.IsActive && x.MimeType.StartsWith("image/"), token))
            return ValidationProblem("Choose an active edition image.");
        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        if (!request.Translations.Any(x => x.LanguageCode == "sq") || !request.Translations.Any(x => x.LanguageCode == "en"))
            return ValidationProblem("Albanian and English edition names are required.");
        foreach (var incoming in request.Translations)
        {
            if (!languages.TryGetValue(incoming.LanguageCode, out var language)) continue;
            var translation = edition.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (translation is null)
            {
                translation = new PitfEditionTranslation { LanguageId = language.Id, Language = language };
                edition.Translations.Add(translation);
            }
            translation.Title = incoming.Name.Trim();
            translation.Slug = $"pitf-{request.Year}-{request.EditionNumber}-{incoming.LanguageCode}";
            translation.ShortDescription = incoming.Name.Trim();
            translation.FullDescription = incoming.Name.Trim();
            translation.MetaTitle = incoming.Name.Trim();
            translation.MetaDescription = incoming.Name.Trim();
        }
        edition.EditionNumber = request.EditionNumber;
        edition.Year = request.Year;
        edition.CoverMediaAssetId = request.CoverMediaAssetId;
        edition.DestinationUrl = Clean(request.DestinationUrl);
        edition.IsPublished = true;
        edition.UpdatedAt = clock.UtcNow;
        return null;
    }

    private static AdminPitfPageDto ToDto(TheatreInformation info, IReadOnlyList<PitfEdition> editions) =>
        new(info.PitfPageMediaAssetId, info.PitfPageMediaAsset?.FileUrl, info.PitfPageButtonUrl,
            info.Translations.OrderBy(x => x.Language.Code).Select(x => new AdminPitfTranslationDto(
                x.Language.Code, x.PitfPageTitle, x.PitfPageDescription, x.PitfPageButtonText)).ToList(),
            editions.Select(x => new AdminPitfEditionDto(
                x.Id, x.EditionNumber, x.Year, x.CoverMediaAssetId, x.CoverMediaAsset?.FileUrl,
                x.DestinationUrl, x.IsPublished,
                x.Translations.OrderBy(t => t.Language.Code).Select(t =>
                    new AdminPitfEditionTranslationDto(t.Language.Code, t.Title)).ToList())).ToList());
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsOptionalUrlValid(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFtp));
}
