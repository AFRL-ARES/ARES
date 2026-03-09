using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Sqlite.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class FinalizeSchemaCleanup_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceConfigs_EthernetConnection_EthernetUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceConfigs_SerialConnection_SerialUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceConfigs_UsbConnection_UsbUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropTable(
                name: "EthernetConnection");

            migrationBuilder.DropTable(
                name: "UsbConnection");

            migrationBuilder.DropIndex(
                name: "IX_DeviceConfigs_EthernetUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropIndex(
                name: "IX_DeviceConfigs_SerialUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "BaudRate",
                table: "SerialConnection");

            migrationBuilder.DropColumn(
                name: "DataBits",
                table: "SerialConnection");

            migrationBuilder.DropColumn(
                name: "Handshake",
                table: "SerialConnection");

            migrationBuilder.DropColumn(
                name: "Parity",
                table: "SerialConnection");

            migrationBuilder.DropColumn(
                name: "StopBits",
                table: "SerialConnection");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "DriverSettings",
                table: "DeviceConfigs");

            migrationBuilder.RenameColumn(
                name: "UsbUniqueId",
                table: "DeviceConfigs",
                newName: "SerialInfoUniqueId");

            migrationBuilder.RenameColumn(
                name: "SerialUniqueId",
                table: "DeviceConfigs",
                newName: "DeviceSettings");

            migrationBuilder.RenameColumn(
                name: "EthernetUniqueId",
                table: "DeviceConfigs",
                newName: "DeviceId");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceConfigs_UsbUniqueId",
                table: "DeviceConfigs",
                newName: "IX_DeviceConfigs_SerialInfoUniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceConfigs_SerialConnection_SerialInfoUniqueId",
                table: "DeviceConfigs",
                column: "SerialInfoUniqueId",
                principalTable: "SerialConnection",
                principalColumn: "UniqueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceConfigs_SerialConnection_SerialInfoUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.RenameColumn(
                name: "SerialInfoUniqueId",
                table: "DeviceConfigs",
                newName: "UsbUniqueId");

            migrationBuilder.RenameColumn(
                name: "DeviceSettings",
                table: "DeviceConfigs",
                newName: "SerialUniqueId");

            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "DeviceConfigs",
                newName: "EthernetUniqueId");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceConfigs_SerialInfoUniqueId",
                table: "DeviceConfigs",
                newName: "IX_DeviceConfigs_UsbUniqueId");

            migrationBuilder.AddColumn<int>(
                name: "BaudRate",
                table: "SerialConnection",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DataBits",
                table: "SerialConnection",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Handshake",
                table: "SerialConnection",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Parity",
                table: "SerialConnection",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StopBits",
                table: "SerialConnection",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "DeviceConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverSettings",
                table: "DeviceConfigs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EthernetConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    UseTls = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthernetConnection", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "UsbConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ProductId = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    VendorId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsbConnection", x => x.UniqueId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigs_EthernetUniqueId",
                table: "DeviceConfigs",
                column: "EthernetUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigs_SerialUniqueId",
                table: "DeviceConfigs",
                column: "SerialUniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceConfigs_EthernetConnection_EthernetUniqueId",
                table: "DeviceConfigs",
                column: "EthernetUniqueId",
                principalTable: "EthernetConnection",
                principalColumn: "UniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceConfigs_SerialConnection_SerialUniqueId",
                table: "DeviceConfigs",
                column: "SerialUniqueId",
                principalTable: "SerialConnection",
                principalColumn: "UniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceConfigs_UsbConnection_UsbUniqueId",
                table: "DeviceConfigs",
                column: "UsbUniqueId",
                principalTable: "UsbConnection",
                principalColumn: "UniqueId");
        }
    }
}
