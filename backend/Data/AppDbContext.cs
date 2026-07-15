using Microsoft.EntityFrameworkCore;
using Theatre.Api.Models;

namespace Theatre.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<CreditType> CreditTypes => Set<CreditType>();
    public DbSet<CreditTypeTranslation> CreditTypeTranslations => Set<CreditTypeTranslation>();
    public DbSet<GalleryAlbum> GalleryAlbums => Set<GalleryAlbum>();
    public DbSet<GalleryAlbumMedia> GalleryAlbumMedia => Set<GalleryAlbumMedia>();
    public DbSet<GalleryAlbumTranslation> GalleryAlbumTranslations => Set<GalleryAlbumTranslation>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<LocationTranslation> LocationTranslations => Set<LocationTranslation>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaAssetTranslation> MediaAssetTranslations => Set<MediaAssetTranslation>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsArticleTranslation> NewsArticleTranslations => Set<NewsArticleTranslation>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<PersonTranslation> PersonTranslations => Set<PersonTranslation>();
    public DbSet<PitfEdition> PitfEditions => Set<PitfEdition>();
    public DbSet<PitfEditionTranslation> PitfEditionTranslations => Set<PitfEditionTranslation>();
    public DbSet<Show> Shows => Set<Show>();
    public DbSet<ShowCategory> ShowCategories => Set<ShowCategory>();
    public DbSet<ShowCategoryTranslation> ShowCategoryTranslations => Set<ShowCategoryTranslation>();
    public DbSet<ShowCredit> ShowCredits => Set<ShowCredit>();
    public DbSet<ShowPerformance> ShowPerformances => Set<ShowPerformance>();
    public DbSet<ShowTranslation> ShowTranslations => Set<ShowTranslation>();
    public DbSet<TheatreInformation> TheatreInformation => Set<TheatreInformation>();
    public DbSet<TheatreInformationTranslation> TheatreInformationTranslations => Set<TheatreInformationTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
