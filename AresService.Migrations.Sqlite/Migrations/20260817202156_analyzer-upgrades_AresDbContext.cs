using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
              name: "MultiObjectiveCapable",
              table: "PlannerServiceCapabilities",
              type: "INTEGER",
              nullable: false,
              defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AnalyzerResponse",
                table: "AnalyzerTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "ObjectiveOutputSchema",
                table: "AnalyzerCapabilities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Objectives",
                table: "AnalysisOverview",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "AnalysisResponses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Objectives = table.Column<string>(type: "TEXT", nullable: true),
                    AnalysisOutcome = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorString = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
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
                type: "TEXT",
                nullable: true);
        }
    }
}
