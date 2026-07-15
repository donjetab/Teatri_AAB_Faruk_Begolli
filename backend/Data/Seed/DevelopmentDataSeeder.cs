using Microsoft.EntityFrameworkCore;
using Theatre.Api.Models;

namespace Theatre.Api.Data.Seed;

public static class DevelopmentDataSeeder
{
    private const string Sq = "sq";
    private const string En = "en";

    public static async Task SeedAsync(AppDbContext db, IWebHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var languages = await EnsureLanguagesAsync(db, cancellationToken);
        var media = await EnsureMediaAssetsAsync(db, environment, languages, now, cancellationToken);
        var location = await EnsureLocationAsync(db, languages, cancellationToken);
        var category = await EnsureShowCategoryAsync(db, languages, cancellationToken);
        var directorCreditType = await EnsureCreditTypeAsync(db, languages, cancellationToken);

        await EnsureTheatreInformationAsync(db, languages, media, now, cancellationToken);
        await EnsureShowsAsync(db, languages, media, category, directorCreditType, location, now, cancellationToken);
        await EnsurePitfEditionAsync(db, languages, media, now, cancellationToken);
    }

    private static async Task<Dictionary<string, Language>> EnsureLanguagesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var languages = await db.Languages.ToDictionaryAsync(x => x.Code, cancellationToken);

        UpsertLanguage(languages, Sq, "Albanian", isDefault: true);
        UpsertLanguage(languages, En, "English", isDefault: false);

        await db.SaveChangesAsync(cancellationToken);
        return await db.Languages.ToDictionaryAsync(x => x.Code, cancellationToken);

        void UpsertLanguage(Dictionary<string, Language> existing, string code, string name, bool isDefault)
        {
            if (!existing.TryGetValue(code, out var language))
            {
                language = new Language { Code = code };
                db.Languages.Add(language);
                existing[code] = language;
            }

            language.Name = name;
            language.IsDefault = isDefault;
            language.IsActive = true;
        }
    }

    private static async Task<Dictionary<string, MediaAsset>> EnsureMediaAssetsAsync(
        AppDbContext db,
        IWebHostEnvironment environment,
        IReadOnlyDictionary<string, Language> languages,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new MediaDefinition("logo", "/uploads/dev/branding/aab-theatre-logo-white.png", "aab-theatre-logo-white.png", "image/png", 1158, 309, "Logoja e Teatrit AAB Faruk Begolli", "AAB Theatre Faruk Begolli logo"),
            new MediaDefinition("hero", "/uploads/dev/homepage/hero-theatre-hall.png", "hero-theatre-hall.png", "image/png", 1920, 730, "Salla e Teatrit AAB Faruk Begolli", "AAB Theatre Faruk Begolli auditorium"),
            new MediaDefinition("about", "/uploads/dev/homepage/about-preview-per-ne.jpg", "about-preview-per-ne.jpg", "image/jpeg", 1500, 600, "Pamje per seksionin Per ne", "Image for the About preview section"),
            new MediaDefinition("pitfPreview", "/uploads/dev/homepage/pitf-preview.jpg", "pitf-preview.jpg", "image/jpeg", 2048, 1365, "Shfaqje nga programi PITF", "Performance from the PITF program"),
            new MediaDefinition("reservation", "/uploads/dev/homepage/reservation-banner.png", "reservation-banner.png", "image/png", 2037, 778, "Sfond skene per rezervime", "Stage background for reservations"),
            new MediaDefinition("bretkosaPoster", "/uploads/dev/shows/bretkosa-poster.png", "bretkosa-poster.png", "image/png", 2340, 3120, "Poster per shfaqjen Bretkosa", "Poster for the show Bretkosa"),
            new MediaDefinition("qeniPoster", "/uploads/dev/shows/qeni-i-baskervileve-poster.jpg", "qeni-i-baskervileve-poster.jpg", "image/jpeg", 595, 842, "Poster per shfaqjen Qeni i Baskervileve", "Poster for the show The Hound of the Baskervilles"),
            new MediaDefinition("gjirafaPoster", "/uploads/dev/shows/gjirafa-dhe-buburreci-poster.jpg", "gjirafa-dhe-buburreci-poster.jpg", "image/jpeg", 1242, 1713, "Poster per shfaqjen Gjirafa dhe Buburreci", "Poster for the show The Giraffe and the Ant"),
            new MediaDefinition("rrenaPoster", "/uploads/dev/shows/missing-rrena-poster-placeholder.svg", "missing-rrena-poster-placeholder.svg", "image/svg+xml", 900, 1200, "Poster mungon per shfaqjen Rrena", "Missing poster placeholder for the show Rrena"),
            new MediaDefinition("pitfCover", "/uploads/dev/pitf/pitf-2024-cover.jpg", "pitf-2024-cover.jpg", "image/jpeg", 2048, 1365, "Imazh promovues per PITF 2024", "Promotional image for PITF 2024")
        };

        var mediaByUrl = await db.MediaAssets
            .Include(x => x.Translations)
            .ToDictionaryAsync(x => x.FileUrl, cancellationToken);

        var result = new Dictionary<string, MediaAsset>();
        foreach (var definition in definitions)
        {
            if (!mediaByUrl.TryGetValue(definition.FileUrl, out var asset))
            {
                asset = new MediaAsset
                {
                    FileUrl = definition.FileUrl,
                    UploadedAt = now,
                    IsActive = true
                };
                db.MediaAssets.Add(asset);
                mediaByUrl[definition.FileUrl] = asset;
            }

            asset.FileName = definition.FileName;
            asset.MimeType = definition.MimeType;
            asset.Width = definition.Width;
            asset.Height = definition.Height;
            asset.FileSize = GetFileSize(environment, definition.FileUrl);
            asset.IsActive = true;

            EnsureMediaTranslation(asset, languages[Sq].Id, definition.AltTextSq);
            EnsureMediaTranslation(asset, languages[En].Id, definition.AltTextEn);
            result[definition.Key] = asset;
        }

        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task<Location> EnsureLocationAsync(AppDbContext db, IReadOnlyDictionary<string, Language> languages, CancellationToken cancellationToken)
    {
        var location = await db.Locations
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.GoogleMapsUrl == "https://maps.google.com/?q=AAB+Theatre+Faruk+Begolli", cancellationToken);

        if (location is null)
        {
            location = new Location { GoogleMapsUrl = "https://maps.google.com/?q=AAB+Theatre+Faruk+Begolli" };
            db.Locations.Add(location);
        }

        location.Latitude = 42.648m;
        location.Longitude = 21.148m;
        location.IsActive = true;

        EnsureLocationTranslation(location, languages[Sq].Id, "Teatri AAB Faruk Begolli", "Prishtine, Kosove");
        EnsureLocationTranslation(location, languages[En].Id, "AAB Theatre Faruk Begolli", "Pristina, Kosovo");

        await db.SaveChangesAsync(cancellationToken);
        return location;
    }

    private static async Task<ShowCategory> EnsureShowCategoryAsync(AppDbContext db, IReadOnlyDictionary<string, Language> languages, CancellationToken cancellationToken)
    {
        var category = await db.ShowCategories
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Translations.Any(t => t.LanguageId == languages[Sq].Id && t.Slug == "repertori"), cancellationToken);

        if (category is null)
        {
            category = new ShowCategory();
            db.ShowCategories.Add(category);
        }

        category.IsActive = true;
        category.DisplayOrder = 1;
        EnsureShowCategoryTranslation(category, languages[Sq].Id, "Repertori", "repertori");
        EnsureShowCategoryTranslation(category, languages[En].Id, "Repertoire", "repertoire");

        await db.SaveChangesAsync(cancellationToken);
        return category;
    }

    private static async Task<CreditType> EnsureCreditTypeAsync(AppDbContext db, IReadOnlyDictionary<string, Language> languages, CancellationToken cancellationToken)
    {
        var creditType = await db.CreditTypes
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Code == "director", cancellationToken);

        if (creditType is null)
        {
            creditType = new CreditType { Code = "director" };
            db.CreditTypes.Add(creditType);
        }

        creditType.DisplayOrder = 1;
        EnsureCreditTypeTranslation(creditType, languages[Sq].Id, "Regjia");
        EnsureCreditTypeTranslation(creditType, languages[En].Id, "Director");

        await db.SaveChangesAsync(cancellationToken);
        return creditType;
    }

    private static async Task EnsureTheatreInformationAsync(
        AppDbContext db,
        IReadOnlyDictionary<string, Language> languages,
        IReadOnlyDictionary<string, MediaAsset> media,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var theatreInfo = await db.TheatreInformation
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(cancellationToken);

        if (theatreInfo is null)
        {
            theatreInfo = new TheatreInformation();
            db.TheatreInformation.Add(theatreInfo);
        }

        theatreInfo.FoundedYear = 2009;
        theatreInfo.PerformancesCount = 120;
        theatreInfo.SpectatorsCount = 45000;
        theatreInfo.HeroBackgroundMediaAsset = media["hero"];
        theatreInfo.AboutPreviewMediaAsset = media["about"];
        theatreInfo.PitfFeatureMediaAsset = media["pitfPreview"];
        theatreInfo.ReservationBannerMediaAsset = media["reservation"];
        theatreInfo.Address = "Prishtine, Kosove";
        theatreInfo.Phone = "+383 44 000 000";
        theatreInfo.Email = "info@teatriaab.org";
        theatreInfo.Latitude = 42.648m;
        theatreInfo.Longitude = 21.148m;
        theatreInfo.FacebookUrl = "https://www.facebook.com/aabtheatre";
        theatreInfo.InstagramUrl = "https://www.instagram.com/aabtheatre";
        theatreInfo.ReservationUrl = "https://example.com/reservations";
        theatreInfo.UpdatedAt = now;

        EnsureTheatreInfoTranslation(
            theatreInfo,
            languages[Sq].Id,
            "Teatri AAB \"Faruk Begolli\"",
            "Aty ku skena flet.",
            "Teatri AAB \"Faruk Begolli\" eshte hapesire akademike dhe artistike per shfaqje, festivale dhe zhvillim te krijuesve te rinj.",
            "Teatri AAB \"Faruk Begolli\" kultivon repertor bashkekohor, bashkepunime artistike dhe ngjarje teatrore per publikun ne Kosove.",
            "PITF sjell ne skene artiste, studente dhe produksione teatrore ne nje program festivali nderkombetar.",
            "Prishtine, Kosove",
            "Rezervo vendin tend",
            "Zgjidh shfaqjen dhe siguro vendin per mbremjen e radhes ne teatrin tone.",
            "Teatri AAB Faruk Begolli",
            "Faqja zyrtare e Teatrit AAB Faruk Begolli ne Kosove.");

        EnsureTheatreInfoTranslation(
            theatreInfo,
            languages[En].Id,
            "AAB Theatre \"Faruk Begolli\"",
            "Where the stage speaks.",
            "AAB Theatre \"Faruk Begolli\" is an academic and artistic venue for performances, festivals, and emerging theatre makers.",
            "AAB Theatre \"Faruk Begolli\" develops contemporary repertoire, artistic collaborations, and theatre events for audiences in Kosovo.",
            "PITF brings artists, students, and theatre productions together in an international festival program.",
            "Pristina, Kosovo",
            "Reserve your seat",
            "Choose a performance and secure your place for the next evening at our theatre.",
            "AAB Theatre Faruk Begolli",
            "Official website of AAB Theatre Faruk Begolli in Kosovo.");

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureShowsAsync(
        AppDbContext db,
        IReadOnlyDictionary<string, Language> languages,
        IReadOnlyDictionary<string, MediaAsset> media,
        ShowCategory category,
        CreditType directorCreditType,
        Location location,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var performanceBase = new DateTimeOffset(2030, 9, 1, 19, 0, 0, TimeSpan.Zero);
        var shows = new[]
        {
            new ShowDefinition("bretkosa", "bretkosaPoster", 85, true, "Bretkosa", "Bretkosa", "Satire e erret per identitetin dhe pushtetin.", "A dark satire about identity and power.", "Burbqe Berisha", new[] { 7, 21 }),
            new ShowDefinition("qeni-i-baskervileve", "qeniPoster", 75, false, "Qeni i Baskervileve", "The Hound of the Baskervilles", "Mister klasik ne skenen teatrore.", "A classic mystery brought to the stage.", "Jon Saqipi", new[] { 10 }),
            new ShowDefinition("rrena", "rrenaPoster", 70, false, "Rrena", "The Lie", "Shfaqje zhvillimore per testimin e karteles se trete ne balline.", "Development show for testing the third homepage card.", "Agon Myftari", new[] { 14 })
        };

        foreach (var definition in shows)
        {
            var show = await db.Shows
                .Include(x => x.Translations)
                .Include(x => x.Credits)
                .Include(x => x.Performances)
                .FirstOrDefaultAsync(x => x.Translations.Any(t => t.LanguageId == languages[Sq].Id && t.Slug == definition.Slug), cancellationToken);

            if (show is null)
            {
                show = new Show { CreatedAt = now };
                db.Shows.Add(show);
            }

            show.ShowCategory = category;
            show.PosterMediaAsset = media[definition.PosterKey];
            show.DurationMinutes = definition.DurationMinutes;
            show.Status = ShowStatus.Published;
            show.IsFeatured = definition.IsFeatured;
            show.PublishedAt ??= now;
            show.UpdatedAt = now;

            EnsureShowTranslation(show, languages[Sq].Id, definition.TitleSq, definition.Slug, definition.ShortSq, $"{definition.ShortSq} Ky tekst sherben si permbajtje zhvillimore per testim te faqes.", definition.TitleSq, definition.ShortSq);
            EnsureShowTranslation(show, languages[En].Id, definition.TitleEn, $"{definition.Slug}-en", definition.ShortEn, $"{definition.ShortEn} This text is development content for testing the website.", definition.TitleEn, definition.ShortEn);

            var director = await EnsurePersonAsync(db, languages, definition.DirectorName, now, cancellationToken);
            EnsureShowCredit(show, director, directorCreditType);

            foreach (var days in definition.FuturePerformanceOffsets)
            {
                EnsurePerformance(show, location, performanceBase.AddDays(days), PerformanceStatus.Scheduled, "https://example.com/reservations");
            }
        }

        var bretkosa = await db.Shows
            .Include(x => x.Translations)
            .Include(x => x.Performances)
            .FirstAsync(x => x.Translations.Any(t => t.LanguageId == languages[Sq].Id && t.Slug == "bretkosa"), cancellationToken);

        EnsurePerformance(bretkosa, location, new DateTimeOffset(2024, 10, 31, 19, 0, 0, TimeSpan.Zero), PerformanceStatus.Completed, null);
        EnsurePerformance(bretkosa, location, new DateTimeOffset(2030, 10, 5, 19, 0, 0, TimeSpan.Zero), PerformanceStatus.Cancelled, null);

        var qeni = await db.Shows
            .Include(x => x.Translations)
            .Include(x => x.Credits)
            .FirstAsync(x => x.Translations.Any(t => t.LanguageId == languages[Sq].Id && t.Slug == "qeni-i-baskervileve"), cancellationToken);
        var secondDirector = await EnsurePersonAsync(db, languages, "Kastriot Saqipi", now, cancellationToken);
        EnsureShowCredit(qeni, secondDirector, directorCreditType);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Person> EnsurePersonAsync(AppDbContext db, IReadOnlyDictionary<string, Language> languages, string fullName, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var person = await db.People
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.FullName == fullName, cancellationToken);

        if (person is null)
        {
            person = new Person { FullName = fullName, CreatedAt = now };
            db.People.Add(person);
        }

        person.UpdatedAt = now;
        EnsurePersonTranslation(person, languages[Sq].Id, "Regjisor/e", $"Biografi zhvillimore per {fullName}.");
        EnsurePersonTranslation(person, languages[En].Id, "Director", $"Development biography for {fullName}.");
        return person;
    }

    private static async Task EnsurePitfEditionAsync(
        AppDbContext db,
        IReadOnlyDictionary<string, Language> languages,
        IReadOnlyDictionary<string, MediaAsset> media,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var edition = await db.PitfEditions
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Year == 2024 && x.EditionNumber == 9, cancellationToken);

        if (edition is null)
        {
            edition = new PitfEdition { Year = 2024, EditionNumber = 9, CreatedAt = now };
            db.PitfEditions.Add(edition);
        }

        edition.LogoMediaAsset = media["logo"];
        edition.CoverMediaAsset = media["pitfCover"];
        edition.StartDate = new DateOnly(2024, 5, 20);
        edition.EndDate = new DateOnly(2024, 5, 26);
        edition.IsPublished = true;
        edition.IsFeatured = true;
        edition.UpdatedAt = now;

        EnsurePitfTranslation(edition, languages[Sq].Id, "Prishtina International Theatre Festival 2024", "pitf-2024", "Edicion i vecante i festivalit teatror nderkombetar.", "PITF 2024 mbledh shfaqje dhe artiste nga skena vendore dhe nderkombetare.", "PITF 2024", "Edicioni i vitit 2024 i Prishtina International Theatre Festival.");
        EnsurePitfTranslation(edition, languages[En].Id, "Prishtina International Theatre Festival 2024", "pitf-2024-en", "A featured edition of the international theatre festival.", "PITF 2024 gathers performances and artists from local and international stages.", "PITF 2024", "The 2024 edition of Prishtina International Theatre Festival.");

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureMediaTranslation(MediaAsset asset, int languageId, string altText)
    {
        var translation = asset.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new MediaAssetTranslation { LanguageId = languageId };
            asset.Translations.Add(translation);
        }

        translation.AltText = altText;
        translation.Caption = altText;
    }

    private static void EnsureLocationTranslation(Location location, int languageId, string name, string address)
    {
        var translation = location.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new LocationTranslation { LanguageId = languageId };
            location.Translations.Add(translation);
        }

        translation.Name = name;
        translation.Address = address;
    }

    private static void EnsureShowCategoryTranslation(ShowCategory category, int languageId, string name, string slug)
    {
        var translation = category.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new ShowCategoryTranslation { LanguageId = languageId };
            category.Translations.Add(translation);
        }

        translation.Name = name;
        translation.Slug = slug;
    }

    private static void EnsureCreditTypeTranslation(CreditType creditType, int languageId, string name)
    {
        var translation = creditType.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new CreditTypeTranslation { LanguageId = languageId };
            creditType.Translations.Add(translation);
        }

        translation.Name = name;
    }

    private static void EnsureTheatreInfoTranslation(TheatreInformation theatreInfo, int languageId, string theatreName, string heroSlogan, string aboutShort, string aboutFull, string pitfShortDescription, string addressDisplayText, string reservationTitle, string reservationText, string metaTitle, string metaDescription)
    {
        var translation = theatreInfo.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new TheatreInformationTranslation { LanguageId = languageId };
            theatreInfo.Translations.Add(translation);
        }

        translation.TheatreName = theatreName;
        translation.HeroSlogan = heroSlogan;
        translation.AboutShort = aboutShort;
        translation.AboutFull = aboutFull;
        translation.PitfShortDescription = pitfShortDescription;
        translation.AddressDisplayText = addressDisplayText;
        translation.ReservationCallToActionTitle = reservationTitle;
        translation.ReservationCallToActionText = reservationText;
        translation.MetaTitle = metaTitle;
        translation.MetaDescription = metaDescription;
    }

    private static void EnsureShowTranslation(Show show, int languageId, string title, string slug, string shortDescription, string fullDescription, string metaTitle, string metaDescription)
    {
        var translation = show.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new ShowTranslation { LanguageId = languageId };
            show.Translations.Add(translation);
        }

        translation.Title = title;
        translation.Slug = slug;
        translation.ShortDescription = shortDescription;
        translation.FullDescription = fullDescription;
        translation.MetaTitle = metaTitle;
        translation.MetaDescription = metaDescription;
    }

    private static void EnsurePersonTranslation(Person person, int languageId, string professionalTitle, string biography)
    {
        var translation = person.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new PersonTranslation { LanguageId = languageId };
            person.Translations.Add(translation);
        }

        translation.ProfessionalTitle = professionalTitle;
        translation.Biography = biography;
    }

    private static void EnsureShowCredit(Show show, Person person, CreditType creditType)
    {
        if (show.Credits.Any(x => x.Person == person || x.PersonId == person.Id))
        {
            return;
        }

        show.Credits.Add(new ShowCredit
        {
            Person = person,
            CreditType = creditType,
            DisplayOrder = show.Credits.Count + 1
        });
    }

    private static void EnsurePerformance(Show show, Location location, DateTimeOffset startDateTimeUtc, PerformanceStatus status, string? ticketUrl)
    {
        if (show.Performances.Any(x => x.StartDateTimeUtc == startDateTimeUtc && x.Status == status))
        {
            return;
        }

        show.Performances.Add(new ShowPerformance
        {
            Location = location,
            StartDateTimeUtc = startDateTimeUtc,
            Status = status,
            TicketUrl = ticketUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void EnsurePitfTranslation(PitfEdition edition, int languageId, string title, string slug, string shortDescription, string fullDescription, string metaTitle, string metaDescription)
    {
        var translation = edition.Translations.FirstOrDefault(x => x.LanguageId == languageId);
        if (translation is null)
        {
            translation = new PitfEditionTranslation { LanguageId = languageId };
            edition.Translations.Add(translation);
        }

        translation.Title = title;
        translation.Slug = slug;
        translation.ShortDescription = shortDescription;
        translation.FullDescription = fullDescription;
        translation.MetaTitle = metaTitle;
        translation.MetaDescription = metaDescription;
    }

    private static long? GetFileSize(IWebHostEnvironment environment, string fileUrl)
    {
        var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.Combine(environment.WebRootPath, relativePath);
        return File.Exists(path) ? new FileInfo(path).Length : null;
    }

    private sealed record MediaDefinition(string Key, string FileUrl, string FileName, string MimeType, int Width, int Height, string AltTextSq, string AltTextEn);
    private sealed record ShowDefinition(string Slug, string PosterKey, int DurationMinutes, bool IsFeatured, string TitleSq, string TitleEn, string ShortSq, string ShortEn, string DirectorName, int[] FuturePerformanceOffsets);
}
