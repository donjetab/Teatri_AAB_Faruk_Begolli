using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Theatre.Api.Data;

#nullable disable

namespace Theatre.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806150000_AddPerformanceSeatHolds")]
public partial class AddPerformanceSeatHolds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PerformanceSeatHolds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HoldToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PerformanceSeatId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PerformanceSeatHolds", x => x.Id);
                table.ForeignKey("FK_PerformanceSeatHolds_PerformanceSeats_PerformanceSeatId", x => x.PerformanceSeatId, "PerformanceSeats", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_PerformanceSeatHolds_ExpiresAt", table: "PerformanceSeatHolds", column: "ExpiresAt");
        migrationBuilder.CreateIndex(name: "IX_PerformanceSeatHolds_HoldToken", table: "PerformanceSeatHolds", column: "HoldToken");
        migrationBuilder.CreateIndex(name: "IX_PerformanceSeatHolds_PerformanceSeatId", table: "PerformanceSeatHolds", column: "PerformanceSeatId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "PerformanceSeatHolds");
}
