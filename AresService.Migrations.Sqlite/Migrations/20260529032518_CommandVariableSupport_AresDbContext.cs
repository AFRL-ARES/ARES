using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class CommandVariableSupport_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_ParameterMetadata_PlanningMetadataUniqueId",
                table: "Parameters");

            migrationBuilder.DropIndex(
                name: "IX_Parameters_PlanningMetadataUniqueId",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "EnvironmentBased",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "Planned",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "PlanningMetadataUniqueId",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "VariableType",
                table: "Parameters");

            migrationBuilder.RenameColumn(
                name: "VariableArgument",
                table: "Parameters",
                newName: "Source");

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "DeviceCommandResults",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OutputVarName",
                table: "CommandTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "CommandExecutionSummaries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VarName",
                table: "CommandExecutionSummaries",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "DeviceCommandResults");

            migrationBuilder.DropColumn(
                name: "OutputVarName",
                table: "CommandTemplates");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "CommandExecutionSummaries");

            migrationBuilder.DropColumn(
                name: "VarName",
                table: "CommandExecutionSummaries");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "Parameters",
                newName: "VariableArgument");

            migrationBuilder.AddColumn<bool>(
                name: "EnvironmentBased",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Planned",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanningMetadataUniqueId",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariableType",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_PlanningMetadataUniqueId",
                table: "Parameters",
                column: "PlanningMetadataUniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parameters_ParameterMetadata_PlanningMetadataUniqueId",
                table: "Parameters",
                column: "PlanningMetadataUniqueId",
                principalTable: "ParameterMetadata",
                principalColumn: "UniqueId");
        }
    }
}
