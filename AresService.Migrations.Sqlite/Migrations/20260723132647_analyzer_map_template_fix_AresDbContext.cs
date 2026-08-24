using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class analyzer_map_template_fix_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalyzerMaps",
                table: "ExperimentTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
