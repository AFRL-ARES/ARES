using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
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
          type: "nvarchar(max)",
          nullable: true);

      migrationBuilder.AddColumn<bool>(
          name: "MultiObjectiveCapable",
          table: "PlannerServiceCapabilities",
          type: "bit",
          nullable: false,
          defaultValue: false);

      migrationBuilder.AddColumn<string>(
          name: "AnalyzerResponse",
          table: "AnalyzerTransactions",
          type: "nvarchar(max)",
          nullable: false,
          defaultValue: "{}");

      migrationBuilder.AddColumn<string>(
          name: "ObjectiveOutputSchema",
          table: "AnalyzerCapabilities",
          type: "nvarchar(max)",
          nullable: true);

      migrationBuilder.AddColumn<string>(
          name: "Objectives",
          table: "AnalysisOverview",
          type: "nvarchar(max)",
          nullable: false,
          defaultValue: "[]");

      migrationBuilder.CreateTable(
          name: "AnalysisResponses",
          columns: table => new
          {
            UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Objectives = table.Column<string>(type: "nvarchar(max)", nullable: true),
            AnalysisOutcome = table.Column<int>(type: "int", nullable: false),
            ErrorString = table.Column<string>(type: "nvarchar(max)", nullable: true),
            CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
            LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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
          type: "nvarchar(max)",
          nullable: true);
    }
  }
}