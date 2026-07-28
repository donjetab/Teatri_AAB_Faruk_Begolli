using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "ShowPerformances",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDateTimeUtc",
                table: "ShowPerformances",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hall",
                table: "ShowPerformances",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "ShowPerformances",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "ShowPerformances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ShowPerformances_IsPublished",
                table: "ShowPerformances",
                column: "IsPublished");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShowPerformances_IsPublished",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "EndDateTimeUtc",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "Hall",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "ShowPerformances");
        }
    }
}
