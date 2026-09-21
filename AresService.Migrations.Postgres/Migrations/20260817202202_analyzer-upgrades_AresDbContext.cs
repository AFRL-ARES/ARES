using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
{
  /// <inheritdoc />
  public partial class analyzerupgrades_AresDbContext : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "PlanObjectives",
          table: "ExperimentTemplates",
          type: "text",
          nullable: true);

      migrationBuilder.AddColumn<bool>(
          name: "MultiObjectiveCapable",
          table: "PlannerServiceCapabilities",
          type: "boolean",
          nullable: false,
          defaultValue: false);

      migrationBuilder.AddColumn<string>(
          name: "AnalyzerResponse",
          table: "AnalyzerTransactions",
          type: "text",
          nullable: false,
          defaultValue: "{}");

      migrationBuilder.AddColumn<string>(
          name: "ObjectiveOutputSchema",
          table: "AnalyzerCapabilities",
          type: "text",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "Objectives",
          table: "AnalysisOverview",
          type: "text",
          nullable: false,
          defaultValue: "[]");

      migrationBuilder.CreateTable(
          name: "AnalysisResponses",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
            Objectives = table.Column<string>(type: "text", nullable: true),
            AnalysisOutcome = table.Column<int>(type: "integer", nullable: false),
            ErrorString = table.Column<string>(type: "text", nullable: true),
            CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
            LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_AnalysisResponses", x => x.UniqueId);
          });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "AnalysisResponses");

      migrationBuilder.DropColumn(
          name: "MultiObjectiveCapable",
          table: "PlannerServiceCapabilities");

      migrationBuilder.DropColumn(
          name: "PlanObjectives",
          table: "ExperimentTemplates");

      migrationBuilder.DropColumn(
          name: "AnalyzerResponse",
          table: "AnalyzerTransactions");

      migrationBuilder.DropColumn(
          name: "ObjectiveOutputSchema",
          table: "AnalyzerCapabilities");

      migrationBuilder.DropColumn(
          name: "Objectives",
          table: "AnalysisOverview");

      migrationBuilder.AddColumn<string>(
          name: "Objectives",
          table: "Analyses",
          type: "text",
          nullable: true);
    }
  }
}