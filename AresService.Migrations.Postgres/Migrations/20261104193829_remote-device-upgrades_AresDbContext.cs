using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
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
                type: "text",
                nullable: false, 
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "WidgetDescriptor",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssociatedInput = table.Column<string>(type: "text", nullable: true),
                    WidgetType = table.Column<int>(type: "integer", nullable: false),
                    SliderStepSize = table.Column<float>(type: "real", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
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