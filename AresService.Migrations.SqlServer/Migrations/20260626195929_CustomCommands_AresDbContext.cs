using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
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
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCommands", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CustomCommandParameters",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Schema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    CustomCommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
