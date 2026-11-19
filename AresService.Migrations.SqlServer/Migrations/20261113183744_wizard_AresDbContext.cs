using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class wizard_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomCommandInvocation_Requirement_Description",
                table: "CommandTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCommandInvocation_Requirement_Name",
                table: "CommandTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCommandInvocation_Requirement_OutputSchema",
                table: "CommandTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceCategory",
                table: "CommandMetadata",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeviceDriverId",
                table: "CommandMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceDriverVersion",
                table: "CommandMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomCommandParameterRequirement",
                columns: table => new
                {
                    CustomCommandRequirementCustomCommandInvocationCommandTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniqueId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Schema = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCommandParameterRequirement", x => new { x.CustomCommandRequirementCustomCommandInvocationCommandTemplateUniqueId, x.Id });
                    table.ForeignKey(
                        name: "FK_CustomCommandParameterRequirement_CommandTemplates_CustomCommandRequirementCustomCommandInvocationCommandTemplateUniqueId",
                        column: x => x.CustomCommandRequirementCustomCommandInvocationCommandTemplateUniqueId,
                        principalTable: "CommandTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomCommandParameterRequirement");

            migrationBuilder.DropColumn(
                name: "CustomCommandInvocation_Requirement_Description",
                table: "CommandTemplates");

            migrationBuilder.DropColumn(
                name: "CustomCommandInvocation_Requirement_Name",
                table: "CommandTemplates");

            migrationBuilder.DropColumn(
                name: "CustomCommandInvocation_Requirement_OutputSchema",
                table: "CommandTemplates");

            migrationBuilder.DropColumn(
                name: "DeviceCategory",
                table: "CommandMetadata");

            migrationBuilder.DropColumn(
                name: "DeviceDriverId",
                table: "CommandMetadata");

            migrationBuilder.DropColumn(
                name: "DeviceDriverVersion",
                table: "CommandMetadata");
        }
    }
}
