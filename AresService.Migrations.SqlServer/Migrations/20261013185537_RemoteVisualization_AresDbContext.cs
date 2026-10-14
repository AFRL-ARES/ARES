using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
  /// <inheritdoc />
  public partial class RemoteVisualization_AresDbContext : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "VisualizerAllocations",
          table: "ExperimentTemplates",
          type: "nvarchar(max)",
          nullable: false,
          defaultValue: "[]");

      migrationBuilder.AddColumn<string>(
          name: "GeneratedVisuals",
          table: "ExperimentExecutionSummaries",
          type: "nvarchar(max)",
          nullable: false,
          defaultValue: "{}");

      migrationBuilder.AddColumn<bool>(
          name: "NameFoldersWithLocalTime",
          table: "AresGeneralSettingsConfig",
          type: "bit",
          nullable: false,
          defaultValue: false);

      migrationBuilder.CreateTable(
          name: "RemoteVisualizationConfigs",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ChartTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
            VisualizerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
            RequestedVisual = table.Column<string>(type: "nvarchar(max)", nullable: true),
            GridX = table.Column<int>(type: "int", nullable: false),
            GridY = table.Column<int>(type: "int", nullable: false),
            GridW = table.Column<int>(type: "int", nullable: false),
            GridH = table.Column<int>(type: "int", nullable: false),
            CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
            LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_RemoteVisualizationConfigs", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "VisualizerInfos",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
            CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
            LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_VisualizerInfos", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "Visualizers",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
            CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
            LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Visualizers", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "VisualizerSettings",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            VisualizerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
            Settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
            CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
            LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_VisualizerSettings", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "VisualizerAllocation",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            VisualizerUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            RequestedVisual = table.Column<string>(type: "nvarchar(max)", nullable: true),
            VisualizationMaps = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
            UserProvidedIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
            CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
            LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_VisualizerAllocation", x => x.UniqueId);
            table.ForeignKey(
                      name: "FK_VisualizerAllocation_VisualizerInfos_VisualizerUniqueId",
                      column: x => x.VisualizerUniqueId,
                      principalTable: "VisualizerInfos",
                      principalColumn: "UniqueId");
          });

      migrationBuilder.CreateIndex(
          name: "IX_VisualizerAllocation_VisualizerUniqueId",
          table: "VisualizerAllocation",
          column: "VisualizerUniqueId");

      migrationBuilder.CreateIndex(
          name: "IX_VisualizerSettings_VisualizerId",
          table: "VisualizerSettings",
          column: "VisualizerId",
          unique: true,
          filter: "[VisualizerId] IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "RemoteVisualizationConfigs");

      migrationBuilder.DropTable(
          name: "VisualizerAllocation");

      migrationBuilder.DropTable(
          name: "Visualizers");

      migrationBuilder.DropTable(
          name: "VisualizerSettings");

      migrationBuilder.DropTable(
          name: "VisualizerInfos");

      migrationBuilder.DropColumn(
          name: "VisualizerAllocations",
          table: "ExperimentTemplates");

      migrationBuilder.DropColumn(
          name: "GeneratedVisuals",
          table: "ExperimentExecutionSummaries");

      migrationBuilder.DropColumn(
          name: "NameFoldersWithLocalTime",
          table: "AresGeneralSettingsConfig");
    }
  }
}