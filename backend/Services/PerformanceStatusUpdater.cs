using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.Models;

namespace Theatre.Api.Services;

public sealed class PerformanceStatusUpdater(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<PerformanceStatusUpdater> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateStatusesAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await UpdateStatusesAsync(stoppingToken);
    }

    private async Task UpdateStatusesAsync(CancellationToken token)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = clock.UtcNow;
            var performances = await db.ShowPerformances
                .Where(x => x.StartDateTimeUtc <= now
                    && (x.Status == PerformanceStatus.Scheduled || x.Status == PerformanceStatus.SoldOut))
                .ToListAsync(token);
            if (performances.Count == 0) return;

            foreach (var performance in performances)
            {
                performance.Status = PerformanceStatus.Completed;
                performance.IsPublished = false;
                performance.UpdatedAt = now;
                db.AdminActivities.Add(new AdminActivity
                {
                    Action = "Auto-completed",
                    EntityType = "Performance",
                    EntityId = performance.Id.ToString(),
                    Summary = "Performance automatically completed when its start time was reached.",
                    CreatedAt = now
                });
            }
            await db.SaveChangesAsync(token);
            logger.LogInformation("Automatically completed {Count} performance(s).", performances.Count);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not automatically update performance statuses.");
        }
    }
}
