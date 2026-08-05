using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NavigationConfigurationJson",
                table: "TheatreInformation",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NavigationConfigurationJson",
                table: "TheatreInformation");
        }
    }
}
