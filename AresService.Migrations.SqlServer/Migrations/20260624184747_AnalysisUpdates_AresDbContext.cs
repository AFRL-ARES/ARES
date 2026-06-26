using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
  /// <inheritdoc />
  public partial class AnalysisUpdates_AresDbContext : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
        name: "Objectives",
        table: "Analyses",
        type: "nvarchar(max)",
        nullable: true);

      migrationBuilder.DropColumn(
        name: "Result",
        table: "AnalysisOverview");

      migrationBuilder.DropColumn(
        name: "Result",
        table: "Analyses");

      migrationBuilder.Sql(@"
        UPDATE [AnalyzerTransactions]
        SET [AnalysisResponse] = 
            '{""objectives"":[{""objectiveName"":""Legacy Result"",""objectiveValue"":{""numberValue"":' 
            + COALESCE(JSON_VALUE([AnalysisResponse], '$.result'), JSON_VALUE([AnalysisResponse], '$.Result')) 
            + '}}],""analysisOutcome"":' 
            + COALESCE(JSON_VALUE([AnalysisResponse], '$.analysisOutcome'), JSON_VALUE([AnalysisResponse], '$.analysis_outcome'), JSON_VALUE([AnalysisResponse], '$.AnalysisOutcome'), '0')
            + CASE 
                WHEN JSON_VALUE([AnalysisResponse], '$.errorString') IS NOT NULL THEN ',""errorString"":""' + JSON_VALUE([AnalysisResponse], '$.errorString') + '""'
                WHEN JSON_VALUE([AnalysisResponse], '$.error_string') IS NOT NULL THEN ',""errorString"":""' + JSON_VALUE([AnalysisResponse], '$.error_string') + '""'
                WHEN JSON_VALUE([AnalysisResponse], '$.ErrorString') IS NOT NULL THEN ',""errorString"":""' + JSON_VALUE([AnalysisResponse], '$.ErrorString') + '""'
                ELSE '' 
               END
            + '}'
        WHERE COALESCE(JSON_VALUE([AnalysisResponse], '$.result'), JSON_VALUE([AnalysisResponse], '$.Result')) IS NOT NULL;
    ");

      migrationBuilder.Sql(@"
        UPDATE [PlannerTransactions]
        SET [PlanningRequest] = JSON_MODIFY([PlanningRequest], '$.analysisResults', JSON_QUERY('[]'))
        WHERE JSON_QUERY([PlanningRequest], '$.analysisResults') IS NOT NULL 
          AND JSON_QUERY([PlanningRequest], '$.analysisResults') <> '[]'
          AND JSON_QUERY([PlanningRequest], '$.analysisResults[0].objectives') IS NULL;
    ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      // AnalyzerTransactions: Revert back to the legacy JSON structure
      migrationBuilder.Sql(@"
        UPDATE [AnalyzerTransactions]
        SET [AnalysisResponse] = 
            '{""result"":' + JSON_VALUE([AnalysisResponse], '$.objectives[0].objectiveValue.numberValue')
            + ',""analysisOutcome"":' + COALESCE(JSON_VALUE([AnalysisResponse], '$.analysisOutcome'), '0')
            + CASE 
                WHEN JSON_VALUE([AnalysisResponse], '$.errorString') IS NOT NULL 
                THEN ',""errorString"":""' + JSON_VALUE([AnalysisResponse], '$.errorString') + '""'
                ELSE '' 
               END
            + '}'
        WHERE JSON_QUERY([AnalysisResponse], '$.objectives') IS NOT NULL;
    ");

      migrationBuilder.AddColumn<float>(
        name: "Result",
        table: "AnalysisOverview",
        type: "real",
        nullable: false,
        defaultValue: 0f);

      migrationBuilder.AddColumn<float>(
        name: "Result",
        table: "Analyses",
        type: "real",
        nullable: true);

      migrationBuilder.DropColumn(
        name: "Objectives",
        table: "Analyses");
    }
  }
}
