using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations.SqlServer.Migrations.AresDb
{
    /// <inheritdoc />
    public partial class DeviceRework_AresDbContext : Migration
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

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "DeviceConfigs");

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                table: "DeviceConfigs",
                type: "nvarchar(max)",
                nullable: true);

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
                name: "DeviceId",
                table: "DeviceConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceSettings",
                table: "DeviceConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSimulated",
                table: "DeviceConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SerialInfoUniqueId",
                table: "DeviceConfigs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateId",
                table: "CommandExecutionSummaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceDrivers",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceDrivers", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "SerialConnection",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaudRate = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialConnection", x => x.UniqueId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigs_SerialInfoUniqueId",
                table: "DeviceConfigs",
                column: "SerialInfoUniqueId");

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

            migrationBuilder.DropTable(
                name: "DeviceDrivers");

            migrationBuilder.DropTable(
                name: "SerialConnection");

            migrationBuilder.DropIndex(
                name: "IX_DeviceConfigs_SerialInfoUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "DeviceSettings",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "IsSimulated",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "SerialInfoUniqueId",
                table: "DeviceConfigs");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "CommandExecutionSummaries");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "DeviceConfigs");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "DeviceConfigs",
                type: "nvarchar(max)",
                nullable: true);

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
