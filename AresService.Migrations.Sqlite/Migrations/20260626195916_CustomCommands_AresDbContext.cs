using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class CustomCommands_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomCommands",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OutputSchema = table.Column<string>(type: "TEXT", nullable: true),
                    ScriptBody = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCommands", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CustomCommandParameters",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Schema = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    CustomCommandId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCommandParameters", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CustomCommandParameters_CustomCommands_CustomCommandId",
                        column: x => x.CustomCommandId,
                        principalTable: "CustomCommands",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomCommandParameters_CustomCommandId",
                table: "CustomCommandParameters",
                column: "CustomCommandId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomCommandParameters");

            migrationBuilder.DropTable(
                name: "CustomCommands");
        }
    }
}
