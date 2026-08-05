using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/{languageCode:regex(^(sq|en)$)}/pages")]
public sealed class StaticPagesController(AppDbContext db) : ControllerBase
{
    [HttpGet("{pageKey}")]
    public async Task<IActionResult> Get(string languageCode, string pageKey, CancellationToken token)
    {
        var defaultSocialImageUrl = await db.TheatreInformation.AsNoTracking()
            .Select(x => x.SocialSharingMediaAsset != null
                ? x.SocialSharingMediaAsset.FileUrl
                : x.LogoMediaAsset != null ? x.LogoMediaAsset.FileUrl
                : x.ReservationBannerMediaAsset != null ? x.ReservationBannerMediaAsset.FileUrl
                : x.HeroBackgroundMediaAsset != null ? x.HeroBackgroundMediaAsset.FileUrl
                : x.AboutPreviewMediaAsset != null ? x.AboutPreviewMediaAsset.FileUrl : null)
            .FirstOrDefaultAsync(token);
        var page = await db.StaticPages.AsNoTracking().Where(x => x.PageKey == pageKey && x.IsPublished)
            .Select(x => new
            {
                x.PageKey,
                Title = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.Title).FirstOrDefault(),
                Slug = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.Slug).FirstOrDefault(),
                Content = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.Content).FirstOrDefault(),
                Subtitle = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.Subtitle).FirstOrDefault(),
                QuoteText = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.QuoteText).FirstOrDefault(),
                QuoteAuthor = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.QuoteAuthor).FirstOrDefault(),
                Statistics = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => new[]
                {
                    new { Value = t.StatOneValue, Label = t.StatOneLabel }, new { Value = t.StatTwoValue, Label = t.StatTwoLabel },
                    new { Value = t.StatThreeValue, Label = t.StatThreeLabel }
                }).FirstOrDefault(),
                SeoTitle = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.MetaTitle).FirstOrDefault(),
                SeoDescription = x.Translations.Where(t => t.Language.Code == languageCode).Select(t => t.MetaDescription).FirstOrDefault(),
                FeaturedImageUrl = x.FeaturedMediaAsset == null ? null : x.FeaturedMediaAsset.FileUrl,
                ParallaxImageUrl = x.ParallaxMediaAsset == null ? null : x.ParallaxMediaAsset.FileUrl,
                x.MapEmbedUrl,
                x.MapLinkUrl,
                SocialSharingImageUrl = x.SocialSharingMediaAsset == null ? defaultSocialImageUrl : x.SocialSharingMediaAsset.FileUrl
            }).FirstOrDefaultAsync(token);
        return page is null ? NotFound() : Ok(page);
    }
}
