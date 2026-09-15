using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
  /// <inheritdoc />
  public partial class FixPlanObjectiveNullability_AresDbContext : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql("UPDATE [ExperimentTemplates] SET [PlanObjectives] = '[]' WHERE [PlanObjectives] IS NULL;");

      migrationBuilder.AlterColumn<string>(
          name: "PlanObjectives",
          table: "ExperimentTemplates",
          type: "nvarchar(max)",
          nullable: false,
          defaultValue: "[]",
          oldClrType: typeof(string),
          oldType: "nvarchar(max)",
          oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
  }
}
