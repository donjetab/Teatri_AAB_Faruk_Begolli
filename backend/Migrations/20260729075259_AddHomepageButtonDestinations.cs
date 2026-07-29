using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHomepageButtonDestinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboutButtonLink",
                table: "TheatreInformation",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PitfDestinationUrl",
                table: "TheatreInformation",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryButtonLink",
                table: "TheatreInformation",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AboutButtonLink",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "PitfDestinationUrl",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "PrimaryButtonLink",
                table: "TheatreInformation");
        }
    }
}
