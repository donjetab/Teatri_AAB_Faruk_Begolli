using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController(AppDbContext db, IClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminDashboardDto>> Get(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var languageCount = await db.Languages.CountAsync(x => x.IsActive, cancellationToken);
        var metrics = new List<DashboardMetricDto>
        {
            new("publishedPlays", await db.Shows.CountAsync(x => x.Status == ShowStatus.Published, cancellationToken), "Published plays"),
            new("draftPlays", await db.Shows.CountAsync(x => x.Status == ShowStatus.Draft, cancellationToken), "Draft plays"),
            new("archivedPlays", await db.Shows.CountAsync(x => x.Status == ShowStatus.Archived, cancellationToken), "Archived plays"),
            new("upcomingPerformances", await db.ShowPerformances.CountAsync(x => x.StartDateTimeUtc > now && x.Status != PerformanceStatus.Cancelled, cancellationToken), "Upcoming performances"),
            new("publishedNews", await db.NewsArticles.CountAsync(x => x.IsPublished, cancellationToken), "Published news"),
            new("draftNews", await db.NewsArticles.CountAsync(x => !x.IsPublished, cancellationToken), "Draft news"),
            new("unreadMessages", await db.ContactMessages.CountAsync(x => x.Status == ContactMessageStatus.New, cancellationToken), "New messages"),
            new("subscribers", await db.NewsletterSubscribers.CountAsync(x => x.IsActive, cancellationToken), "Subscribers"),
            new("missingSq", await MissingTranslationsAsync(1, cancellationToken), "Missing Albanian"),
            new("missingEn", await MissingTranslationsAsync(2, cancellationToken), "Missing English"),
            new("brokenLinks", 0, "Broken links"),
            new("unusedMedia", await db.MediaAssets.CountAsync(x => x.IsActive && !db.GalleryAlbumMedia.Any(g => g.MediaAssetId == x.Id), cancellationToken), "Potentially unused media")
        };
        var performances = await db.ShowPerformances.Where(x => x.StartDateTimeUtc > now && x.Status != PerformanceStatus.Cancelled)
            .OrderBy(x => x.StartDateTimeUtc).Take(5).Select(x => new DashboardItemDto(x.Id,
                x.Show.Translations.OrderBy(t => t.LanguageId).Select(t => t.Title).FirstOrDefault() ?? "Untitled",
                x.Location!.Translations.OrderBy(t => t.LanguageId).Select(t => t.Name).FirstOrDefault(), x.StartDateTimeUtc, x.Status.ToString())).ToListAsync(cancellationToken);
        var plays = await db.Shows.OrderByDescending(x => x.UpdatedAt).Take(5).Select(x => new DashboardItemDto(x.Id,
            x.Translations.OrderBy(t => t.LanguageId).Select(t => t.Title).FirstOrDefault() ?? "Untitled", null, x.UpdatedAt, x.Status.ToString())).ToListAsync(cancellationToken);
        var news = await db.NewsArticles.OrderByDescending(x => x.UpdatedAt).Take(5).Select(x => new DashboardItemDto(x.Id,
            x.Translations.OrderBy(t => t.LanguageId).Select(t => t.Title).FirstOrDefault() ?? "Untitled", null, x.UpdatedAt, x.IsPublished ? "Published" : "Draft")).ToListAsync(cancellationToken);
        var messages = await db.ContactMessages.OrderByDescending(x => x.CreatedAt).Take(5).Select(x => new DashboardItemDto(x.Id, x.Subject, x.Name, x.CreatedAt, x.Status.ToString())).ToListAsync(cancellationToken);
        var activity = await db.AdminActivities.OrderByDescending(x => x.CreatedAt).Take(8).Select(x => new DashboardItemDto((int)x.Id, x.Action, x.Summary, x.CreatedAt, x.EntityType)).ToListAsync(cancellationToken);
        return Ok(new AdminDashboardDto(metrics, performances, plays, news, messages, activity));
    }

    private async Task<int> MissingTranslationsAsync(int languageId, CancellationToken token) =>
        await db.Shows.CountAsync(x => x.Translations.Count(t => t.LanguageId == languageId) == 0, token)
        + await db.NewsArticles.CountAsync(x => x.Translations.Count(t => t.LanguageId == languageId) == 0, token)
        + await db.PitfEditions.CountAsync(x => x.Translations.Count(t => t.LanguageId == languageId) == 0, token);
}
