using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class silasupport_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SilaConfigs",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    VendorUri = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SilaConfigs", x => x.UniqueId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SilaConfigs");
        }
    }
}
