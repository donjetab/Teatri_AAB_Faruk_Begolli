using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaManagementMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "MediaAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotographerCredit",
                table: "MediaAssets",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ContentHash",
                table: "MediaAssets",
                column: "ContentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_ContentHash",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "PhotographerCredit",
                table: "MediaAssets");
        }
    }
}
