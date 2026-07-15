using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    GoogleMapsUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterSubscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    PreferredLanguageCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscribedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnsubscribedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShowCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditTypeTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditTypeId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTypeTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTypeTranslations_CreditTypes_CreditTypeId",
                        column: x => x.CreditTypeId,
                        principalTable: "CreditTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditTypeTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationTranslations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssetTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MediaAssetId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssetTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAssetTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MediaAssetTranslations_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoverMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsArticles_MediaAssets_CoverMediaAssetId",
                        column: x => x.CoverMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ProfileMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.ForeignKey(
                        name: "FK_People_MediaAssets_ProfileMediaAssetId",
                        column: x => x.ProfileMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PitfEditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EditionNumber = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LogoMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    CoverMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitfEditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitfEditions_MediaAssets_CoverMediaAssetId",
                        column: x => x.CoverMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PitfEditions_MediaAssets_LogoMediaAssetId",
                        column: x => x.LogoMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TheatreInformation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FoundedYear = table.Column<int>(type: "int", nullable: false),
                    PerformancesCount = table.Column<int>(type: "int", nullable: false),
                    SpectatorsCount = table.Column<int>(type: "int", nullable: false),
                    HeroBackgroundMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    AboutPreviewMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    ReservationBannerMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    PitfFeatureMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    FacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstagramUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReservationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheatreInformation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheatreInformation_MediaAssets_AboutPreviewMediaAssetId",
                        column: x => x.AboutPreviewMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheatreInformation_MediaAssets_HeroBackgroundMediaAssetId",
                        column: x => x.HeroBackgroundMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheatreInformation_MediaAssets_PitfFeatureMediaAssetId",
                        column: x => x.PitfFeatureMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheatreInformation_MediaAssets_ReservationBannerMediaAssetId",
                        column: x => x.ReservationBannerMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShowCategoryTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowCategoryId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowCategoryTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowCategoryTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowCategoryTranslations_ShowCategories_ShowCategoryId",
                        column: x => x.ShowCategoryId,
                        principalTable: "ShowCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowCategoryId = table.Column<int>(type: "int", nullable: false),
                    PosterMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shows_MediaAssets_PosterMediaAssetId",
                        column: x => x.PosterMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shows_ShowCategories_ShowCategoryId",
                        column: x => x.ShowCategoryId,
                        principalTable: "ShowCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticleTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewsArticleId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticleTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsArticleTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsArticleTranslations_NewsArticles_NewsArticleId",
                        column: x => x.NewsArticleId,
                        principalTable: "NewsArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Biography = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfessionalTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonTranslations_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitfEditionTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PitfEditionId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    FullDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitfEditionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitfEditionTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PitfEditionTranslations_PitfEditions_PitfEditionId",
                        column: x => x.PitfEditionId,
                        principalTable: "PitfEditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TheatreInformationTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TheatreInformationId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    TheatreName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    HeroSlogan = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AboutShort = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    AboutFull = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PitfShortDescription = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    AddressDisplayText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ReservationCallToActionTitle = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ReservationCallToActionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheatreInformationTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheatreInformationTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheatreInformationTranslations_TheatreInformation_TheatreInformationId",
                        column: x => x.TheatreInformationId,
                        principalTable: "TheatreInformation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalleryAlbums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CoverMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    ShowId = table.Column<int>(type: "int", nullable: true),
                    NewsArticleId = table.Column<int>(type: "int", nullable: true),
                    PitfEditionId = table.Column<int>(type: "int", nullable: true),
                    IsVisibleInGeneralGallery = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryAlbums", x => x.Id);
                    table.CheckConstraint("CK_GalleryAlbums_AlbumType_Parent", "([AlbumType] = 'General' AND [ShowId] IS NULL AND [NewsArticleId] IS NULL AND [PitfEditionId] IS NULL) OR ([AlbumType] = 'Show' AND [ShowId] IS NOT NULL AND [NewsArticleId] IS NULL AND [PitfEditionId] IS NULL) OR ([AlbumType] = 'NewsArticle' AND [ShowId] IS NULL AND [NewsArticleId] IS NOT NULL AND [PitfEditionId] IS NULL) OR ([AlbumType] = 'PitfEdition' AND [ShowId] IS NULL AND [NewsArticleId] IS NULL AND [PitfEditionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_GalleryAlbums_MediaAssets_CoverMediaAssetId",
                        column: x => x.CoverMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GalleryAlbums_NewsArticles_NewsArticleId",
                        column: x => x.NewsArticleId,
                        principalTable: "NewsArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalleryAlbums_PitfEditions_PitfEditionId",
                        column: x => x.PitfEditionId,
                        principalTable: "PitfEditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalleryAlbums_Shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowCredits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    CreditTypeId = table.Column<int>(type: "int", nullable: false),
                    CharacterName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowCredits_CreditTypes_CreditTypeId",
                        column: x => x.CreditTypeId,
                        principalTable: "CreditTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowCredits_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowCredits_Shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowPerformances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    StartDateTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TicketUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowPerformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowPerformances_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShowPerformances_Shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    FullDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowTranslations_Shows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "Shows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalleryAlbumMedia",
                columns: table => new
                {
                    GalleryAlbumId = table.Column<int>(type: "int", nullable: false),
                    MediaAssetId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryAlbumMedia", x => new { x.GalleryAlbumId, x.MediaAssetId });
                    table.ForeignKey(
                        name: "FK_GalleryAlbumMedia_GalleryAlbums_GalleryAlbumId",
                        column: x => x.GalleryAlbumId,
                        principalTable: "GalleryAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalleryAlbumMedia_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GalleryAlbumTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GalleryAlbumId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryAlbumTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalleryAlbumTranslations_GalleryAlbums_GalleryAlbumId",
                        column: x => x.GalleryAlbumId,
                        principalTable: "GalleryAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalleryAlbumTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "Code", "IsActive", "IsDefault", "Name" },
                values: new object[,]
                {
                    { 1, "sq", true, true, "Albanian" },
                    { 2, "en", true, false, "English" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_CreatedAt",
                table: "ContactMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_IsRead",
                table: "ContactMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTypes_Code",
                table: "CreditTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditTypes_DisplayOrder",
                table: "CreditTypes",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTypeTranslations_CreditTypeId_LanguageId",
                table: "CreditTypeTranslations",
                columns: new[] { "CreditTypeId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditTypeTranslations_LanguageId",
                table: "CreditTypeTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbumMedia_GalleryAlbumId",
                table: "GalleryAlbumMedia",
                column: "GalleryAlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbumMedia_GalleryAlbumId_DisplayOrder",
                table: "GalleryAlbumMedia",
                columns: new[] { "GalleryAlbumId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbumMedia_MediaAssetId",
                table: "GalleryAlbumMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbumMedia_MediaAssetId_IsFeatured",
                table: "GalleryAlbumMedia",
                columns: new[] { "MediaAssetId", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_AlbumType",
                table: "GalleryAlbums",
                column: "AlbumType");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_CoverMediaAssetId",
                table: "GalleryAlbums",
                column: "CoverMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_EventDate",
                table: "GalleryAlbums",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_IsPublished",
                table: "GalleryAlbums",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_IsVisibleInGeneralGallery",
                table: "GalleryAlbums",
                column: "IsVisibleInGeneralGallery");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_NewsArticleId",
                table: "GalleryAlbums",
                column: "NewsArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_PitfEditionId",
                table: "GalleryAlbums",
                column: "PitfEditionId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_ShowId",
                table: "GalleryAlbums",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbumTranslations_GalleryAlbumId_LanguageId",
                table: "GalleryAlbumTranslations",
                columns: new[] { "GalleryAlbumId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbumTranslations_LanguageId_Slug",
                table: "GalleryAlbumTranslations",
                columns: new[] { "LanguageId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Code",
                table: "Languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Languages_IsDefault",
                table: "Languages",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_IsActive",
                table: "Locations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTranslations_LanguageId",
                table: "LocationTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTranslations_LocationId_LanguageId",
                table: "LocationTranslations",
                columns: new[] { "LocationId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_FileUrl",
                table: "MediaAssets",
                column: "FileUrl");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_IsActive",
                table: "MediaAssets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_UploadedAt",
                table: "MediaAssets",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssetTranslations_LanguageId",
                table: "MediaAssetTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssetTranslations_MediaAssetId_LanguageId",
                table: "MediaAssetTranslations",
                columns: new[] { "MediaAssetId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_CoverMediaAssetId",
                table: "NewsArticles",
                column: "CoverMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_IsFeatured",
                table: "NewsArticles",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_IsPublished",
                table: "NewsArticles",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_PublishedAt",
                table: "NewsArticles",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticleTranslations_LanguageId_Slug",
                table: "NewsArticleTranslations",
                columns: new[] { "LanguageId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticleTranslations_NewsArticleId_LanguageId",
                table: "NewsArticleTranslations",
                columns: new[] { "NewsArticleId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_Email",
                table: "NewsletterSubscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_IsActive",
                table: "NewsletterSubscribers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_SubscribedAt",
                table: "NewsletterSubscribers",
                column: "SubscribedAt");

            migrationBuilder.CreateIndex(
                name: "IX_People_FullName",
                table: "People",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_People_ProfileMediaAssetId",
                table: "People",
                column: "ProfileMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonTranslations_LanguageId",
                table: "PersonTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonTranslations_PersonId_LanguageId",
                table: "PersonTranslations",
                columns: new[] { "PersonId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PitfEditions_CoverMediaAssetId",
                table: "PitfEditions",
                column: "CoverMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PitfEditions_LogoMediaAssetId",
                table: "PitfEditions",
                column: "LogoMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PitfEditions_Year",
                table: "PitfEditions",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_PitfEditions_Year_EditionNumber",
                table: "PitfEditions",
                columns: new[] { "Year", "EditionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PitfEditionTranslations_LanguageId_Slug",
                table: "PitfEditionTranslations",
                columns: new[] { "LanguageId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PitfEditionTranslations_PitfEditionId_LanguageId",
                table: "PitfEditionTranslations",
                columns: new[] { "PitfEditionId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowCategories_DisplayOrder",
                table: "ShowCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ShowCategoryTranslations_LanguageId_Slug",
                table: "ShowCategoryTranslations",
                columns: new[] { "LanguageId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowCategoryTranslations_ShowCategoryId_LanguageId",
                table: "ShowCategoryTranslations",
                columns: new[] { "ShowCategoryId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowCredits_CreditTypeId",
                table: "ShowCredits",
                column: "CreditTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowCredits_PersonId",
                table: "ShowCredits",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowCredits_ShowId_DisplayOrder",
                table: "ShowCredits",
                columns: new[] { "ShowId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ShowPerformances_LocationId",
                table: "ShowPerformances",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowPerformances_ShowId",
                table: "ShowPerformances",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowPerformances_StartDateTimeUtc",
                table: "ShowPerformances",
                column: "StartDateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ShowPerformances_Status",
                table: "ShowPerformances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Shows_PosterMediaAssetId",
                table: "Shows",
                column: "PosterMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Shows_PublishedAt",
                table: "Shows",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Shows_ShowCategoryId",
                table: "Shows",
                column: "ShowCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Shows_Status",
                table: "Shows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShowTranslations_LanguageId_Slug",
                table: "ShowTranslations",
                columns: new[] { "LanguageId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowTranslations_ShowId_LanguageId",
                table: "ShowTranslations",
                columns: new[] { "ShowId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_AboutPreviewMediaAssetId",
                table: "TheatreInformation",
                column: "AboutPreviewMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_HeroBackgroundMediaAssetId",
                table: "TheatreInformation",
                column: "HeroBackgroundMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_PitfFeatureMediaAssetId",
                table: "TheatreInformation",
                column: "PitfFeatureMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_ReservationBannerMediaAssetId",
                table: "TheatreInformation",
                column: "ReservationBannerMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformationTranslations_LanguageId",
                table: "TheatreInformationTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformationTranslations_TheatreInformationId_LanguageId",
                table: "TheatreInformationTranslations",
                columns: new[] { "TheatreInformationId", "LanguageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMessages");

            migrationBuilder.DropTable(
                name: "CreditTypeTranslations");

            migrationBuilder.DropTable(
                name: "GalleryAlbumMedia");

            migrationBuilder.DropTable(
                name: "GalleryAlbumTranslations");

            migrationBuilder.DropTable(
                name: "LocationTranslations");

            migrationBuilder.DropTable(
                name: "MediaAssetTranslations");

            migrationBuilder.DropTable(
                name: "NewsArticleTranslations");

            migrationBuilder.DropTable(
                name: "NewsletterSubscribers");

            migrationBuilder.DropTable(
                name: "PersonTranslations");

            migrationBuilder.DropTable(
                name: "PitfEditionTranslations");

            migrationBuilder.DropTable(
                name: "ShowCategoryTranslations");

            migrationBuilder.DropTable(
                name: "ShowCredits");

            migrationBuilder.DropTable(
                name: "ShowPerformances");

            migrationBuilder.DropTable(
                name: "ShowTranslations");

            migrationBuilder.DropTable(
                name: "TheatreInformationTranslations");

            migrationBuilder.DropTable(
                name: "GalleryAlbums");

            migrationBuilder.DropTable(
                name: "CreditTypes");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "TheatreInformation");

            migrationBuilder.DropTable(
                name: "NewsArticles");

            migrationBuilder.DropTable(
                name: "PitfEditions");

            migrationBuilder.DropTable(
                name: "Shows");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "ShowCategories");
        }
    }
}
