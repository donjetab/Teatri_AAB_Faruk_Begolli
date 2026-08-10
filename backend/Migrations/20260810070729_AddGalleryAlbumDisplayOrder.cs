using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryAlbumDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "GalleryAlbums",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH OrderedPlayGalleries AS
                (
                    SELECT [Id], ROW_NUMBER() OVER (ORDER BY [UpdatedAt] DESC, [Id]) - 1 AS [NewOrder]
                    FROM [GalleryAlbums]
                    WHERE [AlbumType] = 'Show'
                )
                UPDATE albums
                SET albums.[DisplayOrder] = ordered.[NewOrder],
                    albums.[IsVisibleInGeneralGallery] = CASE WHEN albums.[IsPublished] = 1 THEN 1 ELSE albums.[IsVisibleInGeneralGallery] END
                FROM [GalleryAlbums] albums
                INNER JOIN OrderedPlayGalleries ordered ON ordered.[Id] = albums.[Id];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_GalleryAlbums_DisplayOrder",
                table: "GalleryAlbums",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GalleryAlbums_DisplayOrder",
                table: "GalleryAlbums");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "GalleryAlbums");
        }
    }
}
