using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class NewsletterServiceTests
{
    [Fact]
    public async Task Subscribe_NormalizesEmailAndStoresPreferredLanguage()
    {
        await using var db = CreateDb();
        var service = new NewsletterService(db, new FakeClock());

        var response = await service.SubscribeAsync(new NewsletterSubscribeRequest(" USER@Example.COM ", "sq"), CancellationToken.None);

        Assert.Equal("Subscription completed successfully.", response.Message);
        var subscriber = Assert.Single(db.NewsletterSubscribers);
        Assert.Equal("user@example.com", subscriber.Email);
        Assert.Equal("sq", subscriber.PreferredLanguageCode);
        Assert.True(subscriber.IsActive);
    }

    [Fact]
    public async Task Subscribe_RejectsDuplicateActiveSubscription()
    {
        await using var db = CreateDb();
        db.NewsletterSubscribers.Add(new NewsletterSubscriber
        {
            Email = "user@example.com",
            PreferredLanguageCode = "sq",
            IsActive = true,
            SubscribedAt = FakeClock.Now
        });
        await db.SaveChangesAsync();

        var service = new NewsletterService(db, new FakeClock());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubscribeAsync(new NewsletterSubscribeRequest("USER@example.com", "en"), CancellationToken.None));

        Assert.Equal(1, await db.NewsletterSubscribers.CountAsync());
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class FakeClock : IClock
    {
        public static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }
}
