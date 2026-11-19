using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
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
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCommandInvocation_Requirement_Name",
                table: "CommandTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCommandInvocation_Requirement_OutputSchema",
                table: "CommandTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceCategory",
                table: "CommandMetadata",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeviceDriverId",
                table: "CommandMetadata",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceDriverVersion",
                table: "CommandMetadata",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomCommandParameterRequirement",
                columns: table => new
                {
                    CustomCommandRequirementCustomCommandInvocationCommandTemplate = table.Column<Guid>(name: "CustomCommandRequirementCustomCommandInvocationCommandTemplate~", type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UniqueId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Schema = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCommandParameterRequirement", x => new { x.CustomCommandRequirementCustomCommandInvocationCommandTemplate, x.Id });
                    table.ForeignKey(
                        name: "FK_CustomCommandParameterRequirement_CommandTemplates_CustomCo~",
                        column: x => x.CustomCommandRequirementCustomCommandInvocationCommandTemplate,
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
