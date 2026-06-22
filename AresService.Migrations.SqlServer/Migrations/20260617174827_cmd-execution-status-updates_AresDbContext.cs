using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class cmdexecutionstatusupdates_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "CommandExecutionStatuses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "CommandExecutionStatuses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariableName",
                table: "CommandExecutionStatuses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Result",
                table: "CommandExecutionStatuses");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "CommandExecutionStatuses");

            migrationBuilder.DropColumn(
                name: "VariableName",
                table: "CommandExecutionStatuses");
        }
    }
}
