using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Analyses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Result = table.Column<float>(type: "real", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzerInfos",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerInfos", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Analyzers",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyzers", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzerSettings",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalyzerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerSettings", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignExecutionStatuses", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignTags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignExecutionSummaries", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignTags",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTags", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "ChillerStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManifoldTemperature = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChillerStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceConfigs",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConfigs", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "MfcStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AbsolutePressure = table.Column<double>(type: "float", nullable: true),
                    Temperature = table.Column<double>(type: "float", nullable: true),
                    VolumetricFlow = table.Column<double>(type: "float", nullable: true),
                    MassFlow = table.Column<double>(type: "float", nullable: true),
                    Setpoint = table.Column<double>(type: "float", nullable: true),
                    Gas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCodes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfcStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerRequests",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerRequests", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerResponses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerResponses", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Planners",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdapterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planners", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "RestDeviceStateEntities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispensedVolume = table.Column<double>(type: "float", nullable: true),
                    WithdrawnVolume = table.Column<double>(type: "float", nullable: true),
                    VolumeUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RateUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
                    Probe1Temperature = table.Column<double>(type: "float", nullable: true),
                    Probe2Temperature = table.Column<double>(type: "float", nullable: true),
                    Probe3Temperature = table.Column<double>(type: "float", nullable: true),
                    Probe4Temperature = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxAcceleration = table.Column<long>(type: "bigint", nullable: false),
                    MaxDeceleration = table.Column<long>(type: "bigint", nullable: false),
                    MaxSpeed = table.Column<long>(type: "bigint", nullable: false),
                    StartingSpeed = table.Column<long>(type: "bigint", nullable: false),
                    CustomStepSize = table.Column<long>(type: "bigint", nullable: false),
                    StepMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentPosition = table.Column<int>(type: "int", nullable: false),
                    TargetPosition = table.Column<int>(type: "int", nullable: false),
                    StatusMessages = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentTemp = table.Column<double>(type: "float", nullable: false),
                    SetPointTemp = table.Column<double>(type: "float", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TubeFurnaceStateEntities", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzerCapabilities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeoutSeconds = table.Column<long>(type: "bigint", nullable: false),
                    SettingsSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyzerInfoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerCapabilities", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_AnalyzerCapabilities_AnalyzerInfos_AnalyzerInfoId",
                        column: x => x.AnalyzerInfoId,
                        principalTable: "AnalyzerInfos",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignExecutionStatusUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentExecutionStatuses", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentExecutionStatuses_CampaignExecutionStatuses_CampaignExecutionStatusUniqueId",
                        column: x => x.CampaignExecutionStatusUniqueId,
                        principalTable: "CampaignExecutionStatuses",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultOutputPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignExecutionSummaryUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentExecutionSummaries", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentExecutionSummaries_CampaignExecutionSummaries_CampaignExecutionSummaryUniqueId",
                        column: x => x.CampaignExecutionSummaryUniqueId,
                        principalTable: "CampaignExecutionSummaries",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Any",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
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
                name: "StepExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentExecutionStatusUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepExecutionStatuses", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_StepExecutionStatuses_ExperimentExecutionStatuses_ExperimentExecutionStatusUniqueId",
                        column: x => x.ExperimentExecutionStatusUniqueId,
                        principalTable: "ExperimentExecutionStatuses",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentExecutionSummaryUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepExecutionSummaries", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_StepExecutionSummaries_ExperimentExecutionSummaries_ExperimentExecutionSummaryUniqueId",
                        column: x => x.ExperimentExecutionSummaryUniqueId,
                        principalTable: "ExperimentExecutionSummaries",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommandName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepExecutionStatusUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandExecutionStatuses", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandExecutionStatuses_StepExecutionStatuses_StepExecutionStatusUniqueId",
                        column: x => x.StepExecutionStatusUniqueId,
                        principalTable: "StepExecutionStatuses",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommandName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommandDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepExecutionSummaryUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandExecutionSummaries", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandExecutionSummaries_StepExecutionSummaries_StepExecutionSummaryUniqueId",
                        column: x => x.StepExecutionSummaryUniqueId,
                        principalTable: "StepExecutionSummaries",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommandResults",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AwaitUserInput = table.Column<bool>(type: "bit", nullable: false),
                    CommandExecutionSummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommandResults", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_DeviceCommandResults_CommandExecutionSummaries_CommandExecutionSummaryId",
                        column: x => x.CommandExecutionSummaryId,
                        principalTable: "CommandExecutionSummaries",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "ExecutionInfos",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeStarted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeFinished = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Timezone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocaltimeOffset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignExecutionSummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommandExecutionSummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepExecutionSummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionInfos", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_CampaignExecutionSummaries_CampaignExecutionSummaryId",
                        column: x => x.CampaignExecutionSummaryId,
                        principalTable: "CampaignExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_CommandExecutionSummaries_CommandExecutionSummaryId",
                        column: x => x.CommandExecutionSummaryId,
                        principalTable: "CommandExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_ExperimentExecutionSummaries_ExperimentResultId",
                        column: x => x.ExperimentResultId,
                        principalTable: "ExperimentExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_StepExecutionSummaries_StepExecutionSummaryId",
                        column: x => x.StepExecutionSummaryId,
                        principalTable: "StepExecutionSummaries",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "CampaignTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartupTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExperimentTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTemplates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalyzerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentTemplates", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentTemplates_CampaignTemplates_UniqueId",
                        column: x => x.UniqueId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentOverviews",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalysisResult = table.Column<double>(type: "float", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentOverviews", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentOverviews_ExperimentExecutionSummaries_ExperimentResultId",
                        column: x => x.ExperimentResultId,
                        principalTable: "ExperimentExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExperimentOverviews_ExperimentTemplates_TemplateUniqueId",
                        column: x => x.TemplateUniqueId,
                        principalTable: "ExperimentTemplates",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "StepTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsParallel = table.Column<bool>(type: "bit", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepTemplates", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_StepTemplates_ExperimentTemplates_ExperimentTemplateUniqueId",
                        column: x => x.ExperimentTemplateUniqueId,
                        principalTable: "ExperimentTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannerTransactions",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlannerInfoUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentOverviewUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTransactions", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_PlannerTransactions_ExperimentOverviews_ExperimentOverviewUniqueId",
                        column: x => x.ExperimentOverviewUniqueId,
                        principalTable: "ExperimentOverviews",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_PlannerTransactions_PlannerRequests_RequestUniqueId",
                        column: x => x.RequestUniqueId,
                        principalTable: "PlannerRequests",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_PlannerTransactions_PlannerResponses_ResponseUniqueId",
                        column: x => x.ResponseUniqueId,
                        principalTable: "PlannerResponses",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_PlannerTransactions_Planners_PlannerInfoUniqueId",
                        column: x => x.PlannerInfoUniqueId,
                        principalTable: "Planners",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplates", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandTemplates_StepTemplates_StepTemplateUniqueId",
                        column: x => x.StepTemplateUniqueId,
                        principalTable: "StepTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommandTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandMetadata", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandMetadata_CommandTemplates_CommandTemplateId",
                        column: x => x.CommandTemplateId,
                        principalTable: "CommandTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutputMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputMetadata", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_OutputMetadata_CommandMetadata_UniqueId",
                        column: x => x.UniqueId,
                        principalTable: "CommandMetadata",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Limits",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Minimum = table.Column<float>(type: "real", nullable: false),
                    Maximum = table.Column<float>(type: "real", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ParameterMetadataUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Limits", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "ParameterMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    NotPlannable = table.Column<bool>(type: "bit", nullable: false),
                    UseDefault = table.Column<bool>(type: "bit", nullable: false),
                    Schema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannerDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraInfoUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampaignTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommandMetadataUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlannerRequestUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterMetadata", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ParameterMetadata_Any_ExtraInfoUniqueId",
                        column: x => x.ExtraInfoUniqueId,
                        principalTable: "Any",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ParameterMetadata_CampaignTemplates_CampaignTemplateUniqueId",
                        column: x => x.CampaignTemplateUniqueId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ParameterMetadata_CommandMetadata_CommandMetadataUniqueId",
                        column: x => x.CommandMetadataUniqueId,
                        principalTable: "CommandMetadata",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ParameterMetadata_PlannerRequests_PlannerRequestUniqueId",
                        column: x => x.PlannerRequestUniqueId,
                        principalTable: "PlannerRequests",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Planned = table.Column<bool>(type: "bit", nullable: false),
                    EnvironmentBased = table.Column<bool>(type: "bit", nullable: false),
                    PlanningMetadataUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VariableType = table.Column<int>(type: "int", nullable: false),
                    VariableArgument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CommandTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentOverviewUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    PlannerResponseUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Parameters_CommandTemplates_CommandTemplateUniqueId",
                        column: x => x.CommandTemplateUniqueId,
                        principalTable: "CommandTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Parameters_ExperimentOverviews_ExperimentOverviewUniqueId",
                        column: x => x.ExperimentOverviewUniqueId,
                        principalTable: "ExperimentOverviews",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_Parameters_ParameterMetadata_PlanningMetadataUniqueId",
                        column: x => x.PlanningMetadataUniqueId,
                        principalTable: "ParameterMetadata",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_Parameters_PlannerResponses_PlannerResponseUniqueId",
                        column: x => x.PlannerResponseUniqueId,
                        principalTable: "PlannerResponses",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "PlannerAllocations",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParameterUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampaignTemplateUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerAllocations", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_PlannerAllocations_CampaignTemplates_CampaignTemplateUniqueId",
                        column: x => x.CampaignTemplateUniqueId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannerAllocations_ParameterMetadata_ParameterUniqueId",
                        column: x => x.ParameterUniqueId,
                        principalTable: "ParameterMetadata",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_PlannerAllocations_Planners_PlannerId",
                        column: x => x.PlannerId,
                        principalTable: "Planners",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "ParameterValues",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterValues", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ParameterValues_Parameters_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "Parameters",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzerCapabilities_AnalyzerInfoId",
                table: "AnalyzerCapabilities",
                column: "AnalyzerInfoId",
                unique: true,
                filter: "[AnalyzerInfoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Any_DeviceConfigId",
                table: "Any",
                column: "DeviceConfigId",
                unique: true,
                filter: "[DeviceConfigId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTemplates_ExperimentTemplateUniqueId",
                table: "CampaignTemplates",
                column: "ExperimentTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTemplates_Name",
                table: "CampaignTemplates",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTemplates_StartupTemplateUniqueId",
                table: "CampaignTemplates",
                column: "StartupTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandExecutionStatuses_StepExecutionStatusUniqueId",
                table: "CommandExecutionStatuses",
                column: "StepExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandExecutionSummaries_StepExecutionSummaryUniqueId",
                table: "CommandExecutionSummaries",
                column: "StepExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandMetadata_CommandTemplateId",
                table: "CommandMetadata",
                column: "CommandTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplates_StepTemplateUniqueId",
                table: "CommandTemplates",
                column: "StepTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommandResults_CommandExecutionSummaryId",
                table: "DeviceCommandResults",
                column: "CommandExecutionSummaryId",
                unique: true,
                filter: "[CommandExecutionSummaryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_CampaignExecutionSummaryId",
                table: "ExecutionInfos",
                column: "CampaignExecutionSummaryId",
                unique: true,
                filter: "[CampaignExecutionSummaryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_CommandExecutionSummaryId",
                table: "ExecutionInfos",
                column: "CommandExecutionSummaryId",
                unique: true,
                filter: "[CommandExecutionSummaryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_ExperimentResultId",
                table: "ExecutionInfos",
                column: "ExperimentResultId",
                unique: true,
                filter: "[ExperimentResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_StepExecutionSummaryId",
                table: "ExecutionInfos",
                column: "StepExecutionSummaryId",
                unique: true,
                filter: "[StepExecutionSummaryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentExecutionStatuses_CampaignExecutionStatusUniqueId",
                table: "ExperimentExecutionStatuses",
                column: "CampaignExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentExecutionSummaries_CampaignExecutionSummaryUniqueId",
                table: "ExperimentExecutionSummaries",
                column: "CampaignExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentOverviews_ExperimentResultId",
                table: "ExperimentOverviews",
                column: "ExperimentResultId",
                unique: true,
                filter: "[ExperimentResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentOverviews_TemplateUniqueId",
                table: "ExperimentOverviews",
                column: "TemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Limits_ParameterMetadataUniqueId",
                table: "Limits",
                column: "ParameterMetadataUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_CampaignTemplateUniqueId",
                table: "ParameterMetadata",
                column: "CampaignTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_CommandMetadataUniqueId",
                table: "ParameterMetadata",
                column: "CommandMetadataUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_ExtraInfoUniqueId",
                table: "ParameterMetadata",
                column: "ExtraInfoUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_ParameterId",
                table: "ParameterMetadata",
                column: "ParameterId",
                unique: true,
                filter: "[ParameterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_PlannerRequestUniqueId",
                table: "ParameterMetadata",
                column: "PlannerRequestUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_CommandTemplateUniqueId",
                table: "Parameters",
                column: "CommandTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_ExperimentOverviewUniqueId",
                table: "Parameters",
                column: "ExperimentOverviewUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_PlannerResponseUniqueId",
                table: "Parameters",
                column: "PlannerResponseUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_PlanningMetadataUniqueId",
                table: "Parameters",
                column: "PlanningMetadataUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterValues_ParameterId",
                table: "ParameterValues",
                column: "ParameterId",
                unique: true,
                filter: "[ParameterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerAllocations_CampaignTemplateUniqueId",
                table: "PlannerAllocations",
                column: "CampaignTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerAllocations_ParameterUniqueId",
                table: "PlannerAllocations",
                column: "ParameterUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerAllocations_PlannerId",
                table: "PlannerAllocations",
                column: "PlannerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTransactions_ExperimentOverviewUniqueId",
                table: "PlannerTransactions",
                column: "ExperimentOverviewUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTransactions_PlannerInfoUniqueId",
                table: "PlannerTransactions",
                column: "PlannerInfoUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTransactions_RequestUniqueId",
                table: "PlannerTransactions",
                column: "RequestUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTransactions_ResponseUniqueId",
                table: "PlannerTransactions",
                column: "ResponseUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_StepExecutionStatuses_ExperimentExecutionStatusUniqueId",
                table: "StepExecutionStatuses",
                column: "ExperimentExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_StepExecutionSummaries_ExperimentExecutionSummaryUniqueId",
                table: "StepExecutionSummaries",
                column: "ExperimentExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_StepTemplates_ExperimentTemplateUniqueId",
                table: "StepTemplates",
                column: "ExperimentTemplateUniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignTemplates_ExperimentTemplates_ExperimentTemplateUniqueId",
                table: "CampaignTemplates",
                column: "ExperimentTemplateUniqueId",
                principalTable: "ExperimentTemplates",
                principalColumn: "UniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignTemplates_ExperimentTemplates_StartupTemplateUniqueId",
                table: "CampaignTemplates",
                column: "StartupTemplateUniqueId",
                principalTable: "ExperimentTemplates",
                principalColumn: "UniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Limits_ParameterMetadata_ParameterMetadataUniqueId",
                table: "Limits",
                column: "ParameterMetadataUniqueId",
                principalTable: "ParameterMetadata",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParameterMetadata_Parameters_ParameterId",
                table: "ParameterMetadata",
                column: "ParameterId",
                principalTable: "Parameters",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Any_DeviceConfigs_DeviceConfigId",
                table: "Any");

            migrationBuilder.DropForeignKey(
                name: "FK_CampaignTemplates_ExperimentTemplates_ExperimentTemplateUniqueId",
                table: "CampaignTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_CampaignTemplates_ExperimentTemplates_StartupTemplateUniqueId",
                table: "CampaignTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_ExperimentOverviews_ExperimentTemplates_TemplateUniqueId",
                table: "ExperimentOverviews");

            migrationBuilder.DropForeignKey(
                name: "FK_StepTemplates_ExperimentTemplates_ExperimentTemplateUniqueId",
                table: "StepTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandMetadata_CommandTemplates_CommandTemplateId",
                table: "CommandMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_CommandTemplates_CommandTemplateUniqueId",
                table: "Parameters");

            migrationBuilder.DropForeignKey(
                name: "FK_ExperimentExecutionSummaries_CampaignExecutionSummaries_CampaignExecutionSummaryUniqueId",
                table: "ExperimentExecutionSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_ExperimentOverviews_ExperimentExecutionSummaries_ExperimentResultId",
                table: "ExperimentOverviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ParameterMetadata_CampaignTemplates_CampaignTemplateUniqueId",
                table: "ParameterMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_ParameterMetadata_PlanningMetadataUniqueId",
                table: "Parameters");

            migrationBuilder.DropTable(
                name: "Analyses");

            migrationBuilder.DropTable(
                name: "AnalyzerCapabilities");

            migrationBuilder.DropTable(
                name: "Analyzers");

            migrationBuilder.DropTable(
                name: "AnalyzerSettings");

            migrationBuilder.DropTable(
                name: "CampaignTags");

            migrationBuilder.DropTable(
                name: "ChillerStates");

            migrationBuilder.DropTable(
                name: "CommandExecutionStatuses");

            migrationBuilder.DropTable(
                name: "DeviceCommandResults");

            migrationBuilder.DropTable(
                name: "ExecutionInfos");

            migrationBuilder.DropTable(
                name: "Limits");

            migrationBuilder.DropTable(
                name: "MfcStates");

            migrationBuilder.DropTable(
                name: "OutputMetadata");

            migrationBuilder.DropTable(
                name: "ParameterValues");

            migrationBuilder.DropTable(
                name: "PlannerAllocations");

            migrationBuilder.DropTable(
                name: "PlannerTransactions");

            migrationBuilder.DropTable(
                name: "Projects");

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

            migrationBuilder.DropTable(
                name: "AnalyzerInfos");

            migrationBuilder.DropTable(
                name: "StepExecutionStatuses");

            migrationBuilder.DropTable(
                name: "CommandExecutionSummaries");

            migrationBuilder.DropTable(
                name: "Planners");

            migrationBuilder.DropTable(
                name: "ExperimentExecutionStatuses");

            migrationBuilder.DropTable(
                name: "StepExecutionSummaries");

            migrationBuilder.DropTable(
                name: "CampaignExecutionStatuses");

            migrationBuilder.DropTable(
                name: "DeviceConfigs");

            migrationBuilder.DropTable(
                name: "ExperimentTemplates");

            migrationBuilder.DropTable(
                name: "CommandTemplates");

            migrationBuilder.DropTable(
                name: "StepTemplates");

            migrationBuilder.DropTable(
                name: "CampaignExecutionSummaries");

            migrationBuilder.DropTable(
                name: "ExperimentExecutionSummaries");

            migrationBuilder.DropTable(
                name: "CampaignTemplates");

            migrationBuilder.DropTable(
                name: "ParameterMetadata");

            migrationBuilder.DropTable(
                name: "Any");

            migrationBuilder.DropTable(
                name: "CommandMetadata");

            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropTable(
                name: "PlannerRequests");

            migrationBuilder.DropTable(
                name: "ExperimentOverviews");

            migrationBuilder.DropTable(
                name: "PlannerResponses");
        }
    }
}
