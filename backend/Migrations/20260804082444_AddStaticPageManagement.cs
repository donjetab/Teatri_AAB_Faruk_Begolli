using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStaticPageManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaticPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FeaturedMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    SocialSharingMediaAssetId = table.Column<int>(type: "int", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticPages_MediaAssets_FeaturedMediaAssetId",
                        column: x => x.FeaturedMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaticPages_MediaAssets_SocialSharingMediaAssetId",
                        column: x => x.SocialSharingMediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaticPageTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaticPageId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticPageTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticPageTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaticPageTranslations_StaticPages_StaticPageId",
                        column: x => x.StaticPageId,
                        principalTable: "StaticPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaticPages_FeaturedMediaAssetId",
                table: "StaticPages",
                column: "FeaturedMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticPages_PageKey",
                table: "StaticPages",
                column: "PageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaticPages_SocialSharingMediaAssetId",
                table: "StaticPages",
                column: "SocialSharingMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_StaticPageTranslations_LanguageId_Slug",
                table: "StaticPageTranslations",
                columns: new[] { "LanguageId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaticPageTranslations_StaticPageId_LanguageId",
                table: "StaticPageTranslations",
                columns: new[] { "StaticPageId", "LanguageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaticPageTranslations");

            migrationBuilder.DropTable(
                name: "StaticPages");
        }
    }
}
