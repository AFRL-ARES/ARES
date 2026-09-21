using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
  /// <inheritdoc />
  public partial class FixPlanObjectiveNullability_AresDbContext : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql("UPDATE \"ExperimentTemplates\" SET \"PlanObjectives\" = '[]' WHERE \"PlanObjectives\" IS NULL;");

      migrationBuilder.AlterColumn<string>(
          name: "PlanObjectives",
          table: "ExperimentTemplates",
          type: "TEXT",
          nullable: false,
          defaultValue: "[]",
          oldClrType: typeof(string),
          oldType: "TEXT",
          oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
  }
}
