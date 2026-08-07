using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Theatre.Api.Data;

#nullable disable

namespace Theatre.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806101500_AddSeparateFooterLogo")]
public partial class AddSeparateFooterLogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "FooterLogoMediaAssetId",
            table: "TheatreInformation",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_TheatreInformation_FooterLogoMediaAssetId",
            table: "TheatreInformation",
            column: "FooterLogoMediaAssetId");

        migrationBuilder.AddForeignKey(
            name: "FK_TheatreInformation_MediaAssets_FooterLogoMediaAssetId",
            table: "TheatreInformation",
            column: "FooterLogoMediaAssetId",
            principalTable: "MediaAssets",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_TheatreInformation_MediaAssets_FooterLogoMediaAssetId", table: "TheatreInformation");
        migrationBuilder.DropIndex(name: "IX_TheatreInformation_FooterLogoMediaAssetId", table: "TheatreInformation");
        migrationBuilder.DropColumn(name: "FooterLogoMediaAssetId", table: "TheatreInformation");
    }
}
