using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShowManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgeRecommendation",
                table: "Shows",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeaturedMediaAssetId",
                table: "Shows",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatus",
                table: "Shows",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Upcoming");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                table: "Shows",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionYear",
                table: "Shows",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrailerUrl",
                table: "Shows",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Shows",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomRoleEn",
                table: "ShowCredits",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomRoleSq",
                table: "ShowCredits",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shows_FeaturedMediaAssetId",
                table: "Shows",
                column: "FeaturedMediaAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shows_MediaAssets_FeaturedMediaAssetId",
                table: "Shows",
                column: "FeaturedMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shows_MediaAssets_FeaturedMediaAssetId",
                table: "Shows");

            migrationBuilder.DropIndex(
                name: "IX_Shows_FeaturedMediaAssetId",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "AgeRecommendation",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "FeaturedMediaAssetId",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "ProductionYear",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "TrailerUrl",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "CustomRoleEn",
                table: "ShowCredits");

            migrationBuilder.DropColumn(
                name: "CustomRoleSq",
                table: "ShowCredits");
        }
    }
}
