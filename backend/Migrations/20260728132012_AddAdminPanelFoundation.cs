using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPanelFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboutButtonText",
                table: "TheatreInformationTranslations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AboutTitle",
                table: "TheatreInformationTranslations",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FooterCopyrightText",
                table: "TheatreInformationTranslations",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroButtonText",
                table: "TheatreInformationTranslations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroSupportingText",
                table: "TheatreInformationTranslations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PitfFeatureButtonText",
                table: "TheatreInformationTranslations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PitfFeatureTitle",
                table: "TheatreInformationTranslations",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReservationButtonText",
                table: "TheatreInformationTranslations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FaviconMediaAssetId",
                table: "TheatreInformation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HeroIsVisible",
                table: "TheatreInformation",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LatestNewsCount",
                table: "TheatreInformation",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "LogoMediaAssetId",
                table: "TheatreInformation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PitfFeatureIsVisible",
                table: "TheatreInformation",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReservationBannerIsVisible",
                table: "TheatreInformation",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SocialSharingMediaAssetId",
                table: "TheatreInformation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminActivities_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_FaviconMediaAssetId",
                table: "TheatreInformation",
                column: "FaviconMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_LogoMediaAssetId",
                table: "TheatreInformation",
                column: "LogoMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_TheatreInformation_SocialSharingMediaAssetId",
                table: "TheatreInformation",
                column: "SocialSharingMediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActivities_AdminUserId",
                table: "AdminActivities",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActivities_CreatedAt",
                table: "AdminActivities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "AdminUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_IsActive",
                table: "AdminUsers",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_TheatreInformation_MediaAssets_FaviconMediaAssetId",
                table: "TheatreInformation",
                column: "FaviconMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TheatreInformation_MediaAssets_LogoMediaAssetId",
                table: "TheatreInformation",
                column: "LogoMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TheatreInformation_MediaAssets_SocialSharingMediaAssetId",
                table: "TheatreInformation",
                column: "SocialSharingMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TheatreInformation_MediaAssets_FaviconMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropForeignKey(
                name: "FK_TheatreInformation_MediaAssets_LogoMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropForeignKey(
                name: "FK_TheatreInformation_MediaAssets_SocialSharingMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropTable(
                name: "AdminActivities");

            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropIndex(
                name: "IX_TheatreInformation_FaviconMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropIndex(
                name: "IX_TheatreInformation_LogoMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropIndex(
                name: "IX_TheatreInformation_SocialSharingMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "AboutButtonText",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "AboutTitle",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "FooterCopyrightText",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "HeroButtonText",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "HeroSupportingText",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "PitfFeatureButtonText",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "PitfFeatureTitle",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "ReservationButtonText",
                table: "TheatreInformationTranslations");

            migrationBuilder.DropColumn(
                name: "FaviconMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "HeroIsVisible",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "LatestNewsCount",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "LogoMediaAssetId",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "PitfFeatureIsVisible",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "ReservationBannerIsVisible",
                table: "TheatreInformation");

            migrationBuilder.DropColumn(
                name: "SocialSharingMediaAssetId",
                table: "TheatreInformation");
        }
    }
}
