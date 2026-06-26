using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb;

/// <inheritdoc />
public partial class AnalysisUpdates_AresDbContext : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.AddColumn<string>(
      name: "Objectives",
      table: "Analyses",
      type: "TEXT",
      nullable: true);

    migrationBuilder.DropColumn(
      name: "Result",
      table: "AnalysisOverview");

    migrationBuilder.DropColumn(
      name: "Result",
      table: "Analyses");

    migrationBuilder.Sql(@"
        UPDATE ""AnalyzerTransactions""
        SET ""AnalysisResponse"" = 
            '{""objectives"":[{""objectiveName"":""Legacy Result"",""objectiveValue"":{""numberValue"":' 
            || COALESCE(json_extract(""AnalysisResponse"", '$.result'), json_extract(""AnalysisResponse"", '$.Result')) 
            || '}}],""analysisOutcome"":' 
            || COALESCE(json_extract(""AnalysisResponse"", '$.analysisOutcome'), json_extract(""AnalysisResponse"", '$.analysis_outcome'), json_extract(""AnalysisResponse"", '$.AnalysisOutcome'), 0)
            || CASE 
                WHEN json_extract(""AnalysisResponse"", '$.errorString') IS NOT NULL THEN ',""errorString"":""' || json_extract(""AnalysisResponse"", '$.errorString') || '""'
                WHEN json_extract(""AnalysisResponse"", '$.error_string') IS NOT NULL THEN ',""errorString"":""' || json_extract(""AnalysisResponse"", '$.error_string') || '""'
                WHEN json_extract(""AnalysisResponse"", '$.ErrorString') IS NOT NULL THEN ',""errorString"":""' || json_extract(""AnalysisResponse"", '$.ErrorString') || '""'
                ELSE '' 
               END
            || '}'
        WHERE COALESCE(json_extract(""AnalysisResponse"", '$.result'), json_extract(""AnalysisResponse"", '$.Result')) IS NOT NULL;
    ");

    migrationBuilder.Sql(@"
        UPDATE ""PlannerTransactions""
        SET ""PlanningRequest"" = json_set(""PlanningRequest"", '$.analysisResults', json('[]'))
        WHERE json_array_length(""PlanningRequest"", '$.analysisResults') > 0
          AND json_extract(""PlanningRequest"", '$.analysisResults[0].objectives') IS NULL;
    ");
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.Sql(@"
        UPDATE ""AnalyzerTransactions""
        SET ""AnalysisResponse"" = 
            '{""result"":' || cast(json_extract(""AnalysisResponse"", '$.objectives[0].objectiveValue.numberValue') as REAL)
            || ',""analysisOutcome"":' || COALESCE(json_extract(""AnalysisResponse"", '$.analysisOutcome'), 0)
            || CASE 
                WHEN json_extract(""AnalysisResponse"", '$.errorString') IS NOT NULL 
                THEN ',""errorString"":""' || json_extract(""AnalysisResponse"", '$.errorString') || '""'
                ELSE '' 
               END
            || '}'
        WHERE json_extract(""AnalysisResponse"", '$.objectives') IS NOT NULL;
    ");

    migrationBuilder.AddColumn<float>(
      name: "Result",
      table: "AnalysisOverview",
      type: "REAL",
      nullable: false,
      defaultValue: 0f);

    migrationBuilder.AddColumn<float>(
      name: "Result",
      table: "Analyses",
      type: "REAL",
      nullable: true);

    migrationBuilder.Sql(@"
        UPDATE ""Analyses""
        SET ""Result"" = cast(json_extract(""Objectives"", '$[0].objectiveValue.numberValue') as REAL)
        WHERE ""Objectives"" IS NOT NULL AND ""Objectives"" <> '[]';
    ");

    migrationBuilder.DropColumn(
      name: "Objectives",
      table: "Analyses");
  }
}
