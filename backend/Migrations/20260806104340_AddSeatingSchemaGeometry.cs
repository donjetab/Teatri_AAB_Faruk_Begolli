using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Theatre.Api.Migrations;

public partial class AddSeatingSchemaGeometry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('SeatingTemplateSeats','Rotation') IS NULL ALTER TABLE SeatingTemplateSeats ADD Rotation decimal(6,2) NOT NULL CONSTRAINT DF_SeatingTemplateSeats_Rotation DEFAULT 0;
            IF COL_LENGTH('SeatingTemplates','CanvasHeight') IS NULL ALTER TABLE SeatingTemplates ADD CanvasHeight decimal(9,2) NOT NULL CONSTRAINT DF_SeatingTemplates_CanvasHeight DEFAULT 800;
            IF COL_LENGTH('SeatingTemplates','CanvasWidth') IS NULL ALTER TABLE SeatingTemplates ADD CanvasWidth decimal(9,2) NOT NULL CONSTRAINT DF_SeatingTemplates_CanvasWidth DEFAULT 1000;
            IF COL_LENGTH('SeatingTemplates','StageHeight') IS NULL ALTER TABLE SeatingTemplates ADD StageHeight decimal(9,2) NULL;
            IF COL_LENGTH('SeatingTemplates','StageLabel') IS NULL ALTER TABLE SeatingTemplates ADD StageLabel nvarchar(80) NULL;
            IF COL_LENGTH('SeatingTemplates','StageWidth') IS NULL ALTER TABLE SeatingTemplates ADD StageWidth decimal(9,2) NULL;
            IF COL_LENGTH('SeatingTemplates','StageX') IS NULL ALTER TABLE SeatingTemplates ADD StageX decimal(9,2) NULL;
            IF COL_LENGTH('SeatingTemplates','StageY') IS NULL ALTER TABLE SeatingTemplates ADD StageY decimal(9,2) NULL;
            IF COL_LENGTH('PerformanceSeats','Rotation') IS NULL ALTER TABLE PerformanceSeats ADD Rotation decimal(6,2) NOT NULL CONSTRAINT DF_PerformanceSeats_Rotation DEFAULT 0;
            IF COL_LENGTH('PerformanceSeatingLayouts','CanvasHeight') IS NULL ALTER TABLE PerformanceSeatingLayouts ADD CanvasHeight decimal(9,2) NOT NULL CONSTRAINT DF_PerformanceSeatingLayouts_CanvasHeight DEFAULT 800;
            IF COL_LENGTH('PerformanceSeatingLayouts','CanvasWidth') IS NULL ALTER TABLE PerformanceSeatingLayouts ADD CanvasWidth decimal(9,2) NOT NULL CONSTRAINT DF_PerformanceSeatingLayouts_CanvasWidth DEFAULT 1000;
            IF COL_LENGTH('PerformanceSeatingLayouts','StageHeight') IS NULL ALTER TABLE PerformanceSeatingLayouts ADD StageHeight decimal(9,2) NULL;
            IF COL_LENGTH('PerformanceSeatingLayouts','StageLabel') IS NULL ALTER TABLE PerformanceSeatingLayouts ADD StageLabel nvarchar(80) NULL;
            IF COL_LENGTH('PerformanceSeatingLayouts','StageWidth') IS NULL ALTER TABLE PerformanceSeatingLayouts ADD StageWidth decimal(9,2) NULL;
            IF COL_LENGTH('PerformanceSeatingLayouts','StageX') IS NULL ALTER TABLE PerformanceSeatingLayouts ADD StageX decimal(9,2) NULL;
            IF COL_LENGTH('PerformanceSeatingLayouts','StageY') IS NULL ALTER TABLE PerformanceSeatingLayouts ADD StageY decimal(9,2) NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Rotation", table: "SeatingTemplateSeats");
        migrationBuilder.DropColumn(name: "CanvasHeight", table: "SeatingTemplates");
        migrationBuilder.DropColumn(name: "CanvasWidth", table: "SeatingTemplates");
        migrationBuilder.DropColumn(name: "StageHeight", table: "SeatingTemplates");
        migrationBuilder.DropColumn(name: "StageLabel", table: "SeatingTemplates");
        migrationBuilder.DropColumn(name: "StageWidth", table: "SeatingTemplates");
        migrationBuilder.DropColumn(name: "StageX", table: "SeatingTemplates");
        migrationBuilder.DropColumn(name: "StageY", table: "SeatingTemplates");
        migrationBuilder.DropColumn(name: "Rotation", table: "PerformanceSeats");
        migrationBuilder.DropColumn(name: "CanvasHeight", table: "PerformanceSeatingLayouts");
        migrationBuilder.DropColumn(name: "CanvasWidth", table: "PerformanceSeatingLayouts");
        migrationBuilder.DropColumn(name: "StageHeight", table: "PerformanceSeatingLayouts");
        migrationBuilder.DropColumn(name: "StageLabel", table: "PerformanceSeatingLayouts");
        migrationBuilder.DropColumn(name: "StageWidth", table: "PerformanceSeatingLayouts");
        migrationBuilder.DropColumn(name: "StageX", table: "PerformanceSeatingLayouts");
        migrationBuilder.DropColumn(name: "StageY", table: "PerformanceSeatingLayouts");
    }
}
