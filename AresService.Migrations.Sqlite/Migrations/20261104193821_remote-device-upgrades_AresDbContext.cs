using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class remotedeviceupgrades_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WidgetDescriptors",
                table: "DeviceCommandDescriptor",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "WidgetDescriptor",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssociatedInput = table.Column<string>(type: "TEXT", nullable: true),
                    WidgetType = table.Column<int>(type: "INTEGER", nullable: false),
                    SliderStepSize = table.Column<float>(type: "REAL", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetDescriptor", x => x.UniqueId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WidgetDescriptor");

            migrationBuilder.DropColumn(
                name: "WidgetDescriptors",
                table: "DeviceCommandDescriptor");
        }
    }
}
