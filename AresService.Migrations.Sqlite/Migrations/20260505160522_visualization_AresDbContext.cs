using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class visualization_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyzerTransactions",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnalyzerName = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzerType = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzerVersion = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzerId = table.Column<string>(type: "TEXT", nullable: true),
                    AnalysisRequest = table.Column<string>(type: "TEXT", nullable: true),
                    AnalysisResponse = table.Column<string>(type: "TEXT", nullable: true),
                    TimeRequestSent = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TimeResponseReceived = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerTransactions", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceVisualizationConfigs",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Paths = table.Column<string>(type: "TEXT", nullable: true),
                    Style = table.Column<int>(type: "INTEGER", nullable: false),
                    PollingRate = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberDisplayPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowDataLabels = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowMarkers = table.Column<bool>(type: "INTEGER", nullable: false),
                    GridX = table.Column<int>(type: "INTEGER", nullable: false),
                    GridY = table.Column<int>(type: "INTEGER", nullable: false),
                    GridW = table.Column<int>(type: "INTEGER", nullable: false),
                    GridH = table.Column<int>(type: "INTEGER", nullable: false),
                    ChartTitle = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceVisualizationConfigs", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerTransactions",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlannerName = table.Column<string>(type: "TEXT", nullable: true),
                    PlannerType = table.Column<string>(type: "TEXT", nullable: true),
                    PlannerVersion = table.Column<string>(type: "TEXT", nullable: true),
                    PlannerId = table.Column<string>(type: "TEXT", nullable: true),
                    PlanningRequest = table.Column<string>(type: "TEXT", nullable: true),
                    PlanningResponse = table.Column<string>(type: "TEXT", nullable: true),
                    TimeRequestSent = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TimeResponseReceived = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTransactions", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "VisualizationPath",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    DataType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPlottable = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssociatedDeviceId = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualizationPath", x => x.UniqueId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyzerTransactions");

            migrationBuilder.DropTable(
                name: "DeviceVisualizationConfigs");

            migrationBuilder.DropTable(
                name: "PlannerTransactions");

            migrationBuilder.DropTable(
                name: "VisualizationPath");
        }
    }
}
