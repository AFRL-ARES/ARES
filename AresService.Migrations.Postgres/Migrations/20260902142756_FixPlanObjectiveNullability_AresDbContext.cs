using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
{
  /// <inheritdoc />
  public partial class FixPlanObjectiveNullability_AresDbContext : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql("UPDATE \"ExperimentTemplates\" SET \"PlanObjectives\" = '[]' WHERE \"PlanObjectives\" IS NULL;");

      migrationBuilder.AlterColumn<string>(
          name: "PlanObjectives",
          table: "ExperimentTemplates",
          type: "text",
          nullable: false,
          defaultValue: "[]",
          oldClrType: typeof(string),
          oldType: "text",
          oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
  }
}
