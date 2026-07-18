using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Theatre.Api.Models;

namespace Theatre.Api.Data.Configurations;

internal sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsDefault)
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        builder.HasData(
            new Language { Id = 1, Code = "sq", Name = "Albanian", IsDefault = true, IsActive = true },
            new Language { Id = 2, Code = "en", Name = "English", IsDefault = false, IsActive = true });
    }
}

internal sealed class ShowConfiguration : IEntityTypeConfiguration<Show>
{
    public void Configure(EntityTypeBuilder<Show> builder)
    {
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.PremiereDate).HasColumnType("date");

        builder.HasOne(x => x.ShowCategory)
            .WithMany(x => x.Shows)
            .HasForeignKey(x => x.ShowCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PosterMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.PosterMediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.PremiereDate);
    }
}

internal sealed class ShowTranslationConfiguration : IEntityTypeConfiguration<ShowTranslation>
{
    public void Configure(EntityTypeBuilder<ShowTranslation> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(600).IsRequired();
        builder.Property(x => x.FullDescription).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(220);
        builder.Property(x => x.MetaDescription).HasMaxLength(320);

        builder.HasOne(x => x.Show)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ShowId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => new { x.LanguageId, x.Slug }).IsUnique();
    }
}

internal sealed class ShowCategoryConfiguration : IEntityTypeConfiguration<ShowCategory>
{
    public void Configure(EntityTypeBuilder<ShowCategory> builder)
    {
        builder.HasIndex(x => x.DisplayOrder);
    }
}

internal sealed class ShowCategoryTranslationConfiguration : IEntityTypeConfiguration<ShowCategoryTranslation>
{
    public void Configure(EntityTypeBuilder<ShowCategoryTranslation> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(160).IsRequired();

        builder.HasOne(x => x.ShowCategory)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.ShowCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ShowCategoryId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => new { x.LanguageId, x.Slug }).IsUnique();
    }
}

internal sealed class ShowPerformanceConfiguration : IEntityTypeConfiguration<ShowPerformance>
{
    public void Configure(EntityTypeBuilder<ShowPerformance> builder)
    {
        builder.Property(x => x.TicketUrl).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne(x => x.Show)
            .WithMany(x => x.Performances)
            .HasForeignKey(x => x.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.Performances)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.StartDateTimeUtc);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ShowId);
    }
}

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(180).IsRequired();

        builder.HasOne(x => x.ProfileMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.ProfileMediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.FullName).IsUnique();
    }
}

internal sealed class PersonTranslationConfiguration : IEntityTypeConfiguration<PersonTranslation>
{
    public void Configure(EntityTypeBuilder<PersonTranslation> builder)
    {
        builder.Property(x => x.ProfessionalTitle).HasMaxLength(160);

        builder.HasOne(x => x.Person)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PersonId, x.LanguageId }).IsUnique();
    }
}

internal sealed class CreditTypeConfiguration : IEntityTypeConfiguration<CreditType>
{
    public void Configure(EntityTypeBuilder<CreditType> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.DisplayOrder);
    }
}

internal sealed class CreditTypeTranslationConfiguration : IEntityTypeConfiguration<CreditTypeTranslation>
{
    public void Configure(EntityTypeBuilder<CreditTypeTranslation> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();

        builder.HasOne(x => x.CreditType)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.CreditTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CreditTypeId, x.LanguageId }).IsUnique();
    }
}

internal sealed class ShowCreditConfiguration : IEntityTypeConfiguration<ShowCredit>
{
    public void Configure(EntityTypeBuilder<ShowCredit> builder)
    {
        builder.Property(x => x.CharacterName).HasMaxLength(180);

        builder.HasOne(x => x.Show)
            .WithMany(x => x.Credits)
            .HasForeignKey(x => x.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Person)
            .WithMany(x => x.ShowCredits)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreditType)
            .WithMany(x => x.ShowCredits)
            .HasForeignKey(x => x.CreditTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ShowId, x.DisplayOrder });
        builder.HasIndex(x => x.PersonId);
        builder.HasIndex(x => x.CreditTypeId);
    }
}

internal sealed class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.HasOne(x => x.CoverMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.CoverMediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.IsPublished);
        builder.HasIndex(x => x.IsFeatured);
    }
}

internal sealed class NewsArticleTranslationConfiguration : IEntityTypeConfiguration<NewsArticleTranslation>
{
    public void Configure(EntityTypeBuilder<NewsArticleTranslation> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(700).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(220);
        builder.Property(x => x.MetaDescription).HasMaxLength(320);

        builder.HasOne(x => x.NewsArticle)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.NewsArticleId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => new { x.LanguageId, x.Slug }).IsUnique();
    }
}

internal sealed class PitfEditionConfiguration : IEntityTypeConfiguration<PitfEdition>
{
    public void Configure(EntityTypeBuilder<PitfEdition> builder)
    {
        builder.HasOne(x => x.LogoMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.LogoMediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CoverMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.CoverMediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Year);
        builder.HasIndex(x => new { x.Year, x.EditionNumber }).IsUnique();
    }
}

internal sealed class PitfEditionTranslationConfiguration : IEntityTypeConfiguration<PitfEditionTranslation>
{
    public void Configure(EntityTypeBuilder<PitfEditionTranslation> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(700).IsRequired();
        builder.Property(x => x.FullDescription).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(220);
        builder.Property(x => x.MetaDescription).HasMaxLength(320);

        builder.HasOne(x => x.PitfEdition)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.PitfEditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PitfEditionId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => new { x.LanguageId, x.Slug }).IsUnique();
    }
}

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.Property(x => x.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(120).IsRequired();

        builder.HasIndex(x => x.FileUrl);
        builder.HasIndex(x => x.UploadedAt);
        builder.HasIndex(x => x.IsActive);
    }
}

internal sealed class MediaAssetTranslationConfiguration : IEntityTypeConfiguration<MediaAssetTranslation>
{
    public void Configure(EntityTypeBuilder<MediaAssetTranslation> builder)
    {
        builder.Property(x => x.AltText).HasMaxLength(250);
        builder.Property(x => x.Caption).HasMaxLength(500);

        builder.HasOne(x => x.MediaAsset)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MediaAssetId, x.LanguageId }).IsUnique();
    }
}

internal sealed class GalleryAlbumConfiguration : IEntityTypeConfiguration<GalleryAlbum>
{
    public void Configure(EntityTypeBuilder<GalleryAlbum> builder)
    {
        builder.Property(x => x.AlbumType).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_GalleryAlbums_AlbumType_Parent",
            "([AlbumType] = 'General' AND [ShowId] IS NULL AND [NewsArticleId] IS NULL AND [PitfEditionId] IS NULL) OR " +
            "([AlbumType] = 'Show' AND [ShowId] IS NOT NULL AND [NewsArticleId] IS NULL AND [PitfEditionId] IS NULL) OR " +
            "([AlbumType] = 'NewsArticle' AND [ShowId] IS NULL AND [NewsArticleId] IS NOT NULL AND [PitfEditionId] IS NULL) OR " +
            "([AlbumType] = 'PitfEdition' AND [ShowId] IS NULL AND [NewsArticleId] IS NULL AND [PitfEditionId] IS NOT NULL)"));

        builder.HasOne(x => x.CoverMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.CoverMediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Show)
            .WithMany(x => x.GalleryAlbums)
            .HasForeignKey(x => x.ShowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.NewsArticle)
            .WithMany(x => x.GalleryAlbums)
            .HasForeignKey(x => x.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PitfEdition)
            .WithMany(x => x.GalleryAlbums)
            .HasForeignKey(x => x.PitfEditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.EventDate);
        builder.HasIndex(x => x.AlbumType);
        builder.HasIndex(x => x.ShowId);
        builder.HasIndex(x => x.NewsArticleId);
        builder.HasIndex(x => x.PitfEditionId);
        builder.HasIndex(x => x.IsVisibleInGeneralGallery);
        builder.HasIndex(x => x.IsPublished);
    }
}

internal sealed class GalleryAlbumTranslationConfiguration : IEntityTypeConfiguration<GalleryAlbumTranslation>
{
    public void Configure(EntityTypeBuilder<GalleryAlbumTranslation> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(700);
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();

        builder.HasOne(x => x.GalleryAlbum)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.GalleryAlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GalleryAlbumId, x.LanguageId }).IsUnique();
        builder.HasIndex(x => new { x.LanguageId, x.Slug }).IsUnique();
    }
}

internal sealed class GalleryAlbumMediaConfiguration : IEntityTypeConfiguration<GalleryAlbumMedia>
{
    public void Configure(EntityTypeBuilder<GalleryAlbumMedia> builder)
    {
        builder.HasKey(x => new { x.GalleryAlbumId, x.MediaAssetId });

        builder.HasOne(x => x.GalleryAlbum)
            .WithMany(x => x.GalleryAlbumMedia)
            .HasForeignKey(x => x.GalleryAlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MediaAsset)
            .WithMany(x => x.GalleryAlbumMedia)
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GalleryAlbumId);
        builder.HasIndex(x => x.MediaAssetId);
        builder.HasIndex(x => new { x.GalleryAlbumId, x.DisplayOrder });
        builder.HasIndex(x => new { x.MediaAssetId, x.IsFeatured });
    }
}

internal sealed class TheatreInformationConfiguration : IEntityTypeConfiguration<TheatreInformation>
{
    public void Configure(EntityTypeBuilder<TheatreInformation> builder)
    {
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.FacebookUrl).HasMaxLength(500);
        builder.Property(x => x.InstagramUrl).HasMaxLength(500);
        builder.Property(x => x.ReservationUrl).HasMaxLength(500);

        builder.HasOne(x => x.HeroBackgroundMediaAsset).WithMany().HasForeignKey(x => x.HeroBackgroundMediaAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AboutPreviewMediaAsset).WithMany().HasForeignKey(x => x.AboutPreviewMediaAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReservationBannerMediaAsset).WithMany().HasForeignKey(x => x.ReservationBannerMediaAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PitfFeatureMediaAsset).WithMany().HasForeignKey(x => x.PitfFeatureMediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TheatreInformationTranslationConfiguration : IEntityTypeConfiguration<TheatreInformationTranslation>
{
    public void Configure(EntityTypeBuilder<TheatreInformationTranslation> builder)
    {
        builder.Property(x => x.TheatreName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.HeroSlogan).HasMaxLength(250).IsRequired();
        builder.Property(x => x.AboutShort).HasMaxLength(700).IsRequired();
        builder.Property(x => x.AboutFull).IsRequired();
        builder.Property(x => x.PitfShortDescription).HasMaxLength(700).IsRequired();
        builder.Property(x => x.AddressDisplayText).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ReservationCallToActionTitle).HasMaxLength(180).IsRequired();
        builder.Property(x => x.ReservationCallToActionText).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(220);
        builder.Property(x => x.MetaDescription).HasMaxLength(320);

        builder.HasOne(x => x.TheatreInformation)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.TheatreInformationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TheatreInformationId, x.LanguageId }).IsUnique();
    }
}

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.GoogleMapsUrl).HasMaxLength(500);
        builder.HasIndex(x => x.IsActive);
    }
}

internal sealed class LocationTranslationConfiguration : IEntityTypeConfiguration<LocationTranslation>
{
    public void Configure(EntityTypeBuilder<LocationTranslation> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();

        builder.HasOne(x => x.Location)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.LocationId, x.LanguageId }).IsUnique();
    }
}

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(8).IsRequired();

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.IsRead);
    }
}

internal sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
        builder.Property(x => x.PreferredLanguageCode).HasMaxLength(8).IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.SubscribedAt);
    }
}
