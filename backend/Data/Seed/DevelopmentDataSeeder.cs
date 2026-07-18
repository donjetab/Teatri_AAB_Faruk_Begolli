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
        var creditTypes = await EnsureCreditTypesAsync(db, languages, cancellationToken);

        await EnsureTheatreInformationAsync(db, languages, media, now, cancellationToken);
        await EnsureShowsAsync(db, languages, media, category, creditTypes, location, now, cancellationToken);
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
            new MediaDefinition("rrenaPoster", "/uploads/dev/shows/rrena-poster.jpg", "rrena-poster.jpg", "image/jpeg", 900, 1200, "Poster mungon per shfaqjen Rrena", "Missing poster placeholder for the show Rrena"),
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

    private static async Task<Dictionary<string, CreditType>> EnsureCreditTypesAsync(AppDbContext db, IReadOnlyDictionary<string, Language> languages, CancellationToken cancellationToken)
    {
        var existing = await db.CreditTypes
            .Include(x => x.Translations)
            .ToDictionaryAsync(x => x.Code, cancellationToken);
        var definitions = new[]
        {
            ("director", "Regjia", "Director"), ("cast", "Luajnë", "Cast"), ("author", "Autor/e", "Author"),
            ("producer", "Producent/e", "Producer"), ("dramaturge", "Dramaturg/e", "Dramaturge"),
            ("music", "Muzika", "Music"), ("orchestration", "Orkestrimi", "Orchestration"),
            ("scenography", "Skenografia", "Scenography"), ("costumes", "Kostumet", "Costumes"),
            ("lighting", "Ndriçimi", "Lighting"), ("sound", "Toni/Zërimi", "Sound"),
            ("organizer", "Organizator/e", "Organizer"), ("coordinator", "Koordinator/e", "Coordinator"),
            ("choreography", "Koreografia", "Choreography"), ("translation", "Përkthimi", "Translation"),
            ("design", "Dizajni", "Design"), ("photography", "Fotografia", "Photography"),
            ("makeup", "Grimi", "Makeup"), ("adaptation", "Adaptimi", "Adaptation"),
            ("illustration", "Ilustrimi", "Illustration"), ("art-director", "Drejtor artistik", "Art director"),
            ("technical", "Realizimi teknik", "Technical realization")
        };

        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            if (!existing.TryGetValue(definition.Item1, out var creditType))
            {
                creditType = new CreditType { Code = definition.Item1 };
                db.CreditTypes.Add(creditType);
                existing[definition.Item1] = creditType;
            }

            creditType.DisplayOrder = index + 1;
            EnsureCreditTypeTranslation(creditType, languages[Sq].Id, definition.Item2);
            EnsureCreditTypeTranslation(creditType, languages[En].Id, definition.Item3);
        }

        await db.SaveChangesAsync(cancellationToken);
        return existing;
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
            "Një skenë ku historitë marrin jetë, emocionet na bashkojnë dhe trashëgimia vazhdon.",
            "Teatri AAB “Faruk Begolli” është themeluar në vitin 2015, si teatër i pavarur. Ёshtë një teatër me të gjitha komoditetet, i rrallë dhe i veçantë për hapësirën kosovare, i cili funksionon në kuadër të kampusit universitar të Kolegjit AAB, në qytetin e Prishtinës, Kosovë.",
            "Teatri AAB \"Faruk Begolli\" kultivon repertor bashkekohor, bashkepunime artistike dhe ngjarje teatrore per publikun ne Kosove.",
            "Festivali Ndërkombëtar i Teatrit “Prishtina International Theater Festival” u themelua në ambientet e Teatrit AAB “Faruk Begolli” në qershor të vitit 2017. Ky teatër vepron brenda Kolegjit AAB në Prishtinë.",
            "Prishtine, Kosove",
            "Rezervo vendin tënd tani",
            "Zgjidh shfaqjen dhe siguro vendin per mbremjen e radhes ne teatrin tone.",
            "Teatri AAB Faruk Begolli",
            "Faqja zyrtare e Teatrit AAB Faruk Begolli ne Kosove.");

        EnsureTheatreInfoTranslation(
            theatreInfo,
            languages[En].Id,
            "AAB Theatre \"Faruk Begolli\"",
            "A stage where stories come to life, emotions bring us together, and the legacy lives on.",
            "The AAB “Faruk Begolli” Theater was founded in 2015, as an independent theater. It is a theater with all the amenities, rare and special for the Kosovar space, which operates within the university campus of the AAB College, in the city of Pristina, Kosovo.",
            "AAB Theatre \"Faruk Begolli\" develops contemporary repertoire, artistic collaborations, and theatre events for audiences in Kosovo.",
            "The International Theater Festival “Prishtina International Theater Festival” was founded in the premises of the AAB Theater “Faruk Begolli” in June 2017. This theater operates within the AAB College in Pristina.",
            "Pristina, Kosovo",
            "Reserve your seat now",
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
        IReadOnlyDictionary<string, CreditType> creditTypes,
        Location location,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var shows = new[]
        {
            Show("profesor-jam-talent", null, "Profesor, jam talent...", new(2016, 3, 31), "“Profesor, jam talent…” është një komedi e gjallë që paraqet të rinj ambiciozë, të cilët përpiqen të dëshmojnë talentin e tyre në aktrim. Përmes audicioneve, ushtrimeve teatrale, personazheve të ekzagjeruara dhe keqkuptimeve komike, shfaqja trajton me humor ëndrrat, pasiguritë dhe rivalitetin mes aktorëve të rinj. Në thelb, ajo promovon talentin e ri dhe rëndësinë e teatrit në zhvillimin e krijimtarisë dhe jetës kulturore.",
                C("director", "Luan Daka"), C("cast", "Alban Zogaj", "Gani Morina", "Edita Dula", "Arta Lahu", "Valmir Krasniqi"), C("sound", "Amir Petrovci"), C("lighting", "Amir Petrovci"), C("coordinator", "Agnesa Bajgora")),
            Show("dy-gjitare-enderrimtare", null, "Dy gjitarë ëndërrimtarë", new(2016, 4, 15), "Maçok Mustaçoku e Minuk Bishtolli, të frymëzuar nga kënga e bukur e fëmijëve shkollarë, vetë, pa asnjë ditë shkolle, ëndërrojnë se janë bërë shkencëtarë të famshëm, por realiteti i kthjell. Ata e kuptojnë se pa dije, sinqeritet dhe punë nuk arrihet asgjë.",
                C("director", "Melihate Qena"), C("author", "Shaip Grabovci"), C("cast", "Edon Bërveniku", "Qëndresa Kajtazi", "Roza Berisha Fejzullahu"), C("music", "Alzan Gashi"), C("orchestration", "Ylber Krasniqi"), C("scenography", "Merita Behluli"), C("costumes", "Merita Behluli", "Ridvan Lahi"), C("lighting", "Asllan Hyseni")),
            Show("per-caj-te-hillary", null, "Për çaj të Hillary", new(2016, 5, 30), "Pasdite e këndshme në Shtëpinë e Bardhë. Zonja e Parë, Hillary, ka ftuar për çaj një grua jo fort në zë të mirë, Monikën. Aty nis e tëra. Gjakrat vlojnë ndërsa ato farkohen mbi kudhrën e përvojës. Mediat e kanë nuhatur se aty diçka po zihet. Është e habitshme të mendosh se ç'mund të ndodhë prapa dyerve të mbyllura dhe dritareve të hapura të shtëpisë më të famshme të kryeqytetit amerikan.",
                C("director", "Burbuqe Berisha"), C("cast", "Shengyl Ismaili", "Rabije Rozi Kryeziu"), C("author", "Johan Mucci"), C("music", "Trimor Dhomi"), C("costumes", "Linda Metaj Tafa"), C("scenography", "Yll Selmani"), C("choreography", "Rudina Berdynaj-Jakupi"), C("lighting", "Asllan Hyseni"), C("translation", "Gazmend Bërlajolli")),
            Show("bretkosa", "bretkosaPoster", "Bretkosa", new(2018, 12, 26), "“Bretkosa” është një dramë që trajton pasojat psikologjike dhe shoqërore të luftës në Kosovë. Përmes historive dhe përballjeve të personazheve, shfaqja paraqet trauma të pazgjidhura, dëshpërimin, vështirësitë ekonomike dhe mungesën e komunikimit në shoqërinë e pasluftës. Duke ndërthurur momente tragjike me humor të errët, ajo sjell një pasqyrim të dhimbshëm, por realist, të jetës në Kosovë dhe të njerëzve që vazhdojnë të luftojnë me të kaluarën e tyre.",
                C("director", "Burbuqe Berisha"), C("cast", "Ilir Tafa", "Valmir Krasniqi", "Basri Shala", "Lum Veseli"), C("dramaturge", "Seadet Beqiri"), C("author", "Dubravko Mihanovic"), C("organizer", "Amir Petrovci"), C("scenography", "Petrit Bakalli"), C("coordinator", "Agnesa Bajgora")),
            Show("hana-dhe-dielli", null, "Hana dhe Dielli", new(2020, 2, 15), "“Hana dhe Dielli” është një shfaqje edukative dhe argëtuese për fëmijë, e cila përmes aventurave të personazheve përcjell mesazhe për respektin, ndershmërinë dhe përgjegjësinë. Shfaqja i mëson fëmijët të respektojnë familjen, mësuesit dhe njëri-tjetrin, të ndihmojnë në shtëpi, të tregojnë të vërtetën dhe të kujdesen për sigurinë e tyre.",
                C("director", "Kaltërim Balaj"), C("cast", "Besnike Arifi", "Burim Koprani"), C("scenography", "Argjira Hoxha"), C("costumes", "Argjira Hoxha"), C("author", "Ekipi i Shfaqjes"), C("organizer", "Amir Petrovci"), C("sound", "Amir Petrovci"), C("lighting", "Amir Petrovci")),
            Show("brinat", null, "BRINAT", new(2021, 10, 21), "“Brinat” flet për raportet mes tre kolegëve dhe shefit të tyre, të cilët duke ia njohur të metat dhe karakterin njëri-tjetrit arrijnë t’i shkaktojnë presion dhe dhunë psikologjike njëri-tjetrit. Vepra trajton këtë gjendje, që ndikon në shkatërrimin e marrëdhënieve profesionale, dhe ngacmimi çon deri te momenti kur personazhet humbin toruan. Kjo situatë ndikon që të shpërfaqet edhe e kaluara e errët e personazheve të shfaqjes.",
                C("director", "Butrint Pasha"), C("cast", "Valmir Krasniqi", "Tristan Halilaj", "Arta Lahu", "Blin Sylejmani"), C("art-director", "Alma Krasniqi"), C("producer", "Ilir Bytyçi"), C("organizer", "Amir Petrovci"), C("sound", "Gëzim Hyseni"), C("lighting", "Gëzim Hyseni"), C("design", "Lirigëzon Selimi"), C("photography", "Kushtrim Tërnava"), C("translation", "Butrint Pasha"), C("music", "Butrint Pasha")),
            Show("tjetri", null, "Tjetri", new(2024, 3, 7), "“Tjetri” trajton me mjeshtëri problemet e përhapura si abuzimi me pushtetin, ngacmimi seksual, racizmi dhe bullizmi. Përmes skenave të saj prekëse, shfaqja vuri në dukje sfidat me të cilat përballen grupet e margjinalizuara, veçanërisht gratë dhe pakicat kombëtare. Duke përdorur formatin e forum teatrit, narrativa angazhoi në mënyrë efektive audiencën, duke i lejuar ata të hyjnë në këpucët e personazheve dhe të diskutojnë çështjet e paraqitura në skenë.",
                C("author", "Valmira Thaqi"), C("director", "Kreshnike Osmani"), C("cast", "Ryva Kajtazi", "Avni Dalipi", "Vedat Haxhiislami", "Alban Goranci", "Fitore Rama", "Ammar Havziji", "Florenta Bajraktari"), C("music", "Memli Kelmendi"), C("scenography", "Artan Hasani"), C("costumes", "Elma Azemi"), C("lighting", "Amir Petrovci"), C("makeup", "Zeprostudio"), C("producer", "Valmira Thaqi", "Ilir Bytyçi")),
            Show("qeni-i-baskervileve", "qeniPoster", "Qeni i Baskërvilëve", new(2024, 2, 29), "“Qeni i Baskervileve” është një shfaqje misterioze e bazuar në veprën e Arthur Conan Doyle. Sherlock Holmes dhe Dr. Watson hetojnë vdekjen e pazakontë të Sir Charles Baskerville, e cila lidhet me legjendën e një qeni të frikshëm që thuhet se ndjek familjen Baskerville. Mes dyshimeve, sekreteve dhe ngjarjeve të errëta, ata përpiqen të zbulojnë nëse familja kërcënohet nga një mallkim i mbinatyrshëm apo nga një plan i mirëmenduar kriminal.",
                C("director", "Jon Saqipi", "Kastriot Saqipi"), C("cast", "Blerina Veseli", "Djellza Dema", "Bleron Hevziu"), C("costumes", "Adelinë Mëziu"), C("scenography", "Artan Hasani"), C("lighting", "Amir Petrovci"), C("coordinator", "Agnesa Bajgora"), C("art-director", "Ilir Bytyçi")),
            Show("venera-ne-gezof", null, "Venera në Gëzof", new(2024, 5, 2), "“Venera në gëzof” është një dramë provokuese që zhvillohet gjatë audicionit të një aktoreje të quajtur Vanda për rolin kryesor në shfaqjen e regjisorit Thomas. Ndërsa prova vazhdon, kufiri mes personazheve dhe realitetit fillon të zbehet, ndërsa raporti i pushtetit mes tyre ndryshon vazhdimisht. Përmes humorit të errët dhe tensionit psikologjik, shfaqja trajton dominimin, dëshirën, identitetin dhe marrëdhëniet mes burrit dhe gruas.",
                C("director", "Agon Myftari"), C("cast", "Albina Krasniqi", "Redon Kika"), C("author", "David Ives"), C("producer", "Ilir Bytyçi"), C("organizer", "Amir Petrovci"), C("design", "Jeta Veseli"), C("coordinator", "Besike Arifi"), C("sound", "Amir Petrovci"), C("lighting", "Amir Petrovci")),
            Show("gjirafa-dhe-buburreci", "gjirafaPoster", "Gjirafa dhe Buburreci", new(2024, 10, 25), "“Gjirafa dhe Buburreci” është një shfaqje për fëmijë që sjell në skenë botën e kafshëve përmes humorit, imagjinatës dhe aventurave argëtuese. Përmes dy personazheve shumë të ndryshme, shfaqja përcjell mesazhe për miqësinë, mirëkuptimin, pranimin e dallimeve dhe rëndësinë e bashkëpunimit. Ajo synon t’i argëtojë fëmijët, duke i nxitur njëkohësisht të tregojnë respekt dhe dashamirësi ndaj njëri-tjetrit.",
                C("director", "Ben Apolloni"), C("cast", "Arbesa Azemi", "Erza Sejdiu"), C("producer", "Ilir Bytyçi"), C("costumes", "Xheneta Krasniqi"), C("scenography", "Xheneta Krasniqi"), C("music", "Ben Apolloni"), C("organizer", "Amir Petrovci"), C("coordinator", "Agnesa Bajgora"), C("lighting", "Altin Mehmeti"), C("sound", "Altin Mehmeti")),
            Show("rrena", "rrenaPoster", "Rrèna", new(2024, 12, 5), "“Rrèna” nga Florian Zeller është një komedi e mprehtë dhe plot humor që eksploron kompleksitetin e së vërtetës dhe mashtrimit në marrëdhënie martesore. Ajo trajton temën e dukjes, fasadës dhe iluzionit përballë thelbit, brendësisë dhe realitetit. Teksa mbrëmja zhvillohet gjatë një darke mes dy çifteve, tensionet rriten, kufijtë mjegullohen dhe keqkuptimet e çuditshme pasojnë, duke lënë të gjithë të pyesin për rolin e gënjeshtrave në dashuri dhe nëse ndershmëria është gjithmonë politika më e mirë. Me humor therës dhe dialog të zgjuar, drama ofron një eksplorim argëtues të besimit, tradhtisë dhe ekuilibrit delikat që mban marrëdhëniet së bashku. Audienca mund të presë një mbrëmje plot të qeshura, kthesa të papritura dhe mendime që nxisin reflektim.",
                C("director", "Agon Myftari"), C("author", "Florian Zeller"), C("cast", "Blend Sadiku", "Vjosë Tasholli", "Agan Asllani", "Albina Krasniqi"), C("producer", "Ilir Bytyçi", "Agon Myftari"), C("scenography", "Doruntina Bislimi", "Fiona Beqiri", "Tringa Blakaj"), C("costumes", "Doruntina Bislimi"), C("translation", "Qerim Ondozi"), C("lighting", "Altin Mehmeti"), C("sound", "Altin Mehmeti"), C("organizer", "Amir Petrovci"), C("coordinator", "Agnesa Bajgora"), C("photography", "Manushaqe Ibrahimi"), C("design", "Dardan Luta")),
            Show("mersieri-dhe-kamieri", null, "Mersieri dhe Kamieri", new(2024, 12, 19), "Dy heronjtë tanë, Mersieri dhe Kamieri, pas shumë hezitimesh nisen me kurajë në një udhëtim të paqartë dhe pa adresë, vetëm që të largohen përfundimisht nga qyteti. Ata janë njerëz të pavullnetshëm në një shfaqje që nuk e kuptojnë. Një çadër, një çantë, një mushama dhe një biçikletë janë garanci për këtë udhëtim të pafund, që del të jetë zbulues për vetminë e tyre të dëshpëruar. Me pak bukë, pak mallkim, pak prostitucion, pak sëmundje e pak metafizikë, kjo shfaqje zhvesh një të vërtetë kritike mbi njeriun modern.",
                C("director", "Butrint Pasha"), C("author", "Samuel Beckett"), C("cast", "Zhaneta Xhemajli", "Blin Mani"), C("producer", "Ilir Bytyqi", "Zhaneta Xhemajli"), C("adaptation", "Zhaneta Xhemajli"), C("music", "Butrint Pasha"), C("lighting", "Altin Mehmeti"), C("organizer", "Amir Petrovci"), C("coordinator", "Agnesa Bajgora"), C("design", "Vlerë Ibrahimi"), C("illustration", "Diella Valla"))
        };

        foreach (var definition in shows)
        {
            var previousSlug = definition.Slug == "dy-gjitare-enderrimtare" ? "macok-mustacoku-dhe-minuk-bishtolli" : null;
            var show = await db.Shows
                .Include(x => x.Translations)
                .Include(x => x.Credits)
                .Include(x => x.Performances)
                .FirstOrDefaultAsync(x => x.Translations.Any(t =>
                    t.LanguageId == languages[Sq].Id &&
                    (t.Slug == definition.Slug || (previousSlug != null && t.Slug == previousSlug))), cancellationToken);

            if (show is null)
            {
                show = new Show { CreatedAt = now };
                db.Shows.Add(show);
            }

            show.ShowCategory = category;
            show.PosterMediaAsset = definition.PosterKey is null ? null : media[definition.PosterKey];
            show.DurationMinutes = null;
            show.PremiereDate = definition.PremiereDate;
            show.Status = ShowStatus.Published;
            show.IsFeatured = definition.Slug == "bretkosa";
            show.PublishedAt ??= now;
            show.UpdatedAt = now;

            var shortDescription = definition.Synopsis.Length <= 320 ? definition.Synopsis : $"{definition.Synopsis[..317]}...";
            EnsureShowTranslation(show, languages[Sq].Id, definition.Title, definition.Slug, shortDescription, definition.Synopsis, definition.Title, shortDescription);
            EnsureShowTranslation(show, languages[En].Id, definition.Title, $"{definition.Slug}-en", shortDescription, definition.Synopsis, definition.Title, shortDescription);

            var displayOrder = 1;
            foreach (var credit in definition.Credits)
            {
                foreach (var name in credit.Names)
                {
                    var person = await EnsurePersonAsync(db, languages, name, creditTypes[credit.TypeCode], now, cancellationToken);
                    EnsureShowCredit(show, person, creditTypes[credit.TypeCode], displayOrder++);
                }
            }

            if (definition.Slug == "bretkosa")
            {
                var incorrectPremieres = show.Performances
                    .Where(x =>
                        x.Status == PerformanceStatus.Completed &&
                        x.StartDateTimeUtc == new DateTimeOffset(2016, 3, 31, 19, 0, 0, TimeSpan.Zero))
                    .ToList();
                db.ShowPerformances.RemoveRange(incorrectPremieres);
            }

            EnsurePerformance(show, location, new DateTimeOffset(definition.PremiereDate, TimeOnly.FromTimeSpan(TimeSpan.FromHours(19)), TimeSpan.Zero), PerformanceStatus.Completed, null);
        }

        await db.SaveChangesAsync(cancellationToken);

        static CreditDefinition C(string typeCode, params string[] names) => new(typeCode, names);
        static ShowDefinition Show(string slug, string? posterKey, string title, DateOnly premiereDate, string synopsis, params CreditDefinition[] credits) =>
            new(slug, posterKey, title, premiereDate, synopsis, credits);
    }

    private static async Task<Person> EnsurePersonAsync(AppDbContext db, IReadOnlyDictionary<string, Language> languages, string fullName, CreditType creditType, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var normalizedFullName = fullName.Trim();
        var person = db.People.Local
            .FirstOrDefault(x => string.Equals(x.FullName, normalizedFullName, StringComparison.OrdinalIgnoreCase));

        person ??= await db.People
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.FullName == normalizedFullName, cancellationToken);

        if (person is null)
        {
            person = new Person { FullName = normalizedFullName, CreatedAt = now };
            db.People.Add(person);
        }

        person.FullName = normalizedFullName;
        person.UpdatedAt = now;
        var sqTitle = creditType.Translations.First(x => x.LanguageId == languages[Sq].Id).Name;
        var enTitle = creditType.Translations.First(x => x.LanguageId == languages[En].Id).Name;
        EnsurePersonTranslation(person, languages[Sq].Id, sqTitle, null);
        EnsurePersonTranslation(person, languages[En].Id, enTitle, null);
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

        EnsurePitfTranslation(edition, languages[Sq].Id, "Prishtina International Theatre Festival 2024", "pitf-2024", "Festivali Ndërkombëtar i Teatrit “Prishtina International Theater Festival” u themelua në ambientet e Teatrit AAB “Faruk Begolli” në qershor të vitit 2017. Ky teatër vepron brenda Kolegjit AAB në Prishtinë", "PITF 2024 mbledh shfaqje dhe artiste nga skena vendore dhe nderkombetare.", "PITF 2024", "Edicioni i vitit 2024 i Prishtina International Theatre Festival.");
        EnsurePitfTranslation(edition, languages[En].Id, "Prishtina International Theatre Festival 2024", 
        "pitf-2024-en", 
        "The International Theater Festival “Prishtina International Theater Festival” was founded in the premises of the AAB Theater “Faruk Begolli” in June 2017. This theater operates within the AAB College in Pristina.", 
        "PITF 2024 gathers performances and artists from local and international stages.", "PITF 2024", "The 2024 edition of Prishtina International Theatre Festival.");

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

    private static void EnsurePersonTranslation(Person person, int languageId, string professionalTitle, string? biography)
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

    private static void EnsureShowCredit(Show show, Person person, CreditType creditType, int displayOrder)
    {
        if (show.Credits.Any(x =>
                (x.Person == person || (person.Id != 0 && x.PersonId == person.Id)) &&
                (x.CreditType == creditType || (creditType.Id != 0 && x.CreditTypeId == creditType.Id))))
        {
            return;
        }

        show.Credits.Add(new ShowCredit
        {
            Person = person,
            CreditType = creditType,
            DisplayOrder = displayOrder
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
    private sealed record ShowDefinition(string Slug, string? PosterKey, string Title, DateOnly PremiereDate, string Synopsis, CreditDefinition[] Credits);
    private sealed record CreditDefinition(string TypeCode, string[] Names);
}
