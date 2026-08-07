using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReservationMode",
                table: "ShowPerformances",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "ExternalUrl");

            migrationBuilder.AddColumn<int>(
                name: "SeatingTemplateId",
                table: "ShowPerformances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    NormalizedPhone = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceSeatingLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PerformanceId = table.Column<int>(type: "int", nullable: false),
                    SourceTemplateId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceSeatingLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceSeatingLayouts_ShowPerformances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "ShowPerformances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatingTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatingTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatingTemplates_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PerformanceId = table.Column<int>(type: "int", nullable: false),
                    ReservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConfirmationStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    AdminComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_ShowPerformances_PerformanceId",
                        column: x => x.PerformanceId,
                        principalTable: "ShowPerformances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceSeats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PerformanceSeatingLayoutId = table.Column<int>(type: "int", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowLabel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SeatLabel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SectionOrder = table.Column<int>(type: "int", nullable: false),
                    RowOrder = table.Column<int>(type: "int", nullable: false),
                    SeatOrder = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    PositionY = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceSeats_PerformanceSeatingLayouts_PerformanceSeatingLayoutId",
                        column: x => x.PerformanceSeatingLayoutId,
                        principalTable: "PerformanceSeatingLayouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatingTemplateSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatingTemplateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatingTemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatingTemplateSections_SeatingTemplates_SeatingTemplateId",
                        column: x => x.SeatingTemplateId,
                        principalTable: "SeatingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservationAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationAuditEvents_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PerformanceSeatId = table.Column<int>(type: "int", nullable: false),
                    ReservationId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AdminComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatAllocations", x => x.Id);
                    table.CheckConstraint("CK_SeatAllocation_Owner", "([Type] = 'AdminBlock' AND [ReservationId] IS NULL) OR ([Type] = 'Reservation' AND [ReservationId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SeatAllocations_PerformanceSeats_PerformanceSeatId",
                        column: x => x.PerformanceSeatId,
                        principalTable: "PerformanceSeats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatAllocations_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeatingTemplateRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatingTemplateSectionId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatingTemplateRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatingTemplateRows_SeatingTemplateSections_SeatingTemplateSectionId",
                        column: x => x.SeatingTemplateSectionId,
                        principalTable: "SeatingTemplateSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatingTemplateSeats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatingTemplateRowId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    PositionY = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatingTemplateSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatingTemplateSeats_SeatingTemplateRows_SeatingTemplateRowId",
                        column: x => x.SeatingTemplateRowId,
                        principalTable: "SeatingTemplateRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShowPerformances_SeatingTemplateId",
                table: "ShowPerformances",
                column: "SeatingTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedPhone",
                table: "Customers",
                column: "NormalizedPhone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSeatingLayouts_PerformanceId",
                table: "PerformanceSeatingLayouts",
                column: "PerformanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSeats_PerformanceSeatingLayoutId_SectionName_RowLabel_SeatLabel",
                table: "PerformanceSeats",
                columns: new[] { "PerformanceSeatingLayoutId", "SectionName", "RowLabel", "SeatLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAuditEvents_ReservationId_CreatedAt",
                table: "ReservationAuditEvents",
                columns: new[] { "ReservationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PerformanceId_Status",
                table: "Reservations",
                columns: new[] { "PerformanceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_PerformanceSeatId",
                table: "SeatAllocations",
                column: "PerformanceSeatId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_ReservationId",
                table: "SeatAllocations",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatingTemplateRows_SeatingTemplateSectionId_DisplayOrder",
                table: "SeatingTemplateRows",
                columns: new[] { "SeatingTemplateSectionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatingTemplates_LocationId_Name",
                table: "SeatingTemplates",
                columns: new[] { "LocationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatingTemplateSeats_SeatingTemplateRowId_DisplayOrder",
                table: "SeatingTemplateSeats",
                columns: new[] { "SeatingTemplateRowId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatingTemplateSections_SeatingTemplateId_DisplayOrder",
                table: "SeatingTemplateSections",
                columns: new[] { "SeatingTemplateId", "DisplayOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShowPerformances_SeatingTemplates_SeatingTemplateId",
                table: "ShowPerformances",
                column: "SeatingTemplateId",
                principalTable: "SeatingTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO SeatingTemplates (LocationId, Name, IsDefault, IsActive, CreatedAt, UpdatedAt)
                SELECT l.Id, CASE WHEN lt.Name LIKE '%Kamertal%' THEN 'Teatri Kamertal AAB' ELSE 'Teatri AAB Faruk Begolli' END,
                       1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
                FROM Locations l
                INNER JOIN LocationTranslations lt ON lt.LocationId = l.Id
                WHERE lt.LanguageId = 1 AND (lt.Name LIKE '%Faruk Begolli%' OR lt.Name LIKE '%Kamertal%')
                  AND NOT EXISTS (SELECT 1 FROM SeatingTemplates st WHERE st.LocationId = l.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShowPerformances_SeatingTemplates_SeatingTemplateId",
                table: "ShowPerformances");

            migrationBuilder.DropTable(
                name: "ReservationAuditEvents");

            migrationBuilder.DropTable(
                name: "SeatAllocations");

            migrationBuilder.DropTable(
                name: "SeatingTemplateSeats");

            migrationBuilder.DropTable(
                name: "PerformanceSeats");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "SeatingTemplateRows");

            migrationBuilder.DropTable(
                name: "PerformanceSeatingLayouts");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "SeatingTemplateSections");

            migrationBuilder.DropTable(
                name: "SeatingTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ShowPerformances_SeatingTemplateId",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "ReservationMode",
                table: "ShowPerformances");

            migrationBuilder.DropColumn(
                name: "SeatingTemplateId",
                table: "ShowPerformances");
        }
    }
}
