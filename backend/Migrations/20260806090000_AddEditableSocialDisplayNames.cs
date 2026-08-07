using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Theatre.Api.Data;

#nullable disable

namespace Theatre.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806090000_AddEditableSocialDisplayNames")]
public partial class AddEditableSocialDisplayNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FacebookDisplayName",
            table: "TheatreInformation",
            type: "nvarchar(180)",
            maxLength: 180,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InstagramDisplayName",
            table: "TheatreInformation",
            type: "nvarchar(180)",
            maxLength: 180,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FacebookDisplayName", table: "TheatreInformation");
        migrationBuilder.DropColumn(name: "InstagramDisplayName", table: "TheatreInformation");
    }
}
