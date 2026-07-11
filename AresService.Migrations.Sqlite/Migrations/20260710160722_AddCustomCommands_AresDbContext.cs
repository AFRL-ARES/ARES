using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SystemCommand_Operation",
                table: "CommandTemplates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceCommandId",
                table: "CommandMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomCommandVersions",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomCommandId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OutputSchema = table.Column<string>(type: "TEXT", nullable: true),
                    ScriptBody = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
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
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommandTemplateId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
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
                INSERT INTO "DeviceCommands" ("UniqueId", "CommandTemplateId")
                SELECT "UniqueId", "UniqueId"
                FROM "CommandTemplates";

                UPDATE "CommandMetadata"
                SET "DeviceCommandId" = "CommandTemplateId";
                """);

            migrationBuilder.DropColumn(
                name: "CommandTemplateId",
                table: "CommandMetadata");

            migrationBuilder.CreateIndex(
                name: "IX_CommandMetadata_DeviceCommandId",
                table: "CommandMetadata",
                column: "DeviceCommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomCommandVersions_CustomCommandId_VersionNumber",
                table: "CustomCommandVersions",
                columns: new[] { "CustomCommandId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommands_CommandTemplateId",
                table: "DeviceCommands",
                column: "CommandTemplateId",
                unique: true);

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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CustomCommands",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputSchema",
                table: "CustomCommands",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommandTemplateId",
                table: "CommandMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "CommandMetadata"
                SET "CommandTemplateId" = (
                    SELECT "CommandTemplateId"
                    FROM "DeviceCommands"
                    WHERE "DeviceCommands"."UniqueId" = "CommandMetadata"."DeviceCommandId"
                );
                """);

            migrationBuilder.DropColumn(
                name: "DeviceCommandId",
                table: "CommandMetadata");

            migrationBuilder.Sql("PRAGMA foreign_keys = 0;", suppressTransaction: true);

            migrationBuilder.DropTable(
                name: "DeviceCommands");

            migrationBuilder.AlterColumn<Guid>(
                name: "CommandTemplateId",
                table: "CommandMetadata",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
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

            migrationBuilder.Sql("PRAGMA foreign_keys = 1;", suppressTransaction: true);
        }
    }
}
