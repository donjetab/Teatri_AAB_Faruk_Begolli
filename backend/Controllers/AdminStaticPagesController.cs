using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "Admin")]
[Route("api/admin/pages")]
public sealed class AdminStaticPagesController(AppDbContext db, IClock clock) : ControllerBase
{
    private const string DefaultMapEmbedUrl = "https://www.google.com/maps/embed?pb=!1m14!1m8!1m3!1d11740.15791744908!2d21.112945!3d42.639323!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x13549f005d591c87%3A0x2473b114eef9fd14!2zVGVhdHJpIEFBQiDigJxGYXJ1ayBCZWdvbGxp4oCd!5e0!3m2!1sen!2sus!4v1785141268194!5m2!1sen!2sus";
    private const string DefaultMapLinkUrl = "https://www.google.com/maps/search/?api=1&query=42.6389837%2C21.1126562";
    private static readonly (string Key, string Sq, string En)[] Definitions =
    [
        ("about", "Për ne", "About"), ("contact", "Kontakti", "Contact"),
        ("shows-introduction", "Shfaqjet", "Shows"), ("news-introduction", "Lajme", "News"), ("reservations", "Rezervimet", "Reservations"),
        ("pitf-introduction", "PITF", "PITF"), ("gallery-introduction", "Galeria", "Gallery")
    ];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminStaticPageDto>>> List(CancellationToken token)
    {
        await EnsurePages(token);
        return Ok((await Query().OrderBy(x => x.Id).ToListAsync(token)).Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminStaticPageDto>> Get(int id, CancellationToken token)
    {
        var item = await Query().FirstOrDefaultAsync(x => x.Id == id, token);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminStaticPageDto>> Save(int id, SaveAdminStaticPageRequest request, CancellationToken token)
    {
        var item = await db.StaticPages.Include(x => x.Translations).ThenInclude(x => x.Language).FirstOrDefaultAsync(x => x.Id == id, token);
        if (item is null) return NotFound();
        if (request.Translations.Count(x => x.LanguageCode is "sq" or "en") != 2) return ValidationProblem("Albanian and English content are required.");
        foreach (var translation in request.Translations)
            if (string.IsNullOrWhiteSpace(translation.Title))
                return ValidationProblem($"The title is required in {translation.LanguageCode.ToUpperInvariant()}.");
        var imageIds = new[] { request.FeaturedMediaAssetId, request.ParallaxMediaAssetId, request.SocialSharingMediaAssetId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        if (imageIds.Length > 0 && await db.MediaAssets.CountAsync(x => imageIds.Contains(x.Id) && x.IsActive && x.MimeType.StartsWith("image/"), token) != imageIds.Length)
            return ValidationProblem("Featured and sharing media must be active images.");
        var sanitizer = new HtmlSanitizer();
        foreach (var incoming in request.Translations)
        {
            var target = item.Translations.First(x => x.Language.Code == incoming.LanguageCode);
            target.Title = incoming.Title.Trim(); target.Slug = item.PageKey;
            target.Content = sanitizer.Sanitize(incoming.Content ?? string.Empty); target.Subtitle = Clean(incoming.Subtitle);
            target.QuoteText = Clean(incoming.QuoteText); target.QuoteAuthor = Clean(incoming.QuoteAuthor);
            target.StatOneValue = Clean(incoming.StatOneValue); target.StatOneLabel = Clean(incoming.StatOneLabel);
            target.StatTwoValue = Clean(incoming.StatTwoValue); target.StatTwoLabel = Clean(incoming.StatTwoLabel);
            target.StatThreeValue = Clean(incoming.StatThreeValue); target.StatThreeLabel = Clean(incoming.StatThreeLabel);
            target.MetaTitle = Clean(incoming.MetaTitle); target.MetaDescription = Clean(incoming.MetaDescription);
        }
        item.FeaturedMediaAssetId = request.FeaturedMediaAssetId; item.ParallaxMediaAssetId = request.ParallaxMediaAssetId;
        item.MapEmbedUrl = Clean(request.MapEmbedUrl); item.MapLinkUrl = Clean(request.MapLinkUrl);
        item.SocialSharingMediaAssetId = request.SocialSharingMediaAssetId;
        item.IsPublished = request.IsPublished; item.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(token);
        return Ok(ToDto((await Query().FirstAsync(x => x.Id == id, token))));
    }

    private IQueryable<StaticPage> Query() => db.StaticPages.AsNoTracking().Include(x => x.Translations).ThenInclude(x => x.Language).Include(x => x.FeaturedMediaAsset).Include(x => x.ParallaxMediaAsset).Include(x => x.SocialSharingMediaAsset).AsSplitQuery();
    private async Task EnsurePages(CancellationToken token)
    {
        var pages = await db.StaticPages.Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        var obsoleteLegalPages = pages.Where(x => x.PageKey is "privacy-policy" or "terms" or "accessibility").ToList();
        if (obsoleteLegalPages.Count > 0)
        {
            db.StaticPages.RemoveRange(obsoleteLegalPages);
            await db.SaveChangesAsync(token);
            pages = pages.Except(obsoleteLegalPages).ToList();
        }
        var existing = pages.Select(x => x.PageKey).ToList();
        var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token);
        foreach (var definition in Definitions.Where(x => !existing.Contains(x.Key)))
            db.StaticPages.Add(new StaticPage { PageKey = definition.Key, IsPublished = false, CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow,
                Translations = languages.Select(language => new StaticPageTranslation { LanguageId = language.Id, Title = language.Code == "sq" ? definition.Sq : definition.En,
                    Slug = definition.Key, Content = "<p></p>" }).ToList() });
        await db.SaveChangesAsync(token);
        pages = await db.StaticPages.Include(x => x.Translations).ThenInclude(x => x.Language).ToListAsync(token);
        foreach (var page in pages)
        {
            page.IsPublished = true;
            if (page.PageKey == "contact")
            {
                page.MapEmbedUrl ??= DefaultMapEmbedUrl;
                page.MapLinkUrl ??= DefaultMapLinkUrl;
            }
            foreach (var translation in page.Translations)
                ApplyPublicDefaults(page.PageKey, translation);
        }
        await db.SaveChangesAsync(token);
    }

    private static void ApplyPublicDefaults(string pageKey, StaticPageTranslation item)
    {
        var en = item.Language.Code == "en";
        item.Slug = pageKey;
        item.Subtitle ??= pageKey switch
        {
            "news-introduction" => en ? "The latest events, performances and stories from AAB Theatre “Faruk Begolli”." : "Ngjarjet, shfaqjet dhe historitë më të fundit nga Teatri AAB “Faruk Begolli”.",
            "pitf-introduction" => "Prishtina International Theatre Festival",
            _ => en ? "A stage for emotions. A home for every story. A legacy for the future." : "Një skenë për emocionet. Një shtëpi për çdo histori. Një trashëgimi për të ardhmen."
        };
        if (pageKey != "about") return;
        if (string.IsNullOrWhiteSpace(item.Content) || item.Content == "<p></p>")
            item.Content = en
                ? "<p>AAB Theatre “Faruk Begolli” was founded in 2015 as an independent theatre. It is a rare and distinctive theatre space in Kosovo, operating within the university campus of AAB College in Prishtina.</p><p>The theatre bears the name of the emblematic actor Faruk Begolli and was inaugurated on 31.03.2015 with the performance “Jam talent...” directed by Luan Daka. Artists and theatre lovers warmly welcomed its opening and continue to support it.</p><p>The media have also supported the theatre by giving it important visibility as a noble cultural value for theatrical life in Kosovo, especially at a time when theatres around the world face declining audiences.</p>"
                : "<p>Teatri AAB “Faruk Begolli” është themeluar në vitin 2015, si teatër i pavarur. Është një teatër me të gjitha komoditetet, i rrallë dhe i veçantë për hapësirën kosovare, i cili funksionon në kuadër të kampusit universitar të Kolegjit AAB, në qytetin e Prishtinës, Kosovë.</p><p>Teatri mban emrin e aktorit emblematik Faruk Begolli, ndërsa është inauguruar më 31.03.2015 me shfaqjen “Jam talent...” me regji të Luan Dakës. Shumë artistë dhe artdashës e kanë mirëpritur hapjen e këtij teatri dhe vazhdojnë ta përkrahin maksimalisht.</p><p>Gjithashtu edhe mediat e kanë përkrahur duke i dhënë publicitet të madh, si një vlerë sublime dhe fisnike, për një jetë teatrore në Kosovë.</p>";
        item.QuoteText ??= en ? "To build a theatre such as “Faruk Begolli” Theatre is like building a mosque and a church, perhaps even harder. People will give money for mosques and churches, while theatres are built by kings or states." : "Ta ndërtosh një teatër, siç është Teatri “Faruk Begolli”, është si ta ndërtosh një xhami edhe një kishë, bile edhe më vështirë. Se për xhamia e kisha, njerëzit do të japin para, ndërsa teatrot i ndërtojnë mbretërit ose shtetet.";
        item.QuoteAuthor ??= "Enver Petrovci";
        item.StatOneValue ??= "2015"; item.StatOneLabel ??= en ? "Founded" : "Themelimi";
        item.StatTwoValue ??= "500+"; item.StatTwoLabel ??= en ? "Performances" : "Performanca";
        item.StatThreeValue ??= "100K+"; item.StatThreeLabel ??= en ? "Spectators" : "Spektatorë";
    }
    private static AdminStaticPageDto ToDto(StaticPage x) => new(x.Id, x.PageKey, x.FeaturedMediaAssetId, x.FeaturedMediaAsset?.FileUrl,
        x.ParallaxMediaAssetId, x.ParallaxMediaAsset?.FileUrl, x.MapEmbedUrl, x.MapLinkUrl,
        x.SocialSharingMediaAssetId, x.SocialSharingMediaAsset?.FileUrl, x.IsPublished, x.UpdatedAt,
        x.Translations.Select(t => new AdminStaticPageTranslationDto(t.Language.Code, t.Title, t.Slug, t.Content, t.Subtitle,
            t.QuoteText, t.QuoteAuthor, t.StatOneValue, t.StatOneLabel, t.StatTwoValue, t.StatTwoLabel,
            t.StatThreeValue, t.StatThreeLabel, t.MetaTitle, t.MetaDescription)).ToList());
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
