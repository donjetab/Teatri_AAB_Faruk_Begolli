using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class HomepageServiceTests
{
    [Fact]
    public async Task UpcomingShows_AreOrderedByNearestValidFuturePerformance()
    {
        await using var db = CreateDb();
        var ids = await SeedBaseAsync(db);
        var poster = AddMedia(db, "/poster.jpg");

        AddShow(db, ids, "third", "Third", poster, "Director C", [
            Performance(daysFromNow: 9, PerformanceStatus.Scheduled)
        ]);
        AddShow(db, ids, "first", "First", poster, "Director A", [
            Performance(daysFromNow: 3, PerformanceStatus.Scheduled)
        ]);
        AddShow(db, ids, "second", "Second", poster, "Director B", [
            Performance(daysFromNow: 6, PerformanceStatus.Scheduled)
        ]);

        await db.SaveChangesAsync();

        var result = await CreateService(db).GetHomeAsync("sq", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(["First", "Second", "Third"], result.UpcomingShows.Select(x => x.Title).ToArray());
    }

    [Fact]
    public async Task UpcomingShows_ExcludePastAndCancelledPerformances()
    {
        await using var db = CreateDb();
        var ids = await SeedBaseAsync(db);
        var poster = AddMedia(db, "/poster.jpg");

        AddShow(db, ids, "valid", "Valid", poster, "Director", [
            Performance(daysFromNow: 5, PerformanceStatus.Scheduled)
        ]);
        AddShow(db, ids, "past", "Past", poster, "Director", [
            Performance(daysFromNow: -1, PerformanceStatus.Scheduled)
        ]);
        AddShow(db, ids, "cancelled", "Cancelled", poster, "Director", [
            Performance(daysFromNow: 2, PerformanceStatus.Cancelled)
        ]);

        await db.SaveChangesAsync();

        var result = await CreateService(db).GetHomeAsync("sq", CancellationToken.None);

        Assert.NotNull(result);
        var show = Assert.Single(result.UpcomingShows);
        Assert.Equal("Valid", show.Title);
    }

    [Fact]
    public async Task UpcomingShows_ReturnShowOnlyOnceUsingNearestPerformance()
    {
        await using var db = CreateDb();
        var ids = await SeedBaseAsync(db);
        var poster = AddMedia(db, "/poster.jpg");

        AddShow(db, ids, "multi", "Multi", poster, "Director", [
            Performance(daysFromNow: 8, PerformanceStatus.Scheduled),
            Performance(daysFromNow: 2, PerformanceStatus.Scheduled)
        ]);

        await db.SaveChangesAsync();

        var result = await CreateService(db).GetHomeAsync("sq", CancellationToken.None);

        Assert.NotNull(result);
        var show = Assert.Single(result.UpcomingShows);
        Assert.Equal("Multi", show.Title);
        Assert.Equal(FakeClock.Now.AddDays(2), show.NearestPerformanceDateUtc);
    }

    [Fact]
    public async Task Home_UsesAlbanianFallbackWhenEnglishTranslationIsMissing()
    {
        await using var db = CreateDb();
        var ids = await SeedBaseAsync(db, includeEnglishHomeTranslation: false);
        var poster = AddMedia(db, "/poster.jpg");

        AddShow(db, ids, "fallback-show", "Titulli Shqip", poster, "Director", [
            Performance(daysFromNow: 2, PerformanceStatus.Scheduled)
        ], includeEnglishTranslation: false);

        await db.SaveChangesAsync();

        var result = await CreateService(db).GetHomeAsync("en", CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsFallbackTranslation);
        Assert.Equal("Teatri Shqip", result.TheatreName);
        var show = Assert.Single(result.UpcomingShows);
        Assert.True(show.IsFallbackTranslation);
        Assert.Equal("Titulli Shqip", show.Title);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IHomepageService CreateService(AppDbContext db) => new HomepageService(db, new FakeClock());

    private static async Task<TestIds> SeedBaseAsync(AppDbContext db, bool includeEnglishHomeTranslation = true)
    {
        var sq = new Language { Code = "sq", Name = "Albanian", IsDefault = true, IsActive = true };
        var en = new Language { Code = "en", Name = "English", IsDefault = false, IsActive = true };
        var hero = AddMedia(db, "/hero.jpg");
        var about = AddMedia(db, "/about.jpg");
        var reservation = AddMedia(db, "/reservation.jpg");
        var pitf = AddMedia(db, "/pitf.jpg");

        db.Languages.AddRange(sq, en);
        await db.SaveChangesAsync();

        var theatre = new TheatreInformation
        {
            FoundedYear = 2015,
            PerformancesCount = 500,
            SpectatorsCount = 100000,
            HeroBackgroundMediaAsset = hero,
            AboutPreviewMediaAsset = about,
            ReservationBannerMediaAsset = reservation,
            PitfFeatureMediaAsset = pitf,
            Address = "Prishtine",
            Phone = "+383",
            Email = "info@example.com",
            ReservationUrl = "https://example.com",
            UpdatedAt = FakeClock.Now,
            Translations =
            [
                new TheatreInformationTranslation
                {
                    LanguageId = sq.Id,
                    TheatreName = "Teatri Shqip",
                    HeroSlogan = "Slogan",
                    AboutShort = "About",
                    AboutFull = "Full",
                    PitfShortDescription = "PITF",
                    AddressDisplayText = "Prishtine",
                    ReservationCallToActionTitle = "Reserve",
                    ReservationCallToActionText = "Text"
                }
            ]
        };

        if (includeEnglishHomeTranslation)
        {
            theatre.Translations.Add(new TheatreInformationTranslation
            {
                LanguageId = en.Id,
                TheatreName = "English Theatre",
                HeroSlogan = "Slogan",
                AboutShort = "About",
                AboutFull = "Full",
                PitfShortDescription = "PITF",
                AddressDisplayText = "Pristina",
                ReservationCallToActionTitle = "Reserve",
                ReservationCallToActionText = "Text"
            });
        }

        var category = new ShowCategory { IsActive = true, DisplayOrder = 1 };
        var creditType = new CreditType { Code = "director", DisplayOrder = 1 };
        var location = new Location { IsActive = true, Latitude = 1, Longitude = 1 };
        var pitfEdition = new PitfEdition
        {
            EditionNumber = 1,
            Year = 2030,
            IsFeatured = true,
            IsPublished = true,
            CoverMediaAsset = pitf,
            CreatedAt = FakeClock.Now,
            UpdatedAt = FakeClock.Now,
            Translations =
            [
                new PitfEditionTranslation
                {
                    LanguageId = sq.Id,
                    Title = "PITF",
                    Slug = "pitf",
                    ShortDescription = "Short",
                    FullDescription = "Full"
                }
            ]
        };

        db.AddRange(theatre, category, creditType, location, pitfEdition);
        await db.SaveChangesAsync();

        return new TestIds(sq.Id, en.Id, category, creditType, location);
    }

    private static MediaAsset AddMedia(AppDbContext db, string url)
    {
        var media = new MediaAsset
        {
            FileUrl = url,
            FileName = Path.GetFileName(url),
            MimeType = "image/jpeg",
            UploadedAt = FakeClock.Now,
            IsActive = true
        };
        db.MediaAssets.Add(media);
        return media;
    }

    private static void AddShow(
        AppDbContext db,
        TestIds ids,
        string slug,
        string title,
        MediaAsset poster,
        string directorName,
        IEnumerable<ShowPerformance> performances,
        bool includeEnglishTranslation = true)
    {
        var director = new Person { FullName = directorName, CreatedAt = FakeClock.Now, UpdatedAt = FakeClock.Now };
        var show = new Show
        {
            ShowCategory = ids.Category,
            PosterMediaAsset = poster,
            Status = ShowStatus.Published,
            CreatedAt = FakeClock.Now,
            UpdatedAt = FakeClock.Now,
            PublishedAt = FakeClock.Now,
            Translations =
            [
                new ShowTranslation
                {
                    LanguageId = ids.SqLanguageId,
                    Title = title,
                    Slug = slug,
                    ShortDescription = "Short",
                    FullDescription = "Full"
                }
            ],
            Credits =
            [
                new ShowCredit
                {
                    Person = director,
                    CreditType = ids.DirectorCreditType,
                    DisplayOrder = 1
                }
            ]
        };

        if (includeEnglishTranslation)
        {
            show.Translations.Add(new ShowTranslation
            {
                LanguageId = ids.EnLanguageId,
                Title = $"{title} EN",
                Slug = $"{slug}-en",
                ShortDescription = "Short",
                FullDescription = "Full"
            });
        }

        foreach (var performance in performances)
        {
            performance.Show = show;
            performance.Location = ids.Location;
            show.Performances.Add(performance);
        }

        db.Shows.Add(show);
    }

    private static ShowPerformance Performance(int daysFromNow, PerformanceStatus status)
    {
        return new ShowPerformance
        {
            StartDateTimeUtc = FakeClock.Now.AddDays(daysFromNow),
            Status = status,
            TicketUrl = "https://tickets.example.com",
            CreatedAt = FakeClock.Now,
            UpdatedAt = FakeClock.Now
        };
    }

    private sealed record TestIds(int SqLanguageId, int EnLanguageId, ShowCategory Category, CreditType DirectorCreditType, Location Location);

    private sealed class FakeClock : IClock
    {
        public static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }
}
