using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
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
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerName = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    VendorUri = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
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
