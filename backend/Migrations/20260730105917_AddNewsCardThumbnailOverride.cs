using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsCardThumbnailOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardThumbnailMediaAssetId",
                table: "NewsArticles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_CardThumbnailMediaAssetId",
                table: "NewsArticles",
                column: "CardThumbnailMediaAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_NewsArticles_MediaAssets_CardThumbnailMediaAssetId",
                table: "NewsArticles",
                column: "CardThumbnailMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewsArticles_MediaAssets_CardThumbnailMediaAssetId",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_CardThumbnailMediaAssetId",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "CardThumbnailMediaAssetId",
                table: "NewsArticles");
        }
    }
}
