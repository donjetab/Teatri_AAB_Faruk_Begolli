using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class PreserveActivityActorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminDisplayName",
                table: "AdminActivities",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminDisplayName",
                table: "AdminActivities");
        }
    }
}
