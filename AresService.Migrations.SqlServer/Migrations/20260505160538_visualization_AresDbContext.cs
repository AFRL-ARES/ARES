using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
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
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalyzerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyzerType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyzerVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyzerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalysisRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalysisResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeRequestSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeResponseReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerTransactions", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceVisualizationConfigs",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Paths = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Style = table.Column<int>(type: "int", nullable: false),
                    PollingRate = table.Column<int>(type: "int", nullable: false),
                    NumberDisplayPoints = table.Column<int>(type: "int", nullable: false),
                    ShowDataLabels = table.Column<bool>(type: "bit", nullable: false),
                    ShowMarkers = table.Column<bool>(type: "bit", nullable: false),
                    GridX = table.Column<int>(type: "int", nullable: false),
                    GridY = table.Column<int>(type: "int", nullable: false),
                    GridW = table.Column<int>(type: "int", nullable: false),
                    GridH = table.Column<int>(type: "int", nullable: false),
                    ChartTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceVisualizationConfigs", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerTransactions",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannerType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannerVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanningRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanningResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeRequestSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeResponseReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTransactions", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "VisualizationPath",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsPlottable = table.Column<bool>(type: "bit", nullable: false),
                    AssociatedDeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
