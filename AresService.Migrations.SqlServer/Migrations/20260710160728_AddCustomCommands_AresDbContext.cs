using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class AddCustomCommands_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandMetadata_CommandTemplates_CommandTemplateId",
                table: "CommandMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomCommandParameters_CustomCommands_CustomCommandId",
                table: "CustomCommandParameters");

            migrationBuilder.DropIndex(
                name: "IX_CommandMetadata_CommandTemplateId",
                table: "CommandMetadata");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CustomCommands");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CustomCommands");

            migrationBuilder.DropColumn(
                name: "OutputSchema",
                table: "CustomCommands");

            migrationBuilder.RenameColumn(
                name: "ScriptBody",
                table: "CustomCommands",
                newName: "CurrentVersionId");

            migrationBuilder.RenameColumn(
                name: "CustomCommandId",
                table: "CustomCommandParameters",
                newName: "CustomCommandVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomCommandParameters_CustomCommandId",
                table: "CustomCommandParameters",
                newName: "IX_CustomCommandParameters_CustomCommandVersionId");

            migrationBuilder.AddColumn<string>(
                name: "CustomCommandInvocation_CustomCommandId",
                table: "CommandTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SystemCommand_Operation",
                table: "CommandTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceCommandId",
                table: "CommandMetadata",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomCommandVersions",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomCommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCommandVersions", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CustomCommandVersions_CustomCommands_CustomCommandId",
                        column: x => x.CustomCommandId,
                        principalTable: "CustomCommands",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommands",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommands", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_DeviceCommands_CommandTemplates_CommandTemplateId",
                        column: x => x.CommandTemplateId,
                        principalTable: "CommandTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [DeviceCommands] ([UniqueId], [CommandTemplateId])
                SELECT [UniqueId], [UniqueId]
                FROM [CommandTemplates];

                UPDATE [CommandMetadata]
                SET [DeviceCommandId] = [CommandTemplateId];
                """);

            migrationBuilder.DropColumn(
                name: "CommandTemplateId",
                table: "CommandMetadata");

            migrationBuilder.CreateIndex(
                name: "IX_CommandMetadata_DeviceCommandId",
                table: "CommandMetadata",
                column: "DeviceCommandId",
                unique: true,
                filter: "[DeviceCommandId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomCommandVersions_CustomCommandId_VersionNumber",
                table: "CustomCommandVersions",
                columns: new[] { "CustomCommandId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_CommandTemplateId",
                table: "DeviceCommands",
                column: "CommandTemplateId",
                unique: true,
                filter: "[CommandTemplateId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandMetadata_DeviceCommands_DeviceCommandId",
                table: "CommandMetadata",
                column: "DeviceCommandId",
                principalTable: "DeviceCommands",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomCommandParameters_CustomCommandVersions_CustomCommandVersionId",
                table: "CustomCommandParameters",
                column: "CustomCommandVersionId",
                principalTable: "CustomCommandVersions",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandMetadata_DeviceCommands_DeviceCommandId",
                table: "CommandMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomCommandParameters_CustomCommandVersions_CustomCommandVersionId",
                table: "CustomCommandParameters");

            migrationBuilder.DropTable(
                name: "CustomCommandVersions");

            migrationBuilder.DropIndex(
                name: "IX_CommandMetadata_DeviceCommandId",
                table: "CommandMetadata");

            migrationBuilder.DropColumn(
                name: "CustomCommandInvocation_CustomCommandId",
                table: "CommandTemplates");

            migrationBuilder.DropColumn(
                name: "SystemCommand_Operation",
                table: "CommandTemplates");

            migrationBuilder.RenameColumn(
                name: "CurrentVersionId",
                table: "CustomCommands",
                newName: "ScriptBody");

            migrationBuilder.RenameColumn(
                name: "CustomCommandVersionId",
                table: "CustomCommandParameters",
                newName: "CustomCommandId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomCommandParameters_CustomCommandVersionId",
                table: "CustomCommandParameters",
                newName: "IX_CustomCommandParameters_CustomCommandId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CustomCommands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CustomCommands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputSchema",
                table: "CustomCommands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommandTemplateId",
                table: "CommandMetadata",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE metadata
                SET metadata.[CommandTemplateId] = commands.[CommandTemplateId]
                FROM [CommandMetadata] AS metadata
                INNER JOIN [DeviceCommands] AS commands
                    ON commands.[UniqueId] = metadata.[DeviceCommandId];
                """);

            migrationBuilder.DropColumn(
                name: "DeviceCommandId",
                table: "CommandMetadata");

            migrationBuilder.DropTable(
                name: "DeviceCommands");

            migrationBuilder.AlterColumn<Guid>(
                name: "CommandTemplateId",
                table: "CommandMetadata",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandMetadata_CommandTemplateId",
                table: "CommandMetadata",
                column: "CommandTemplateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandMetadata_CommandTemplates_CommandTemplateId",
                table: "CommandMetadata",
                column: "CommandTemplateId",
                principalTable: "CommandTemplates",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomCommandParameters_CustomCommands_CustomCommandId",
                table: "CustomCommandParameters",
                column: "CustomCommandId",
                principalTable: "CustomCommands",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
