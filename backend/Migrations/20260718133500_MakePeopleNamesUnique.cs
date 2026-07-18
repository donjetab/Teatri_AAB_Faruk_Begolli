using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Theatre.Api.Data;

#nullable disable

namespace Theatre.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260718133500_MakePeopleNamesUnique")]
public partial class MakePeopleNamesUnique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE [People]
            SET [FullName] = LTRIM(RTRIM([FullName]));

            SELECT [Id], MIN([Id]) OVER (PARTITION BY [FullName]) AS [KeepId]
            INTO #DuplicatePeople
            FROM [People];

            UPDATE keeper
            SET [ProfileMediaAssetId] = duplicate.[ProfileMediaAssetId]
            FROM [People] keeper
            INNER JOIN #DuplicatePeople map ON map.[KeepId] = keeper.[Id]
            INNER JOIN [People] duplicate ON duplicate.[Id] = map.[Id]
            WHERE map.[Id] <> map.[KeepId]
              AND keeper.[ProfileMediaAssetId] IS NULL
              AND duplicate.[ProfileMediaAssetId] IS NOT NULL;

            ;WITH MissingTranslations AS
            (
                SELECT
                    map.[KeepId],
                    translation.[LanguageId],
                    translation.[Biography],
                    translation.[ProfessionalTitle],
                    ROW_NUMBER() OVER (
                        PARTITION BY map.[KeepId], translation.[LanguageId]
                        ORDER BY translation.[Id]
                    ) AS [RowNumber]
                FROM #DuplicatePeople map
                INNER JOIN [PersonTranslations] translation ON translation.[PersonId] = map.[Id]
                WHERE map.[Id] <> map.[KeepId]
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [PersonTranslations] existing
                      WHERE existing.[PersonId] = map.[KeepId]
                        AND existing.[LanguageId] = translation.[LanguageId]
                  )
            )
            INSERT INTO [PersonTranslations] ([PersonId], [LanguageId], [Biography], [ProfessionalTitle])
            SELECT [KeepId], [LanguageId], [Biography], [ProfessionalTitle]
            FROM MissingTranslations
            WHERE [RowNumber] = 1;

            UPDATE credit
            SET [PersonId] = map.[KeepId]
            FROM [ShowCredits] credit
            INNER JOIN #DuplicatePeople map ON map.[Id] = credit.[PersonId]
            WHERE map.[Id] <> map.[KeepId];

            ;WITH DuplicateCredits AS
            (
                SELECT
                    [Id],
                    ROW_NUMBER() OVER (
                        PARTITION BY [ShowId], [PersonId], [CreditTypeId], [CharacterName]
                        ORDER BY [Id]
                    ) AS [RowNumber]
                FROM [ShowCredits]
            )
            DELETE FROM DuplicateCredits WHERE [RowNumber] > 1;

            DELETE translation
            FROM [PersonTranslations] translation
            INNER JOIN #DuplicatePeople map ON map.[Id] = translation.[PersonId]
            WHERE map.[Id] <> map.[KeepId];

            DELETE person
            FROM [People] person
            INNER JOIN #DuplicatePeople map ON map.[Id] = person.[Id]
            WHERE map.[Id] <> map.[KeepId];

            DROP TABLE #DuplicatePeople;
            """);

        migrationBuilder.DropIndex(
            name: "IX_People_FullName",
            table: "People");

        migrationBuilder.CreateIndex(
            name: "IX_People_FullName",
            table: "People",
            column: "FullName",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_People_FullName",
            table: "People");

        migrationBuilder.CreateIndex(
            name: "IX_People_FullName",
            table: "People",
            column: "FullName");
    }
}
