namespace Theatre.Api.DTOs;

public sealed record AdminStaticPageTranslationDto(string LanguageCode, string Title, string Slug, string Content, string? Subtitle,
    string? QuoteText, string? QuoteAuthor, string? StatOneValue, string? StatOneLabel, string? StatTwoValue, string? StatTwoLabel,
    string? StatThreeValue, string? StatThreeLabel, string? MetaTitle, string? MetaDescription);
public sealed record AdminStaticPageDto(int Id, string PageKey, int? FeaturedMediaAssetId, string? FeaturedImageUrl,
    int? ParallaxMediaAssetId, string? ParallaxImageUrl, string? MapEmbedUrl, string? MapLinkUrl,
    int? SocialSharingMediaAssetId, string? SocialSharingImageUrl, bool IsPublished, DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminStaticPageTranslationDto> Translations);
public sealed record SaveAdminStaticPageRequest(int? FeaturedMediaAssetId, int? ParallaxMediaAssetId, string? MapEmbedUrl, string? MapLinkUrl,
    int? SocialSharingMediaAssetId, bool IsPublished,
    IReadOnlyList<AdminStaticPageTranslationDto> Translations);
