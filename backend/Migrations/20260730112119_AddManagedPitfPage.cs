using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedPitfPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationUrl",
                table: "PitfEditions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "PitfEditions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PitfEditions_DisplayOrder",
                table: "PitfEditions",
                column: "DisplayOrder");

            // Move the ten editions that were previously hardcoded in the React page
            // into editable database records. Existing records for a year are preserved.
            migrationBuilder.Sql(
                """
                DECLARE @Now datetimeoffset = SYSDATETIMEOFFSET();

                INSERT INTO [MediaAssets]
                    ([FileUrl], [FileName], [MimeType], [Width], [Height], [FileSize], [TakenAt], [UploadedAt], [IsActive])
                SELECT
                    CONCAT('/uploads/pitf-editions/', source.[EditionNumber], '.png'),
                    CONCAT(source.[EditionNumber], '.png'),
                    'image/png', NULL, NULL, NULL, NULL, @Now, CAST(1 AS bit)
                FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8), (9), (10)) source([EditionNumber])
                WHERE NOT EXISTS (
                    SELECT 1 FROM [MediaAssets] media
                    WHERE media.[FileUrl] = CONCAT('/uploads/pitf-editions/', source.[EditionNumber], '.png')
                );

                INSERT INTO [PitfEditions]
                    ([EditionNumber], [Year], [LogoMediaAssetId], [CoverMediaAssetId], [StartDate], [EndDate],
                     [DestinationUrl], [DisplayOrder], [IsPublished], [IsFeatured], [CreatedAt], [UpdatedAt])
                SELECT
                    source.[EditionNumber], source.[Year], NULL, media.[Id], NULL, NULL,
                    NULL, source.[DisplayOrder], CAST(1 AS bit), CAST(0 AS bit), @Now, @Now
                FROM (VALUES
                    (10, 2026, 0), (9, 2025, 1), (8, 2024, 2), (7, 2023, 3), (6, 2022, 4),
                    (5, 2021, 5), (4, 2020, 6), (3, 2019, 7), (2, 2018, 8), (1, 2017, 9)
                ) source([EditionNumber], [Year], [DisplayOrder])
                INNER JOIN [MediaAssets] media
                    ON media.[FileUrl] = CONCAT('/uploads/pitf-editions/', source.[EditionNumber], '.png')
                WHERE NOT EXISTS (
                    SELECT 1 FROM [PitfEditions] edition WHERE edition.[Year] = source.[Year]
                );

                UPDATE edition
                SET edition.[CoverMediaAssetId] = media.[Id],
                    edition.[DisplayOrder] = source.[DisplayOrder],
                    edition.[UpdatedAt] = @Now
                FROM [PitfEditions] edition
                INNER JOIN (VALUES
                    (10, 2026, 0), (9, 2025, 1), (8, 2024, 2), (7, 2023, 3), (6, 2022, 4),
                    (5, 2021, 5), (4, 2020, 6), (3, 2019, 7), (2, 2018, 8), (1, 2017, 9)
                ) source([EditionNumber], [Year], [DisplayOrder]) ON source.[Year] = edition.[Year]
                INNER JOIN [MediaAssets] media
                    ON media.[FileUrl] = CONCAT('/uploads/pitf-editions/', source.[EditionNumber], '.png');

                INSERT INTO [PitfEditionTranslations]
                    ([PitfEditionId], [LanguageId], [Title], [Slug], [ShortDescription],
                     [FullDescription], [MetaTitle], [MetaDescription])
                SELECT
                    edition.[Id], language.[Id],
                    CONCAT('PITF ', edition.[Year]),
                    CONCAT('pitf-', edition.[Year], '-', edition.[EditionNumber], '-', language.[Code]),
                    CONCAT('PITF ', edition.[Year]),
                    CONCAT('PITF ', edition.[Year]),
                    CONCAT('PITF ', edition.[Year]),
                    CONCAT('PITF ', edition.[Year])
                FROM [PitfEditions] edition
                CROSS JOIN [Languages] language
                WHERE edition.[Year] BETWEEN 2017 AND 2026
                  AND language.[Code] IN ('sq', 'en')
                  AND NOT EXISTS (
                      SELECT 1 FROM [PitfEditionTranslations] translation
                      WHERE translation.[PitfEditionId] = edition.[Id]
                        AND translation.[LanguageId] = language.[Id]
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PitfEditions_DisplayOrder",
                table: "PitfEditions");

            migrationBuilder.DropColumn(
                name: "DestinationUrl",
                table: "PitfEditions");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "PitfEditions");
        }
    }
}
