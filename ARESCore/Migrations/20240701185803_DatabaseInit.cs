using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARESCore.Migrations
{
    public partial class DatabaseInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                name: "CampaignResults",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    CampaignId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignResults", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTemplates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceConfigs",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerResponses", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                name: "SyringePumpStates",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TubeFurnaceStateEntities", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    ExperimentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignExecutionStatusUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                name: "ExperimentResults",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    ExperimentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignResultUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentResults", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentResults_CampaignResults_CampaignResultUniqueId",
                        column: x => x.CampaignResultUniqueId,
                        principalTable: "CampaignResults",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    StepId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentExecutionStatusUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                name: "CompletedExperiments",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentResultId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedExperiments", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CompletedExperiments_ExperimentResults_ExperimentResultId",
                        column: x => x.ExperimentResultId,
                        principalTable: "ExperimentResults",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "CommandExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    CommandId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepExecutionStatusUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true)
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
                name: "Analyses",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    CompletedExperimentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Result = table.Column<float>(type: "real", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Analyses_CompletedExperiments_CompletedExperimentId",
                        column: x => x.CompletedExperimentId,
                        principalTable: "CompletedExperiments",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "ExperimentTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    OutputCommandId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    CampaignTemplateUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CompletedExperimentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentTemplates", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentTemplates_CampaignTemplates_CampaignTemplateUniqueId",
                        column: x => x.CampaignTemplateUniqueId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperimentTemplates_CompletedExperiments_CompletedExperimentId",
                        column: x => x.CompletedExperimentId,
                        principalTable: "CompletedExperiments",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "Analyzers",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalysisId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentTemplateId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyzers", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Analyzers_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Analyzers_ExperimentTemplates_ExperimentTemplateId",
                        column: x => x.ExperimentTemplateId,
                        principalTable: "ExperimentTemplates",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "StepTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsParallel = table.Column<bool>(type: "bit", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentTemplateUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                name: "CommandTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepTemplateUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                name: "StepResults",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    StepId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentResultUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepResults", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_StepResults_ExperimentResults_ExperimentResultUniqueId",
                        column: x => x.ExperimentResultUniqueId,
                        principalTable: "ExperimentResults",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StepResults_StepTemplates_StepId",
                        column: x => x.StepId,
                        principalTable: "StepTemplates",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "CommandMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommandTemplateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                name: "CommandResults",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    CommandId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepResultUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandResults", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandResults_CommandTemplates_CommandId",
                        column: x => x.CommandId,
                        principalTable: "CommandTemplates",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_CommandResults_StepResults_StepResultUniqueId",
                        column: x => x.StepResultUniqueId,
                        principalTable: "StepResults",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutputMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "DeviceCommandResults",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommandResultId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommandResults", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_DeviceCommandResults_CommandResults_CommandResultId",
                        column: x => x.CommandResultId,
                        principalTable: "CommandResults",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "ExecutionInfos",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    TimeStarted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeFinished = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CampaignResultId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CommandResultId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ExperimentResultId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    StepResultId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionInfos", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_CampaignResults_CampaignResultId",
                        column: x => x.CampaignResultId,
                        principalTable: "CampaignResults",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_CommandResults_CommandResultId",
                        column: x => x.CommandResultId,
                        principalTable: "CommandResults",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_ExperimentResults_ExperimentResultId",
                        column: x => x.ExperimentResultId,
                        principalTable: "ExperimentResults",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_StepResults_StepResultId",
                        column: x => x.StepResultId,
                        principalTable: "StepResults",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "Any",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    TypeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CompletedExperimentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    DeviceCommandResultId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DeviceConfigId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Any", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Any_CompletedExperiments_CompletedExperimentId",
                        column: x => x.CompletedExperimentId,
                        principalTable: "CompletedExperiments",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_Any_DeviceCommandResults_DeviceCommandResultId",
                        column: x => x.DeviceCommandResultId,
                        principalTable: "DeviceCommandResults",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_Any_DeviceConfigs_DeviceConfigId",
                        column: x => x.DeviceConfigId,
                        principalTable: "DeviceConfigs",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "Limits",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Minimum = table.Column<float>(type: "real", nullable: false),
                    Maximum = table.Column<float>(type: "real", nullable: false),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ParameterMetadataUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Limits", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "ParameterMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CampaignTemplateUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CommandMetadataUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ParameterId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlannerRequestUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterMetadata", x => x.UniqueId);
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Planned = table.Column<bool>(type: "bit", nullable: false),
                    PlanningMetadataUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Index = table.Column<long>(type: "bigint", nullable: false),
                    CommandTemplateUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    PlannerResponseUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true)
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
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    ParameterUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CampaignTemplateUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "ParameterValues",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Value = table.Column<float>(type: "real", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    ParameterId = table.Column<string>(type: "nvarchar(450)", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Planners",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    PlannerAllocationId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planners", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Planners_PlannerAllocations_PlannerAllocationId",
                        column: x => x.PlannerAllocationId,
                        principalTable: "PlannerAllocations",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "PlannerTransactions",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValueSql: "NEWID()"),
                    RequestUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ResponseUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlannerInfoUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CompletedExperimentUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerTransactions", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_PlannerTransactions_CompletedExperiments_CompletedExperimentUniqueId",
                        column: x => x.CompletedExperimentUniqueId,
                        principalTable: "CompletedExperiments",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_CompletedExperimentId",
                table: "Analyses",
                column: "CompletedExperimentId",
                unique: true,
                filter: "[CompletedExperimentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Analyzers_AnalysisId",
                table: "Analyzers",
                column: "AnalysisId",
                unique: true,
                filter: "[AnalysisId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Analyzers_ExperimentTemplateId",
                table: "Analyzers",
                column: "ExperimentTemplateId",
                unique: true,
                filter: "[ExperimentTemplateId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Any_CompletedExperimentId",
                table: "Any",
                column: "CompletedExperimentId",
                unique: true,
                filter: "[CompletedExperimentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Any_DeviceCommandResultId",
                table: "Any",
                column: "DeviceCommandResultId",
                unique: true,
                filter: "[DeviceCommandResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Any_DeviceConfigId",
                table: "Any",
                column: "DeviceConfigId",
                unique: true,
                filter: "[DeviceConfigId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTemplates_Name",
                table: "CampaignTemplates",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommandExecutionStatuses_StepExecutionStatusUniqueId",
                table: "CommandExecutionStatuses",
                column: "StepExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandMetadata_CommandTemplateId",
                table: "CommandMetadata",
                column: "CommandTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandResults_CommandId",
                table: "CommandResults",
                column: "CommandId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandResults_StepResultUniqueId",
                table: "CommandResults",
                column: "StepResultUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplates_StepTemplateUniqueId",
                table: "CommandTemplates",
                column: "StepTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedExperiments_ExperimentResultId",
                table: "CompletedExperiments",
                column: "ExperimentResultId",
                unique: true,
                filter: "[ExperimentResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommandResults_CommandResultId",
                table: "DeviceCommandResults",
                column: "CommandResultId",
                unique: true,
                filter: "[CommandResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_CampaignResultId",
                table: "ExecutionInfos",
                column: "CampaignResultId",
                unique: true,
                filter: "[CampaignResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_CommandResultId",
                table: "ExecutionInfos",
                column: "CommandResultId",
                unique: true,
                filter: "[CommandResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_ExperimentResultId",
                table: "ExecutionInfos",
                column: "ExperimentResultId",
                unique: true,
                filter: "[ExperimentResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_StepResultId",
                table: "ExecutionInfos",
                column: "StepResultId",
                unique: true,
                filter: "[StepResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentExecutionStatuses_CampaignExecutionStatusUniqueId",
                table: "ExperimentExecutionStatuses",
                column: "CampaignExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentResults_CampaignResultUniqueId",
                table: "ExperimentResults",
                column: "CampaignResultUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentTemplates_CampaignTemplateUniqueId",
                table: "ExperimentTemplates",
                column: "CampaignTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentTemplates_CompletedExperimentId",
                table: "ExperimentTemplates",
                column: "CompletedExperimentId",
                unique: true,
                filter: "[CompletedExperimentId] IS NOT NULL");

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
                name: "IX_Planners_PlannerAllocationId",
                table: "Planners",
                column: "PlannerAllocationId",
                unique: true,
                filter: "[PlannerAllocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerTransactions_CompletedExperimentUniqueId",
                table: "PlannerTransactions",
                column: "CompletedExperimentUniqueId");

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
                name: "IX_StepResults_ExperimentResultUniqueId",
                table: "StepResults",
                column: "ExperimentResultUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_StepResults_StepId",
                table: "StepResults",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_StepTemplates_ExperimentTemplateUniqueId",
                table: "StepTemplates",
                column: "ExperimentTemplateUniqueId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExperimentTemplates_CompletedExperiments_CompletedExperimentId",
                table: "ExperimentTemplates");

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
                name: "FK_ParameterMetadata_CampaignTemplates_CampaignTemplateUniqueId",
                table: "ParameterMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_ParameterMetadata_PlanningMetadataUniqueId",
                table: "Parameters");

            migrationBuilder.DropTable(
                name: "Analyzers");

            migrationBuilder.DropTable(
                name: "Any");

            migrationBuilder.DropTable(
                name: "CommandExecutionStatuses");

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
                name: "PlannerTransactions");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "SyringePumpStates");

            migrationBuilder.DropTable(
                name: "Tc0304States");

            migrationBuilder.DropTable(
                name: "TicStepperControllerStates");

            migrationBuilder.DropTable(
                name: "TubeFurnaceStateEntities");

            migrationBuilder.DropTable(
                name: "Analyses");

            migrationBuilder.DropTable(
                name: "DeviceCommandResults");

            migrationBuilder.DropTable(
                name: "DeviceConfigs");

            migrationBuilder.DropTable(
                name: "StepExecutionStatuses");

            migrationBuilder.DropTable(
                name: "Planners");

            migrationBuilder.DropTable(
                name: "CommandResults");

            migrationBuilder.DropTable(
                name: "ExperimentExecutionStatuses");

            migrationBuilder.DropTable(
                name: "PlannerAllocations");

            migrationBuilder.DropTable(
                name: "StepResults");

            migrationBuilder.DropTable(
                name: "CampaignExecutionStatuses");

            migrationBuilder.DropTable(
                name: "CompletedExperiments");

            migrationBuilder.DropTable(
                name: "ExperimentResults");

            migrationBuilder.DropTable(
                name: "CampaignResults");

            migrationBuilder.DropTable(
                name: "ExperimentTemplates");

            migrationBuilder.DropTable(
                name: "CommandTemplates");

            migrationBuilder.DropTable(
                name: "StepTemplates");

            migrationBuilder.DropTable(
                name: "CampaignTemplates");

            migrationBuilder.DropTable(
                name: "ParameterMetadata");

            migrationBuilder.DropTable(
                name: "CommandMetadata");

            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropTable(
                name: "PlannerRequests");

            migrationBuilder.DropTable(
                name: "PlannerResponses");
        }
    }
}
