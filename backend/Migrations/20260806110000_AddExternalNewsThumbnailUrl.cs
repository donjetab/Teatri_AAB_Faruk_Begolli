using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Theatre.Api.Data;

#nullable disable

namespace Theatre.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806110000_AddExternalNewsThumbnailUrl")]
public partial class AddExternalNewsThumbnailUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CardThumbnailExternalUrl",
            table: "NewsArticles",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CardThumbnailExternalUrl", table: "NewsArticles");
    }
}
