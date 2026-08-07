using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationCompletionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxSeatsPerReservation",
                table: "ShowPerformances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReservationClosesAtUtc",
                table: "ShowPerformances",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReservationOpensAtUtc",
                table: "ShowPerformances",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationUnavailableMessage",
                table: "ShowPerformances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReservationsEnabled",
                table: "ShowPerformances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnonymizedAt",
                table: "Customers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReservationAdminAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    ReservationId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PreviousValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationAdminAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationAdminAudits_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReservationAdminAudits_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationAdminAudits_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAdminAudits_AdminUserId",
                table: "ReservationAdminAudits",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAdminAudits_CustomerId",
                table: "ReservationAdminAudits",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAdminAudits_EntityType_EntityId_CreatedAt",
                table: "ReservationAdminAudits",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAdminAudits_ReservationId",
                table: "ReservationAdminAudits",
                column: "ReservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationAdminAudits");

            migrationBuilder.DropColumn(
                name: "MaxSeatsPerReservation",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "ReservationClosesAtUtc",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "ReservationOpensAtUtc",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "ReservationUnavailableMessage",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "ReservationsEnabled",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "Customers");
        }
    }
}
