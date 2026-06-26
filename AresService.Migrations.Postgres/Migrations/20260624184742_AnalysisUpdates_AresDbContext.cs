using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
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
        type: "jsonb",
        nullable: true);

      migrationBuilder.DropColumn(
        name: "Result",
        table: "AnalysisOverview");

      migrationBuilder.DropColumn(
        name: "Result",
        table: "Analyses");

      migrationBuilder.Sql(@"
        UPDATE ""AnalyzerTransactions""
        SET ""AnalysisResponse"" = (
            '{""objectives"":[{""objectiveName"":""Legacy Result"",""objectiveValue"":{""numberValue"":' 
            || COALESCE(""AnalysisResponse""->>'result', ""AnalysisResponse""->>'Result') 
            || '}}],""analysisOutcome"":' 
            || COALESCE(""AnalysisResponse""->>'analysisOutcome', ""AnalysisResponse""->>'analysis_outcome', ""AnalysisResponse""->>'AnalysisOutcome', '0')
            || CASE 
                WHEN ""AnalysisResponse""->>'errorString' IS NOT NULL THEN ',""errorString"":""' || (""AnalysisResponse""->>'errorString') || '""'
                WHEN ""AnalysisResponse""->>'error_string' IS NOT NULL THEN ',""errorString"":""' || (""AnalysisResponse""->>'error_string') || '""'
                WHEN ""AnalysisResponse""->>'ErrorString' IS NOT NULL THEN ',""errorString"":""' || (""AnalysisResponse""->>'ErrorString') || '""'
                ELSE '' 
                END
            || '}'
        )::jsonb
        WHERE COALESCE(""AnalysisResponse""->>'result', ""AnalysisResponse""->>'Result') IS NOT NULL;
    ");

      // PlannerTransactions: Wipe the array if legacy data is detected
      // Note: We check if it's an array first, then use path traversal '#>' to check for objectives
      migrationBuilder.Sql(@"
        UPDATE ""PlannerTransactions""
        SET ""PlanningRequest"" = jsonb_set(""PlanningRequest"", '{analysisResults}', '[]'::jsonb)
        WHERE jsonb_typeof(""PlanningRequest""->'analysisResults') = 'array'
          AND jsonb_array_length(""PlanningRequest""->'analysisResults') > 0
          AND ""PlanningRequest""#>'{analysisResults,0,objectives}' IS NULL;
    ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql(@"
        UPDATE ""AnalyzerTransactions""
        SET ""AnalysisResponse"" = (
            '{""result"":' || (""AnalysisResponse""#>>'{objectives,0,objectiveValue,numberValue}')
            || ',""analysisOutcome"":' || COALESCE(""AnalysisResponse""->>'analysisOutcome', '0')
            || CASE 
                WHEN ""AnalysisResponse""->>'errorString' IS NOT NULL 
                THEN ',""errorString"":""' || (""AnalysisResponse""->>'errorString') || '""'
                ELSE '' 
               END
            || '}'
        )::jsonb
        WHERE ""AnalysisResponse""->'objectives' IS NOT NULL;
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
