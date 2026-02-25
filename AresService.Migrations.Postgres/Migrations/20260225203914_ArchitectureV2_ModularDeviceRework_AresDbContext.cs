using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.Postgres.Migrations.AresDb
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

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "PlannerSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SettingsSchema",
                table: "PlannerServiceCapabilities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "Parameters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Schema",
                table: "ParameterMetadata",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InitialValue",
                table: "ParameterMetadata",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataSchema",
                table: "OutputMetadata",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Minimum",
                table: "Limits",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "Maximum",
                table: "Limits",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "ExperimentOverviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "DeviceStates",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "DeviceSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SettingsSchema",
                table: "DeviceInfos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                table: "DeviceConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "DeviceConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EthernetUniqueId",
                table: "DeviceConfigs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSimulated",
                table: "DeviceConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SerialUniqueId",
                table: "DeviceConfigs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsbUniqueId",
                table: "DeviceConfigs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "DeviceCommandResults",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OutputSchema",
                table: "DeviceCommandDescriptor",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InputSchema",
                table: "DeviceCommandDescriptor",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "AnalyzerSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SettingsSchema",
                table: "AnalyzerCapabilities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "EthernetConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    UseTls = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthernetConnection", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "SerialConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    PortName = table.Column<string>(type: "text", nullable: true),
                    BaudRate = table.Column<int>(type: "integer", nullable: false),
                    DataBits = table.Column<int>(type: "integer", nullable: false),
                    Parity = table.Column<int>(type: "integer", nullable: false),
                    StopBits = table.Column<string>(type: "text", nullable: true),
                    Handshake = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialConnection", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "UsbConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<string>(type: "text", nullable: true),
                    ProductId = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
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

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "PlannerSettings",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SettingsSchema",
                table: "PlannerServiceCapabilities",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "Parameters",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Schema",
                table: "ParameterMetadata",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InitialValue",
                table: "ParameterMetadata",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExtraInfoUniqueId",
                table: "ParameterMetadata",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataSchema",
                table: "OutputMetadata",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Minimum",
                table: "Limits",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<float>(
                name: "Maximum",
                table: "Limits",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<long>(
                name: "Index",
                table: "Limits",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "ExperimentOverviews",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "DeviceStates",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "DeviceSettings",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SettingsSchema",
                table: "DeviceInfos",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "DeviceCommandResults",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OutputSchema",
                table: "DeviceCommandDescriptor",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InputSchema",
                table: "DeviceCommandDescriptor",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Settings",
                table: "AnalyzerSettings",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SettingsSchema",
                table: "AnalyzerCapabilities",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Any",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeviceConfigId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    TypeUrl = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<byte[]>(type: "bytea", nullable: true)
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
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ManifoldTemperature = table.Column<double>(type: "double precision", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChillerStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "MfcStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    AbsolutePressure = table.Column<double>(type: "double precision", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    Gas = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    MassFlow = table.Column<double>(type: "double precision", nullable: true),
                    Setpoint = table.Column<double>(type: "double precision", nullable: true),
                    StatusCodes = table.Column<string>(type: "text", nullable: true),
                    Temperature = table.Column<double>(type: "double precision", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VolumetricFlow = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfcStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "RestDeviceStateEntities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestDeviceStateEntities", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "RestSerialDeviceStateEntities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestSerialDeviceStateEntities", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "SyringePumpStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    DispensedVolume = table.Column<double>(type: "double precision", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    RateUnit = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VolumeUnit = table.Column<string>(type: "text", nullable: false),
                    WithdrawnVolume = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyringePumpStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Tc0304States",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Probe1Temperature = table.Column<double>(type: "double precision", nullable: true),
                    Probe2Temperature = table.Column<double>(type: "double precision", nullable: true),
                    Probe3Temperature = table.Column<double>(type: "double precision", nullable: true),
                    Probe4Temperature = table.Column<double>(type: "double precision", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tc0304States", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "TicStepperControllerStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CurrentPosition = table.Column<int>(type: "integer", nullable: false),
                    CustomStepSize = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    MaxAcceleration = table.Column<long>(type: "bigint", nullable: false),
                    MaxDeceleration = table.Column<long>(type: "bigint", nullable: false),
                    MaxSpeed = table.Column<long>(type: "bigint", nullable: false),
                    StartingSpeed = table.Column<long>(type: "bigint", nullable: false),
                    StatusMessages = table.Column<string>(type: "text", nullable: true),
                    StepMode = table.Column<string>(type: "text", nullable: false),
                    TargetPosition = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicStepperControllerStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "TubeFurnaceStateEntities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CurrentTemp = table.Column<double>(type: "double precision", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SetPointTemp = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParameterMetadata_Any_ExtraInfoUniqueId",
                table: "ParameterMetadata",
                column: "ExtraInfoUniqueId",
                principalTable: "Any",
                principalColumn: "UniqueId");
        }
    }
}
