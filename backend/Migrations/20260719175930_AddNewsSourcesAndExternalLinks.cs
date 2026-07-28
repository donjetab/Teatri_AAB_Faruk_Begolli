using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsSourcesAndExternalLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArticleType",
                table: "NewsArticles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Authored");

            migrationBuilder.AddColumn<string>(
                name: "ExternalSourceName",
                table: "NewsArticles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "NewsArticles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NewsExternalLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewsArticleId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsExternalLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsExternalLinks_NewsArticles_NewsArticleId",
                        column: x => x.NewsArticleId,
                        principalTable: "NewsArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_ArticleType",
                table: "NewsArticles",
                column: "ArticleType");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NewsArticles_ArticleType_ExternalUrl",
                table: "NewsArticles",
                sql: "([ArticleType] = 'Authored' AND [ExternalUrl] IS NULL) OR ([ArticleType] = 'External' AND [ExternalUrl] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_NewsExternalLinks_NewsArticleId_DisplayOrder",
                table: "NewsExternalLinks",
                columns: new[] { "NewsArticleId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsExternalLinks_Url",
                table: "NewsExternalLinks",
                column: "Url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsExternalLinks");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_ArticleType",
                table: "NewsArticles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NewsArticles_ArticleType_ExternalUrl",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "ArticleType",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "ExternalSourceName",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "NewsArticles");
        }
    }
}
