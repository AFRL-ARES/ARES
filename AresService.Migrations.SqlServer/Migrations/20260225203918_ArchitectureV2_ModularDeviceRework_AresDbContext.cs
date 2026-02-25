using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class ArchitectureV2_ModularDeviceRework_AresDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParameterMetadata_Any_ExtraInfoUniqueId",
                table: "ParameterMetadata");

            migrationBuilder.DropTable(
                name: "Any");

            migrationBuilder.DropTable(
                name: "ChillerStates");

            migrationBuilder.DropTable(
                name: "MfcStates");

            migrationBuilder.DropTable(
                name: "RestDeviceStateEntities");

            migrationBuilder.DropTable(
                name: "RestSerialDeviceStateEntities");

            migrationBuilder.DropTable(
                name: "SyringePumpStates");

            migrationBuilder.DropTable(
                name: "Tc0304States");

            migrationBuilder.DropTable(
                name: "TicStepperControllerStates");

            migrationBuilder.DropTable(
                name: "TubeFurnaceStateEntities");

            migrationBuilder.DropIndex(
                name: "IX_ParameterMetadata_ExtraInfoUniqueId",
                table: "ParameterMetadata");

            migrationBuilder.DropColumn(
                name: "ExtraInfoUniqueId",
                table: "ParameterMetadata");

            migrationBuilder.DropColumn(
                name: "Index",
                table: "Limits");

            migrationBuilder.RenameColumn(
                name: "DeviceType",
                table: "DeviceConfigs",
                newName: "DriverSettings");

            migrationBuilder.AlterColumn<double>(
                name: "Minimum",
                table: "Limits",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "Maximum",
                table: "Limits",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                table: "DeviceConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "DeviceConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EthernetUniqueId",
                table: "DeviceConfigs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSimulated",
                table: "DeviceConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SerialUniqueId",
                table: "DeviceConfigs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsbUniqueId",
                table: "DeviceConfigs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EthernetConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Port = table.Column<int>(type: "int", nullable: false),
                    UseTls = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthernetConnection", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "SerialConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaudRate = table.Column<int>(type: "int", nullable: false),
                    DataBits = table.Column<int>(type: "int", nullable: false),
                    Parity = table.Column<int>(type: "int", nullable: false),
                    StopBits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Handshake = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialConnection", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "UsbConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigs_UsbUniqueId",
                table: "DeviceConfigs",
                column: "UsbUniqueId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "SerialConnection");

            migrationBuilder.DropTable(
                name: "UsbConnection");

            migrationBuilder.DropIndex(
                name: "IX_DeviceConfigs_EthernetUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropIndex(
                name: "IX_DeviceConfigs_SerialUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropIndex(
                name: "IX_DeviceConfigs_UsbUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "EthernetUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "IsSimulated",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "SerialUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "UsbUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.RenameColumn(
                name: "DriverSettings",
                table: "DeviceConfigs",
                newName: "DeviceType");

            migrationBuilder.AddColumn<Guid>(
                name: "ExtraInfoUniqueId",
                table: "ParameterMetadata",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Minimum",
                table: "Limits",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<float>(
                name: "Maximum",
                table: "Limits",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<long>(
                name: "Index",
                table: "Limits",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "Any",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    TypeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Any", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Any_DeviceConfigs_DeviceConfigId",
                        column: x => x.DeviceConfigId,
                        principalTable: "DeviceConfigs",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "ChillerStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ManifoldTemperature = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChillerStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "MfcStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AbsolutePressure = table.Column<double>(type: "float", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    MassFlow = table.Column<double>(type: "float", nullable: true),
                    Setpoint = table.Column<double>(type: "float", nullable: true),
                    StatusCodes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Temperature = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VolumetricFlow = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfcStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "RestDeviceStateEntities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestDeviceStateEntities", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "RestSerialDeviceStateEntities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestSerialDeviceStateEntities", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "SyringePumpStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispensedVolume = table.Column<double>(type: "float", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    RateUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VolumeUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WithdrawnVolume = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyringePumpStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Tc0304States",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    Probe1Temperature = table.Column<double>(type: "float", nullable: true),
                    Probe2Temperature = table.Column<double>(type: "float", nullable: true),
                    Probe3Temperature = table.Column<double>(type: "float", nullable: true),
                    Probe4Temperature = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tc0304States", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "TicStepperControllerStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    CurrentPosition = table.Column<int>(type: "int", nullable: false),
                    CustomStepSize = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    MaxAcceleration = table.Column<long>(type: "bigint", nullable: false),
                    MaxDeceleration = table.Column<long>(type: "bigint", nullable: false),
                    MaxSpeed = table.Column<long>(type: "bigint", nullable: false),
                    StartingSpeed = table.Column<long>(type: "bigint", nullable: false),
                    StatusMessages = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetPosition = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicStepperControllerStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "TubeFurnaceStateEntities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    CurrentTemp = table.Column<double>(type: "float", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    SetPointTemp = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TubeFurnaceStateEntities", x => x.UniqueId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_ExtraInfoUniqueId",
                table: "ParameterMetadata",
                column: "ExtraInfoUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Any_DeviceConfigId",
                table: "Any",
                column: "DeviceConfigId",
                unique: true,
                filter: "[DeviceConfigId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ParameterMetadata_Any_ExtraInfoUniqueId",
                table: "ParameterMetadata",
                column: "ExtraInfoUniqueId",
                principalTable: "Any",
                principalColumn: "UniqueId");
        }
    }
}
