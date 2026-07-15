namespace Theatre.Api.Models;

public sealed class Language
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Show
{
    public int Id { get; set; }
    public int ShowCategoryId { get; set; }
    public int? PosterMediaAssetId { get; set; }
    public int? DurationMinutes { get; set; }
    public ShowStatus Status { get; set; } = ShowStatus.Draft;
    public bool IsFeatured { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    public ShowCategory ShowCategory { get; set; } = null!;
    public MediaAsset? PosterMediaAsset { get; set; }
    public ICollection<ShowTranslation> Translations { get; set; } = [];
    public ICollection<ShowPerformance> Performances { get; set; } = [];
    public ICollection<ShowCredit> Credits { get; set; } = [];
    public ICollection<GalleryAlbum> GalleryAlbums { get; set; } = [];
}

public sealed class ShowTranslation
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public int LanguageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public Show Show { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class ShowCategory
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<Show> Shows { get; set; } = [];
    public ICollection<ShowCategoryTranslation> Translations { get; set; } = [];
}

public sealed class ShowCategoryTranslation
{
    public int Id { get; set; }
    public int ShowCategoryId { get; set; }
    public int LanguageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ShowCategory ShowCategory { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class ShowPerformance
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public int? LocationId { get; set; }
    public DateTimeOffset StartDateTimeUtc { get; set; }
    public string? TicketUrl { get; set; }
    public PerformanceStatus Status { get; set; } = PerformanceStatus.Scheduled;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Show Show { get; set; } = null!;
    public Location? Location { get; set; }
}

public sealed class Person
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? ProfileMediaAssetId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MediaAsset? ProfileMediaAsset { get; set; }
    public ICollection<PersonTranslation> Translations { get; set; } = [];
    public ICollection<ShowCredit> ShowCredits { get; set; } = [];
}

public sealed class PersonTranslation
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public int LanguageId { get; set; }
    public string? Biography { get; set; }
    public string? ProfessionalTitle { get; set; }

    public Person Person { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class CreditType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<CreditTypeTranslation> Translations { get; set; } = [];
    public ICollection<ShowCredit> ShowCredits { get; set; } = [];
}

public sealed class CreditTypeTranslation
{
    public int Id { get; set; }
    public int CreditTypeId { get; set; }
    public int LanguageId { get; set; }
    public string Name { get; set; } = string.Empty;

    public CreditType CreditType { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class ShowCredit
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public int PersonId { get; set; }
    public int CreditTypeId { get; set; }
    public string? CharacterName { get; set; }
    public int DisplayOrder { get; set; }

    public Show Show { get; set; } = null!;
    public Person Person { get; set; } = null!;
    public CreditType CreditType { get; set; } = null!;
}

public sealed class NewsArticle
{
    public int Id { get; set; }
    public int? CoverMediaAssetId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MediaAsset? CoverMediaAsset { get; set; }
    public ICollection<NewsArticleTranslation> Translations { get; set; } = [];
    public ICollection<GalleryAlbum> GalleryAlbums { get; set; } = [];
}

public sealed class NewsArticleTranslation
{
    public int Id { get; set; }
    public int NewsArticleId { get; set; }
    public int LanguageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public NewsArticle NewsArticle { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class PitfEdition
{
    public int Id { get; set; }
    public int EditionNumber { get; set; }
    public int Year { get; set; }
    public int? LogoMediaAssetId { get; set; }
    public int? CoverMediaAssetId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MediaAsset? LogoMediaAsset { get; set; }
    public MediaAsset? CoverMediaAsset { get; set; }
    public ICollection<PitfEditionTranslation> Translations { get; set; } = [];
    public ICollection<GalleryAlbum> GalleryAlbums { get; set; } = [];
}

public sealed class PitfEditionTranslation
{
    public int Id { get; set; }
    public int PitfEditionId { get; set; }
    public int LanguageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public PitfEdition PitfEdition { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class MediaAsset
{
    public int Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSize { get; set; }
    public DateTimeOffset? TakenAt { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MediaAssetTranslation> Translations { get; set; } = [];
    public ICollection<GalleryAlbumMedia> GalleryAlbumMedia { get; set; } = [];
}

public sealed class MediaAssetTranslation
{
    public int Id { get; set; }
    public int MediaAssetId { get; set; }
    public int LanguageId { get; set; }
    public string? AltText { get; set; }
    public string? Caption { get; set; }

    public MediaAsset MediaAsset { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class GalleryAlbum
{
    public int Id { get; set; }
    public GalleryAlbumType AlbumType { get; set; } = GalleryAlbumType.General;
    public DateOnly? EventDate { get; set; }
    public int? CoverMediaAssetId { get; set; }
    public int? ShowId { get; set; }
    public int? NewsArticleId { get; set; }
    public int? PitfEditionId { get; set; }
    public bool IsVisibleInGeneralGallery { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MediaAsset? CoverMediaAsset { get; set; }
    public Show? Show { get; set; }
    public NewsArticle? NewsArticle { get; set; }
    public PitfEdition? PitfEdition { get; set; }
    public ICollection<GalleryAlbumTranslation> Translations { get; set; } = [];
    public ICollection<GalleryAlbumMedia> GalleryAlbumMedia { get; set; } = [];
}

public sealed class GalleryAlbumTranslation
{
    public int Id { get; set; }
    public int GalleryAlbumId { get; set; }
    public int LanguageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;

    public GalleryAlbum GalleryAlbum { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class GalleryAlbumMedia
{
    public int GalleryAlbumId { get; set; }
    public int MediaAssetId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCover { get; set; }
    public bool IsFeatured { get; set; }

    public GalleryAlbum GalleryAlbum { get; set; } = null!;
    public MediaAsset MediaAsset { get; set; } = null!;
}

public sealed class TheatreInformation
{
    public int Id { get; set; }
    public int FoundedYear { get; set; }
    public int PerformancesCount { get; set; }
    public int SpectatorsCount { get; set; }
    public int? HeroBackgroundMediaAssetId { get; set; }
    public int? AboutPreviewMediaAssetId { get; set; }
    public int? ReservationBannerMediaAssetId { get; set; }
    public int? PitfFeatureMediaAssetId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? ReservationUrl { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MediaAsset? HeroBackgroundMediaAsset { get; set; }
    public MediaAsset? AboutPreviewMediaAsset { get; set; }
    public MediaAsset? ReservationBannerMediaAsset { get; set; }
    public MediaAsset? PitfFeatureMediaAsset { get; set; }
    public ICollection<TheatreInformationTranslation> Translations { get; set; } = [];
}

public sealed class TheatreInformationTranslation
{
    public int Id { get; set; }
    public int TheatreInformationId { get; set; }
    public int LanguageId { get; set; }
    public string TheatreName { get; set; } = string.Empty;
    public string HeroSlogan { get; set; } = string.Empty;
    public string AboutShort { get; set; } = string.Empty;
    public string AboutFull { get; set; } = string.Empty;
    public string PitfShortDescription { get; set; } = string.Empty;
    public string AddressDisplayText { get; set; } = string.Empty;
    public string ReservationCallToActionTitle { get; set; } = string.Empty;
    public string ReservationCallToActionText { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public TheatreInformation TheatreInformation { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class Location
{
    public int Id { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<LocationTranslation> Translations { get; set; } = [];
    public ICollection<ShowPerformance> Performances { get; set; } = [];
}

public sealed class LocationTranslation
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public int LanguageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public Location Location { get; set; } = null!;
    public Language Language { get; set; } = null!;
}

public sealed class ContactMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class NewsletterSubscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PreferredLanguageCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset SubscribedAt { get; set; }
    public DateTimeOffset? UnsubscribedAt { get; set; }
}
