using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
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
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyzerName = table.Column<string>(type: "text", nullable: true),
                    AnalyzerType = table.Column<string>(type: "text", nullable: true),
                    AnalyzerVersion = table.Column<string>(type: "text", nullable: true),
                    AnalyzerId = table.Column<string>(type: "text", nullable: true),
                    AnalysisRequest = table.Column<string>(type: "jsonb", nullable: true),
                    AnalysisResponse = table.Column<string>(type: "jsonb", nullable: true),
                    TimeRequestSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeResponseReceived = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerTransactions", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceVisualizationConfigs",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Paths = table.Column<string>(type: "jsonb", nullable: true),
                    Style = table.Column<int>(type: "integer", nullable: false),
                    PollingRate = table.Column<int>(type: "integer", nullable: false),
                    NumberDisplayPoints = table.Column<int>(type: "integer", nullable: false),
                    ShowDataLabels = table.Column<bool>(type: "boolean", nullable: false),
                    ShowMarkers = table.Column<bool>(type: "boolean", nullable: false),
                    GridX = table.Column<int>(type: "integer", nullable: false),
                    GridY = table.Column<int>(type: "integer", nullable: false),
                    GridW = table.Column<int>(type: "integer", nullable: false),
                    GridH = table.Column<int>(type: "integer", nullable: false),
                    ChartTitle = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceVisualizationConfigs", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerTransactions",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannerName = table.Column<string>(type: "text", nullable: true),
                    PlannerType = table.Column<string>(type: "text", nullable: true),
                    PlannerVersion = table.Column<string>(type: "text", nullable: true),
                    PlannerId = table.Column<string>(type: "text", nullable: true),
                    PlanningRequest = table.Column<string>(type: "jsonb", nullable: true),
                    PlanningResponse = table.Column<string>(type: "jsonb", nullable: true),
                    TimeRequestSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeResponseReceived = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTransactions", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "VisualizationPath",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: true),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    IsPlottable = table.Column<bool>(type: "boolean", nullable: false),
                    AssociatedDeviceId = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
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
