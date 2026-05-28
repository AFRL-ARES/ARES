using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
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

            migrationBuilder.AddColumn<string>(
                name: "OutputVarName",
                table: "CommandTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VarName",
                table: "CommandExecutionSummaries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutputVarName",
                table: "CommandTemplates");

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
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Planned",
                table: "Parameters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanningMetadataUniqueId",
                table: "Parameters",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "Parameters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariableType",
                table: "Parameters",
                type: "int",
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
