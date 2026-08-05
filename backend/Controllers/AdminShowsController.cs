using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/shows")]
public sealed class AdminShowsController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet("credit-types")]
    public async Task<ActionResult<IReadOnlyList<AdminCreditTypeDto>>> GetCreditTypes(CancellationToken token)
    {
        var types = await db.CreditTypes.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => new AdminCreditTypeDto(
            x.Id, x.Code,
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault() ?? x.Code,
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Name).FirstOrDefault() ?? x.Code)).ToListAsync(token);
        return Ok(types);
    }

    [HttpGet]
    public async Task<ActionResult<AdminShowListResponseDto>> List(
        [FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] string? status,
        [FromQuery] string? lifecycleStatus, [FromQuery] int? year, [FromQuery] bool? featured,
        [FromQuery] string sort = "production", [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken token = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Shows.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Translations.Any(t => t.Title.Contains(term)));
        }
        if (categoryId.HasValue) query = query.Where(x => x.ShowCategoryId == categoryId);
        if (Enum.TryParse<ShowStatus>(status, true, out var parsedStatus)) query = query.Where(x => x.Status == parsedStatus);
        if (Enum.TryParse<ShowLifecycleStatus>(lifecycleStatus, true, out var parsedLifecycle)) query = query.Where(x => x.LifecycleStatus == parsedLifecycle);
        if (year.HasValue) query = query.Where(x => x.ProductionYear == year);
        if (featured.HasValue) query = query.Where(x => x.IsFeatured == featured);
        query = sort.ToLowerInvariant() switch
        {
            "title" => query.OrderBy(x => x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault()),
            "premiere" => query.OrderByDescending(x => x.PremiereDate),
            "production" => query.OrderByDescending(x => x.IsFeatured)
                .ThenByDescending(x => x.ProductionYear)
                .ThenBy(x => x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault()),
            _ => query.OrderByDescending(x => x.UpdatedAt)
        };
        var total = await query.CountAsync(token);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminShowListItemDto(
                x.Id,
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Title).FirstOrDefault() ?? "Pa titull",
                x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Title).FirstOrDefault() ?? "Untitled",
                x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Slug).FirstOrDefault() ?? "",
                x.ShowCategory.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault() ?? "—",
                x.Status.ToString(), x.LifecycleStatus.ToString(), x.ProductionYear, x.PremiereDate, x.IsFeatured,
                x.Performances.Count, x.UpdatedAt, x.PosterMediaAsset == null ? null : x.PosterMediaAsset.FileUrl))
            .ToListAsync(token);
        var categories = await db.ShowCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder)
            .Select(x => new AdminLookupDto(x.Id, x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault() ?? $"Category {x.Id}"))
            .ToListAsync(token);
        var years = await db.Shows.AsNoTracking().Where(x => x.ProductionYear.HasValue).Select(x => x.ProductionYear!.Value).Distinct().OrderByDescending(x => x).ToListAsync(token);
        return Ok(new AdminShowListResponseDto(items, page, pageSize, total, categories, years));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminShowDetailDto>> Get(int id, CancellationToken token)
    {
        var show = await LoadAsync(id, token);
        return show is null ? NotFound() : Ok(ToDetail(show));
    }

    [HttpGet("{id:int}/credits")]
    public async Task<ActionResult<AdminShowCreditsResponseDto>> GetCredits(int id, CancellationToken token)
    {
        if (!await db.Shows.AnyAsync(x => x.Id == id, token)) return NotFound();
        var credits = await db.ShowCredits.AsNoTracking().Where(x => x.ShowId == id).OrderBy(x => x.DisplayOrder)
            .Select(x => new AdminShowCreditDto(x.Id, x.PersonId, x.Person.FullName, x.CreditTypeId, x.CreditType.Code,
                x.CustomRoleSq ?? x.CreditType.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault() ?? x.CreditType.Code,
                x.CustomRoleEn ?? x.CreditType.Translations.Where(t => t.Language.Code == "en").Select(t => t.Name).FirstOrDefault() ?? x.CreditType.Code,
                x.CharacterName, x.DisplayOrder)).ToListAsync(token);
        var types = await db.CreditTypes.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => new AdminCreditTypeDto(
            x.Id, x.Code,
            x.Translations.Where(t => t.Language.Code == "sq").Select(t => t.Name).FirstOrDefault() ?? x.Code,
            x.Translations.Where(t => t.Language.Code == "en").Select(t => t.Name).FirstOrDefault() ?? x.Code)).ToListAsync(token);
        return Ok(new AdminShowCreditsResponseDto(credits, types));
    }

    [HttpPut("{id:int}/credits")]
    public async Task<ActionResult<AdminShowCreditsResponseDto>> SaveCredits(int id, SaveAdminShowCreditsRequest request, CancellationToken token)
    {
        var show = await db.Shows.Include(x => x.Credits).ThenInclude(x => x.Person).Include(x => x.Translations).ThenInclude(x => x.Language).FirstOrDefaultAsync(x => x.Id == id, token);
        if (show is null) return NotFound();
        if (request.Credits.Any(x => string.IsNullOrWhiteSpace(x.PersonName)))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["Credits"] = ["Every credit requires a person name."] }));
        var validTypeIds = await db.CreditTypes.Select(x => x.Id).ToHashSetAsync(token);
        if (request.Credits.Any(x => !validTypeIds.Contains(x.CreditTypeId)))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["Credits"] = ["One or more credit types are invalid."] }));
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        db.ShowCredits.RemoveRange(show.Credits);
        var peopleByName = new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < request.Credits.Count; index++)
        {
            var item = request.Credits[index];
            var personName = item.PersonName.Trim();
            Person? person = null;
            if (item.PersonId.HasValue)
                person = await db.People.FirstOrDefaultAsync(x => x.Id == item.PersonId, token);
            if (person is null && !peopleByName.TryGetValue(personName, out person))
                person = await db.People.FirstOrDefaultAsync(x => x.FullName == personName, token);
            if (person is null)
            {
                person = new Person { FullName = personName, CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow };
                db.People.Add(person);
            }
            peopleByName[personName] = person;
            show.Credits.Add(new ShowCredit
            {
                Person = person, CreditTypeId = item.CreditTypeId, CharacterName = Clean(item.CharacterName),
                CustomRoleSq = Clean(item.RoleSq), CustomRoleEn = Clean(item.RoleEn), DisplayOrder = index
            });
        }
        show.UpdatedAt = clock.UtcNow;
        AddActivity("Updated credits", show, Title(show));
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return await GetCredits(id, token);
    }

    [HttpPost("{id:int}/gallery")]
    public async Task<ActionResult<AdminShowDetailDto>> AttachGalleryMedia(
        int id, AttachShowGalleryMediaRequest request, CancellationToken token)
    {
        var show = await LoadAsync(id, token);
        if (show is null) return NotFound();
        var media = await db.MediaAssets.FirstOrDefaultAsync(
            x => x.Id == request.MediaAssetId && x.IsActive && x.MimeType.StartsWith("image/"), token);
        if (media is null) return ValidationProblem("Choose an active image from the Media Library.");

        var album = show.GalleryAlbums.FirstOrDefault();
        if (album is null)
        {
            album = new GalleryAlbum
            {
                AlbumType = GalleryAlbumType.Show,
                ShowId = show.Id,
                IsPublished = true,
                IsVisibleInGeneralGallery = false,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            };
            var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token);
            foreach (var language in languages)
            {
                var title = show.Translations.FirstOrDefault(x => x.LanguageId == language.Id)?.Title ?? Title(show);
                album.Translations.Add(new GalleryAlbumTranslation
                {
                    LanguageId = language.Id,
                    Title = $"{title} Gallery",
                    Slug = $"show-{show.Id}-gallery-{language.Code}"
                });
            }
            show.GalleryAlbums.Add(album);
        }
        if (album.GalleryAlbumMedia.Any(x => x.MediaAssetId == media.Id))
            return Conflict(new ProblemDetails { Title = "Image already attached", Detail = "This image is already in the play gallery.", Status = 409 });

        album.GalleryAlbumMedia.Add(new GalleryAlbumMedia
        {
            MediaAssetId = media.Id,
            DisplayOrder = album.GalleryAlbumMedia.Count,
            IsFeatured = album.GalleryAlbumMedia.Count == 0
        });
        album.UpdatedAt = clock.UtcNow;
        show.UpdatedAt = clock.UtcNow;
        AddActivity("Added gallery image", show, $"{Title(show)}: {media.FileName}");
        await db.SaveChangesAsync(token);
        return Ok((await LoadAsync(id, token)) is { } updated ? ToDetail(updated) : throw new InvalidOperationException());
    }

    [HttpDelete("{id:int}/gallery/{mediaId:int}")]
    public async Task<IActionResult> DetachGalleryMedia(int id, int mediaId, CancellationToken token)
    {
        var item = await db.GalleryAlbumMedia
            .Include(x => x.GalleryAlbum).ThenInclude(x => x.Show!).ThenInclude(x => x.Translations).ThenInclude(x => x.Language)
            .FirstOrDefaultAsync(x => x.GalleryAlbum.ShowId == id && x.MediaAssetId == mediaId, token);
        if (item is null) return NotFound();
        db.GalleryAlbumMedia.Remove(item);
        item.GalleryAlbum.UpdatedAt = clock.UtcNow;
        item.GalleryAlbum.Show!.UpdatedAt = clock.UtcNow;
        AddActivity("Removed gallery image", item.GalleryAlbum.Show, Title(item.GalleryAlbum.Show));
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpPost("{id:int}/gallery/manage-local")]
    public async Task<ActionResult<AdminShowDetailDto>> DisableLocalGalleryFallback(int id, CancellationToken token)
    {
        var show = await LoadAsync(id, token);
        if (show is null) return NotFound();
        show.UseLocalGalleryFallback = false;
        show.UpdatedAt = clock.UtcNow;
        AddActivity("Converted local gallery", show, $"{Title(show)} gallery is now database-managed");
        await db.SaveChangesAsync(token);
        return Ok(ToDetail(show));
    }

    [HttpPost]
    public async Task<ActionResult<AdminShowDetailDto>> Create(SaveAdminShowRequest request, CancellationToken token)
    {
        var validation = await ValidateRequestAsync(request, null, token);
        if (validation is not null) return ValidationProblem(validation);
        var now = clock.UtcNow;
        var show = new Show { CreatedAt = now, UpdatedAt = now, Status = ShowStatus.Draft };
        Apply(show, request);
        await ApplyTranslationsAsync(show, request.Translations, token);
        db.Shows.Add(show);
        AddActivity("Created", show, request.Translations.First(x => x.LanguageCode == "sq").Title);
        await db.SaveChangesAsync(token);
        return CreatedAtAction(nameof(Get), new { id = show.Id }, ToDetail(show));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminShowDetailDto>> Update(int id, SaveAdminShowRequest request, CancellationToken token)
    {
        var show = await LoadAsync(id, token);
        if (show is null) return NotFound();
        var validation = await ValidateRequestAsync(request, id, token);
        if (validation is not null) return ValidationProblem(validation);
        Apply(show, request);
        await ApplyTranslationsAsync(show, request.Translations, token);
        show.UpdatedAt = clock.UtcNow;
        AddActivity("Updated", show, request.Translations.First(x => x.LanguageCode == "sq").Title);
        await db.SaveChangesAsync(token);
        return Ok(ToDetail(show));
    }

    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<AdminShowDetailDto>> Publish(int id, CancellationToken token)
    {
        var show = await LoadAsync(id, token);
        if (show is null) return NotFound();
        var missing = RequiredTranslationWarning(show);
        if (missing is not null) return Conflict(new ProblemDetails { Title = "Missing required translations", Detail = missing, Status = 409 });
        show.Status = ShowStatus.Published;
        show.PublishedAt ??= clock.UtcNow;
        show.UpdatedAt = clock.UtcNow;
        AddActivity("Published", show, Title(show));
        await db.SaveChangesAsync(token);
        return Ok(ToDetail(show));
    }

    [HttpPost("{id:int}/unpublish")]
    public async Task<ActionResult<AdminShowDetailDto>> Unpublish(int id, CancellationToken token) =>
        await ChangeStatus(id, ShowStatus.Draft, "Unpublished", token);

    [HttpPost("{id:int}/archive")]
    public async Task<ActionResult<AdminShowDetailDto>> Archive(int id, CancellationToken token) =>
        await ChangeStatus(id, ShowStatus.Archived, "Archived", token);

    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult<AdminShowDetailDto>> Restore(int id, CancellationToken token) =>
        await ChangeStatus(id, ShowStatus.Draft, "Restored", token);

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<AdminShowDetailDto>> Duplicate(int id, CancellationToken token)
    {
        var source = await LoadAsync(id, token);
        if (source is null) return NotFound();
        var now = clock.UtcNow;
        var copy = new Show
        {
            ShowCategoryId = source.ShowCategoryId, PosterMediaAssetId = source.PosterMediaAssetId,
            FeaturedMediaAssetId = source.FeaturedMediaAssetId, DurationMinutes = source.DurationMinutes,
            ProductionYear = source.ProductionYear, AgeRecommendation = source.AgeRecommendation,
            OriginalLanguage = source.OriginalLanguage, TrailerUrl = source.TrailerUrl, VideoUrl = source.VideoUrl,
            PremiereDate = source.PremiereDate, LifecycleStatus = source.LifecycleStatus,
            Status = ShowStatus.Draft, IsFeatured = false, CreatedAt = now, UpdatedAt = now
        };
        foreach (var translation in source.Translations)
            copy.Translations.Add(new ShowTranslation
            {
                LanguageId = translation.LanguageId, Title = $"{translation.Title} (Copy)",
                Slug = $"{translation.Slug}-copy-{now.ToUnixTimeSeconds()}",
                ShortDescription = translation.ShortDescription, FullDescription = translation.FullDescription,
                MetaTitle = translation.MetaTitle, MetaDescription = translation.MetaDescription
            });
        db.Shows.Add(copy);
        AddActivity("Duplicated", copy, Title(source));
        await db.SaveChangesAsync(token);
        return CreatedAtAction(nameof(Get), new { id = copy.Id }, ToDetail(copy));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool confirmHistorical = false, CancellationToken token = default)
    {
        var show = await db.Shows.Include(x => x.Performances).Include(x => x.GalleryAlbums).FirstOrDefaultAsync(x => x.Id == id, token);
        if (show is null) return NotFound();
        var isSuperAdmin = User.IsInRole(nameof(AdminRole.SuperAdmin));
        if (show.Status != ShowStatus.Draft && (!isSuperAdmin || !confirmHistorical))
            return Conflict(new ProblemDetails { Title = "Archive this play instead", Detail = "Only a Super Admin may permanently delete historical content after explicit confirmation.", Status = 409 });
        if (show.Performances.Count != 0)
            return Conflict(new ProblemDetails { Title = "Play has scheduled performances", Detail = "Remove its performances before permanently deleting this play.", Status = 409 });
        AddActivity("Deleted", show, $"Deleted play {id}");
        db.Shows.Remove(show);
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    private async Task<ActionResult<AdminShowDetailDto>> ChangeStatus(int id, ShowStatus status, string action, CancellationToken token)
    {
        var show = await LoadAsync(id, token);
        if (show is null) return NotFound();
        show.Status = status;
        show.UpdatedAt = clock.UtcNow;
        AddActivity(action, show, Title(show));
        await db.SaveChangesAsync(token);
        return Ok(ToDetail(show));
    }

    private async Task<Show?> LoadAsync(int id, CancellationToken token) =>
        await db.Shows
            .Include(x => x.Translations).ThenInclude(x => x.Language)
            .Include(x => x.PosterMediaAsset)
            .Include(x => x.FeaturedMediaAsset)
            .Include(x => x.GalleryAlbums).ThenInclude(x => x.GalleryAlbumMedia).ThenInclude(x => x.MediaAsset).ThenInclude(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, token);

    private async Task<ValidationProblemDetails?> ValidateRequestAsync(SaveAdminShowRequest request, int? id, CancellationToken token)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.TryParse<ShowLifecycleStatus>(request.LifecycleStatus, true, out _))
            errors[nameof(request.LifecycleStatus)] = ["Invalid lifecycle status."];
        if (!string.IsNullOrWhiteSpace(request.TrailerUrl) &&
            !request.TrailerUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) &&
            (!Uri.TryCreate(request.TrailerUrl, UriKind.Absolute, out var trailerUri) ||
             (trailerUri.Scheme != Uri.UriSchemeHttp && trailerUri.Scheme != Uri.UriSchemeHttps)))
            errors[nameof(request.TrailerUrl)] = ["Choose a local Media Library video or enter a valid HTTP/HTTPS video URL."];
        var codes = request.Translations.Select(x => x.LanguageCode).ToList();
        if (!codes.Contains("sq") || !codes.Contains("en") || codes.Distinct().Count() != codes.Count)
            errors[nameof(request.Translations)] = ["Exactly one Albanian and one English translation are required."];
        foreach (var translation in request.Translations)
        {
            var slug = Slugify(translation.Title);
            if (string.IsNullOrEmpty(slug))
                errors[$"Translations.{translation.LanguageCode}.Title"] = ["The title must contain letters or numbers."];
        }
        if (!await db.ShowCategories.AnyAsync(x => x.Id == request.ShowCategoryId && x.IsActive, token))
            errors[nameof(request.ShowCategoryId)] = ["The selected category does not exist."];
        return errors.Count == 0 ? null : new ValidationProblemDetails(errors);
    }

    private static void Apply(Show show, SaveAdminShowRequest request)
    {
        show.ShowCategoryId = request.ShowCategoryId;
        show.PosterMediaAssetId = request.PosterMediaAssetId;
        show.FeaturedMediaAssetId = request.FeaturedMediaAssetId;
        show.DurationMinutes = request.DurationMinutes;
        show.ProductionYear = request.ProductionYear;
        show.AgeRecommendation = request.AgeRecommendation;
        show.OriginalLanguage = Clean(request.OriginalLanguage);
        show.TrailerUrl = Clean(request.TrailerUrl);
        show.VideoUrl = Clean(request.VideoUrl);
        show.PremiereDate = request.PremiereDate;
        show.LifecycleStatus = Enum.Parse<ShowLifecycleStatus>(request.LifecycleStatus, true);
        show.IsFeatured = request.IsFeatured;
    }

    private async Task ApplyTranslationsAsync(Show show, IReadOnlyList<SaveAdminShowTranslationDto> translations, CancellationToken token)
    {
        var languages = await db.Languages.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, token);
        foreach (var item in translations)
        {
            var language = languages[item.LanguageCode];
            var translation = show.Translations.FirstOrDefault(x => x.LanguageId == language.Id);
            if (translation is null)
            {
                translation = new ShowTranslation { LanguageId = language.Id, Language = language };
                show.Translations.Add(translation);
            }
            translation.Title = item.Title.Trim();
            translation.Slug = await UniqueSlugAsync(item.Title, language.Id, show.Id, token);
            translation.ShortDescription = item.ShortDescription.Trim();
            translation.FullDescription = item.FullDescription.Trim();
            translation.MetaTitle = Clean(item.MetaTitle);
            translation.MetaDescription = Clean(item.MetaDescription);
        }
    }

    private void AddActivity(string action, Show show, string summary) =>
        db.AdminActivities.Add(new AdminActivity
        {
            AdminUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
            Action = action, EntityType = "Show", EntityId = show.Id == 0 ? null : show.Id.ToString(),
            Summary = summary, CreatedAt = clock.UtcNow
        });

    private static string? RequiredTranslationWarning(Show show)
    {
        foreach (var code in new[] { "sq", "en" })
        {
            var t = show.Translations.FirstOrDefault(x => x.Language.Code == code);
            if (t is null || string.IsNullOrWhiteSpace(t.Title) || string.IsNullOrWhiteSpace(t.ShortDescription) || string.IsNullOrWhiteSpace(t.FullDescription))
                return $"The {code.ToUpperInvariant()} translation is incomplete.";
        }
        return null;
    }

    private static AdminShowDetailDto ToDetail(Show x) => new(
        x.Id, x.ShowCategoryId, x.PosterMediaAssetId, x.FeaturedMediaAssetId,
        x.PosterMediaAsset?.FileUrl, x.FeaturedMediaAsset?.FileUrl, x.DurationMinutes,
        x.ProductionYear, x.AgeRecommendation, x.OriginalLanguage, x.TrailerUrl, x.VideoUrl,
        x.PremiereDate, x.Status.ToString(), x.LifecycleStatus.ToString(), x.IsFeatured,
        x.CreatedAt, x.UpdatedAt, x.PublishedAt,
        x.Translations.OrderBy(t => t.Language.Code).Select(t => new AdminShowTranslationDto(
            t.Language.Code, t.Title, t.Slug, t.ShortDescription, t.FullDescription, t.MetaTitle, t.MetaDescription)).ToList(),
        x.GalleryAlbums.SelectMany(a => a.GalleryAlbumMedia).OrderBy(m => m.DisplayOrder).Select(m =>
            new AdminShowGalleryMediaDto(m.MediaAsset.Id, m.MediaAsset.FileUrl, m.MediaAsset.FileName, m.MediaAsset.MimeType,
                m.MediaAsset.Translations.FirstOrDefault(t => t.Language.Code == "sq")?.Caption,
                m.MediaAsset.Translations.FirstOrDefault(t => t.Language.Code == "en")?.Caption)).ToList(),
        x.UseLocalGalleryFallback);
    private static string Title(Show show) => show.Translations.FirstOrDefault(x => x.Language.Code == "sq")?.Title ?? $"Show {show.Id}";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(normalized.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark).ToArray());
        var slug = Regex.Replace(withoutMarks, "[^a-z0-9]+", "-").Trim('-');
        return slug.Length <= 220 ? slug : slug[..220].TrimEnd('-');
    }

    private async Task<string> UniqueSlugAsync(string title, int languageId, int showId, CancellationToken token)
    {
        var baseSlug = Slugify(title);
        var candidate = baseSlug;
        for (var suffix = 2; await db.ShowTranslations.AnyAsync(
                 x => x.LanguageId == languageId && x.Slug == candidate && x.ShowId != showId, token); suffix++)
        {
            var ending = $"-{suffix}";
            candidate = $"{baseSlug[..Math.Min(baseSlug.Length, 220 - ending.Length)].TrimEnd('-')}{ending}";
        }
        return candidate;
    }
}
