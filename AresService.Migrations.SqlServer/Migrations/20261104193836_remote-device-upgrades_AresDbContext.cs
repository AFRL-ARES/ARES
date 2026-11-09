using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
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
          type: "nvarchar(max)",
          nullable: false,
          defaultValue: "[]");

      migrationBuilder.CreateTable(
          name: "WidgetDescriptor",
          columns: table => new
          {
              UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
              AssociatedInput = table.Column<string>(type: "nvarchar(max)", nullable: true),
              WidgetType = table.Column<int>(type: "int", nullable: false),
              SliderStepSize = table.Column<float>(type: "real", nullable: false),
              CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
              LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
