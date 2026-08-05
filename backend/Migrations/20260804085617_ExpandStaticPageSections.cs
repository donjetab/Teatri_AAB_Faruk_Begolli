using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandStaticPageSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuoteAuthor",
                table: "StaticPageTranslations",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuoteText",
                table: "StaticPageTranslations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatOneLabel",
                table: "StaticPageTranslations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatOneValue",
                table: "StaticPageTranslations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatThreeLabel",
                table: "StaticPageTranslations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatThreeValue",
                table: "StaticPageTranslations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatTwoLabel",
                table: "StaticPageTranslations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatTwoValue",
                table: "StaticPageTranslations",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "StaticPageTranslations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapEmbedUrl",
                table: "StaticPages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapLinkUrl",
                table: "StaticPages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParallaxMediaAssetId",
                table: "StaticPages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaticPages_ParallaxMediaAssetId",
                table: "StaticPages",
                column: "ParallaxMediaAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaticPages_MediaAssets_ParallaxMediaAssetId",
                table: "StaticPages",
                column: "ParallaxMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaticPages_MediaAssets_ParallaxMediaAssetId",
                table: "StaticPages");

            migrationBuilder.DropIndex(
                name: "IX_StaticPages_ParallaxMediaAssetId",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "QuoteAuthor",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "QuoteText",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "StatOneLabel",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "StatOneValue",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "StatThreeLabel",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "StatThreeValue",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "StatTwoLabel",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "StatTwoValue",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "StaticPageTranslations");

            migrationBuilder.DropColumn(
                name: "MapEmbedUrl",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "MapLinkUrl",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "ParallaxMediaAssetId",
                table: "StaticPages");
        }
    }
}
