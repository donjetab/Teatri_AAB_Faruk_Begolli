using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShowPremiereDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PremiereDate",
                table: "Shows",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shows_PremiereDate",
                table: "Shows",
                column: "PremiereDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shows_PremiereDate",
                table: "Shows");

            migrationBuilder.DropColumn(
                name: "PremiereDate",
                table: "Shows");
        }
    }
}
