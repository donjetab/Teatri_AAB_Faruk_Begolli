using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeparatePitfPageContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PitfPageButtonText",
                table: "TheatreInformationTranslations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PitfPageDescription",
                table: "TheatreInformationTranslations",
                type: "nvarchar(700)",
                maxLength: 700,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PitfPageTitle",
                table: "TheatreInformationTranslations",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PitfPageButtonUrl",
                table: "TheatreInformation",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PitfPageMediaAssetId",
                table: "TheatreInformation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_PitfPageMediaAssetId",
                table: "TheatreInformation",
                column: "PitfPageMediaAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_TheatreInformation_MediaAssets_PitfPageMediaAssetId",
                table: "TheatreInformation",
                column: "PitfPageMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Copy the currently visible values once, then keep the public PITF
            // page and the homepage PITF feature independent.
            migrationBuilder.Sql(
                """
                UPDATE [TheatreInformation]
                SET [PitfPageMediaAssetId] = [PitfFeatureMediaAssetId],
                    [PitfPageButtonUrl] = [PitfDestinationUrl];

                UPDATE [TheatreInformationTranslations]
                SET [PitfPageTitle] = [PitfFeatureTitle],
                    [PitfPageDescription] = [PitfShortDescription],
                    [PitfPageButtonText] = [PitfFeatureButtonText];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TheatreInformation_MediaAssets_PitfPageMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropIndex(
                name: "IX_TheatreInformation_PitfPageMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "PitfPageButtonText",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "PitfPageDescription",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "PitfPageTitle",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "PitfPageButtonUrl",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "PitfPageMediaAssetId",
                table: "TheatreInformation");
        }
    }
}
