using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
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
                name: "VariableArgument",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "VariableType",
                table: "Parameters");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Parameters",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "DeviceCommandResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OutputVarName",
                table: "CommandTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "CommandExecutionSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VarName",
                table: "CommandExecutionSummaries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Parameters");

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

            migrationBuilder.AddColumn<bool>(
                name: "EnvironmentBased",
                table: "Parameters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Planned",
                table: "Parameters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanningMetadataUniqueId",
                table: "Parameters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "Parameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariableArgument",
                table: "Parameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariableType",
                table: "Parameters",
                type: "integer",
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
