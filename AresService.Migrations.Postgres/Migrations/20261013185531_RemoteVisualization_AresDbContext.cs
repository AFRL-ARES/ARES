using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
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
          type: "text",
          nullable: false,
          defaultValue: "[]");

      migrationBuilder.AddColumn<string>(
          name: "GeneratedVisuals",
          table: "ExperimentExecutionSummaries",
          type: "text",
          nullable: false,
          defaultValue: "{}");

      migrationBuilder.AddColumn<bool>(
          name: "NameFoldersWithLocalTime",
          table: "AresGeneralSettingsConfig",
          type: "boolean",
          nullable: false,
          defaultValue: false);

      migrationBuilder.CreateTable(
          name: "RemoteVisualizationConfigs",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
            ChartTitle = table.Column<string>(type: "text", nullable: true),
            VisualizerId = table.Column<string>(type: "text", nullable: true),
            RequestedVisual = table.Column<string>(type: "text", nullable: true),
            GridX = table.Column<int>(type: "integer", nullable: false),
            GridY = table.Column<int>(type: "integer", nullable: false),
            GridW = table.Column<int>(type: "integer", nullable: false),
            GridH = table.Column<int>(type: "integer", nullable: false),
            CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_RemoteVisualizationConfigs", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "VisualizerInfos",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "text", nullable: true),
            Type = table.Column<string>(type: "text", nullable: true),
            Description = table.Column<string>(type: "text", nullable: true),
            Version = table.Column<string>(type: "text", nullable: true),
            Url = table.Column<string>(type: "text", nullable: true),
            CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_VisualizerInfos", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "Visualizers",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "text", nullable: true),
            Url = table.Column<string>(type: "text", nullable: true),
            CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Visualizers", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "VisualizerSettings",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
            VisualizerId = table.Column<string>(type: "text", nullable: true),
            Settings = table.Column<string>(type: "text", nullable: true),
            CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_VisualizerSettings", x => x.UniqueId);
          });

      migrationBuilder.CreateTable(
          name: "VisualizerAllocation",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
            VisualizerUniqueId = table.Column<Guid>(type: "uuid", nullable: true),
            RequestedVisual = table.Column<string>(type: "text", nullable: true),
            VisualizationMaps = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
            UserProvidedIdentifier = table.Column<string>(type: "text", nullable: true),
            CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
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
          unique: true);
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