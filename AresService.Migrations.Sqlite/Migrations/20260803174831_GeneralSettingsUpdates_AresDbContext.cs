using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class GeneralSettingsUpdates_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DisplayCompatabilityWarnings",
                table: "AresGeneralSettingsConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DisplayDataCollectionWidget",
                table: "AresGeneralSettingsConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayCompatabilityWarnings",
                table: "AresGeneralSettingsConfig");

            migrationBuilder.DropColumn(
                name: "DisplayDataCollectionWidget",
                table: "AresGeneralSettingsConfig");
        }
    }
}
