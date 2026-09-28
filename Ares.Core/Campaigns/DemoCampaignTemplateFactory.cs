using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns;

/// <summary>
/// Builds the static campaign template mapped directly from template configuration.
/// </summary>
internal static class DemoCampaignTemplateFactory
{
  public static CampaignTemplate Create()
  {
    var plannableParameters = CreatePlannableParameters();

    var template = new CampaignTemplate
    {
      UniqueId = "63b6560d-b27d-47c0-9ff1-4468b6925a49",
      Name = "Template Inspiration",
      StartupTemplate = CreateStartupTemplate(),
      ExperimentTemplate = CreateExperimentTemplate(),
      CloseoutTemplate = CreateCloseoutTemplate()
    };

    template.PlannableParameters.AddRange(plannableParameters);
    template.PlannerAllocations.AddRange(CreatePlannerAllocations(plannableParameters));

    return template;
  }

  #region Startup Template

  private static ExperimentTemplate CreateStartupTemplate()
  {
    var startupStep = new StepTemplate
    {
      UniqueId = "ebae1c1a-7d76-40c9-b144-2c80ba4f8a99",
      Name = "Startup Script Demo",
      IsParallel = false,
      Index = 0
    };

    startupStep.CommandTemplates.AddRange(CreateStartupCommandTemplates());

    var startupTemplate = new ExperimentTemplate
    {
      UniqueId = "52e51fc3-fd1c-4198-866f-a8aca2a1e64e",
      Name = "Startup Template",
      Resolved = false
    };

    startupTemplate.StepTemplates.Add(startupStep);
    return startupTemplate;
  }

  private static IEnumerable<CommandTemplate> CreateStartupCommandTemplates()
  {
    return new List<CommandTemplate>
        {
            // Catalyst MFC - NewSetpoint (0)
            new CommandTemplate
            {
                UniqueId = "c295da9a-c3b1-41cc-acd9-c1711c9d5467",
                Index = 0,
                DeviceCommand = new DeviceCommand
                {
                    Metadata = new CommandMetadata
                    {
                        UniqueId = "f2cba765-3a6b-42eb-92bb-9dd3363e3175",
                        Name = "NewSetpoint",
                        Description = "Sets a new target mass flow",
                        DeviceId = "7f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8e",
                        DeviceType = "Catalyst MFC",
                        ParameterMetadatas = { CreateSetpointMetadata("3d36d956-c70f-4331-9c35-1820d1f69a75", AresDataType.Number) }
                    }
                },
                ArgumentBindings = { CreateLiteralParameter("7a63a7cd-6507-44bf-99ba-a3e475db738d", "4c5ad35a-6e03-4bcc-90b9-a412d4e9a91f", "Setpoint", 0, AresDataType.Number) }
            },
            // Tube Furnace - SetSetpoint (20)
            new CommandTemplate
            {
                UniqueId = "fe893e59-c300-448a-b6c1-3a114b93da3f",
                Index = 1,
                DeviceCommand = new DeviceCommand
                {
                    Metadata = new CommandMetadata
                    {
                        UniqueId = "5d447d52-dc9d-467e-b12f-fb1dd74fdfb4",
                        Name = "SetSetpoint",
                        Description = "Sets a new target setpoint for the furnace.",
                        DeviceId = "8d9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8f",
                        DeviceType = "Demo Tube Furance",
                        ParameterMetadatas = { CreateSetpointMetadata("20f82057-0b69-4613-a47e-b638bf672dd2", AresDataType.Number) }
                    }
                },
                ArgumentBindings = { CreateLiteralParameter("8bbf24db-9768-46e0-bc1f-72e18254c62e", "e952b10c-66f0-42d8-a716-9755ace45e3e", "Setpoint", 20, AresDataType.Number) }
            },
            // Syringe Pump - Stop (PumpIndex 0)
            new CommandTemplate
            {
                UniqueId = "e25a4a11-763b-48f8-8eca-559902ddc367",
                Index = 2,
                DeviceCommand = new DeviceCommand
                {
                    Metadata = new CommandMetadata
                    {
                        UniqueId = "18deaaa0-38b1-46e6-8ff6-c684abe4416c",
                        Name = "Stop",
                        Description = "Stops the pump.",
                        DeviceId = "8f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8f",
                        DeviceType = "Demo Syringe Pump",
                        ParameterMetadatas = { CreateParameterMetadata("3bc16d24-6ee8-45cd-a8c6-1e13546ac91b", "PumpIndex", AresDataType.Number) }
                    }
                },
                ArgumentBindings = { CreateLiteralParameter("d7dd8a82-7672-4e43-9a26-023c7931a9ff", "6145ba27-3bd2-4e9b-be80-e102ca5584db", "PumpIndex", 0, AresDataType.Number) }
            },
            // Nitrogen MFC - NewSetpoint (0)
            new CommandTemplate
            {
                UniqueId = "d0d4ffe2-b43d-4f56-8537-de608f217186",
                Index = 3,
                DeviceCommand = new DeviceCommand
                {
                    Metadata = new CommandMetadata
                    {
                        UniqueId = "aa99255f-4f46-46c8-ba27-aaa71d766898",
                        Name = "NewSetpoint",
                        Description = "Sets a new target mass flow",
                        DeviceId = "7f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e6e",
                        DeviceType = "Nitrogen MFC",
                        ParameterMetadatas = { CreateSetpointMetadata("17ec161d-efcb-497d-a000-9ab4ec7c5fa9", AresDataType.Number) }
                    }
                },
                ArgumentBindings = { CreateLiteralParameter("1bb116cf-f9c0-4caf-8234-5e5c5731b497", "e1b6c4a3-bbca-44b8-8094-2d9d8819b227", "Setpoint", 0, AresDataType.Number) }
            }
        };
  }

  #endregion

  #region Experiment Template

  private static ExperimentTemplate CreateExperimentTemplate()
  {
    var experimentStep = new StepTemplate
    {
      UniqueId = "d148d078-86b7-4b24-821e-82b76de8e15b",
      Name = "Experiment Step Demo",
      IsParallel = false,
      Index = 0
    };

    experimentStep.CommandTemplates.AddRange(CreateExperimentCommandTemplates());

    var maps = new Dictionary<string, string>
    {
      { "Temperature", "Temperature-Output" },
      { "Flow Rate", "Setpoint-Output" }
    };

    var experimentTemplate = new ExperimentTemplate
    {
      UniqueId = "1f60837b-bd42-4458-8e87-a5ef31f8d0bb",
      Name = "New Experiment",
      AnalyzerId = "9e5a8f3b-5c7d-4a1b-9f0a-1a2b3c4d5e6f",
      Resolved = false
    };

    experimentTemplate.AnalyzerMaps.Add(maps);
    experimentTemplate.PlanObjectives.Add("Yield");

    experimentTemplate.StepTemplates.Add(experimentStep);
    return experimentTemplate;
  }

  private static IEnumerable<CommandTemplate> CreateExperimentCommandTemplates()
  {
    return new List<CommandTemplate>
    {
      // 0: Catalyst MFC - NewSetpoint (Planned: Flow Rate Param)
      new CommandTemplate
      {
        UniqueId = "b83791fb-4e4a-495c-8e6a-e875bd413d72",
        Index = 0,
        DeviceCommand = new DeviceCommand
        {
          Metadata = new CommandMetadata
          {
            UniqueId = "8bcc9b98-ede1-42d4-a8d6-219904d2f9d5",
            Name = "NewSetpoint",
            Description = "Sets a new target mass flow",
            DeviceId = "7f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8e",
            DeviceType = "Catalyst MFC",
            ParameterMetadatas = 
            { 
              CreateSetpointMetadata("a37ea282-f66b-471c-a3cf-7d0ae90fcb05", AresDataType.Number) 
            }
          }
        },
        ArgumentBindings =
        {
          CreatePlannedParameter(
            "2cd91d65-55cc-4b0f-b313-7a1963040892",
            "c975ed82-9028-47a1-9b9d-b7e76075d5df",
            "Setpoint",
            "dc65f42f-5640-4ffe-8ce7-1654e4148949",
            "Flow Rate Param",
            minValue: 10,
            maxValue: 200
          )
        }
      },
        // 1: Tube Furnace - SetSetpoint (Planned: Temperature Param)
        new CommandTemplate
        {
          UniqueId = "604b4472-1530-45f4-bfa6-f33d64fe54de",
          Index = 1,
          DeviceCommand = new DeviceCommand
          {
            Metadata = new CommandMetadata
            {
              UniqueId = "46b44dce-5231-450f-bdf4-63c199aa5f02",
              Name = "SetSetpoint",
              Description = "Sets a new target setpoint for the furnace.",
              DeviceId = "8d9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8f",
              DeviceType = "Demo Tube Furance",
              ParameterMetadatas = 
              { 
                CreateSetpointMetadata("f44f05dd-c42a-446d-a066-93bc123ebe7c", AresDataType.Number) 
              }
            }
          },
          ArgumentBindings =
          {
            CreatePlannedParameter(
                "cac800c4-d6cd-4da3-af21-3ecc1f8236bb",
                "5e5f2c49-e419-4db1-9e3b-bbdbecf1ca19",
                "Setpoint",
                "5c4a2b3f-55a3-4ed3-88d1-f1c8ec6008a8",
                "Temperature Param",
                minValue: 10,
                maxValue: 200)
          }
        },
        // 2: Nitrogen MFC - NewSetpoint (75)
        new CommandTemplate
        {
            UniqueId = "16dff689-7255-43be-b6de-7ce8b9952cd8",
            Index = 2,
            DeviceCommand = new DeviceCommand
            {
              Metadata = new CommandMetadata
              {
                UniqueId = "9aadb7d9-154b-420e-8527-a316252564eb",
                Name = "NewSetpoint",
                Description = "Sets a new target mass flow",
                DeviceId = "7f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e6e",
                DeviceType = "Nitrogen MFC",
                ParameterMetadatas = 
                { 
                  CreateSetpointMetadata("d603cce5-7f8e-4069-8e0a-43eb45bf4c98", AresDataType.Number) 
                }
              }
            },
            ArgumentBindings = { CreateLiteralParameter("0e9dbcd9-8ee0-45ab-ad67-b53e08a8c829", "a98a2132-0a5f-4708-a863-cec1c4248755", "Setpoint", 75, AresDataType.Number) }
        },
        // 3: Syringe Pump - Start (PumpIndex 0, Mode 0)
        new CommandTemplate
        {
          UniqueId = "71c433a8-773e-4076-a957-5b05527ce748",
          Index = 3,
          DeviceCommand = new DeviceCommand
          {
            Metadata = new CommandMetadata
            {
              UniqueId = "a7ac4552-8231-4a7e-a2aa-d7e9f059e3d8",
              Name = "Start",
              Description = "Starts the pump.",
              DeviceId = "8f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8f",
              DeviceType = "Demo Syringe Pump",
              ParameterMetadatas =
              {
                CreateParameterMetadata("e190abbc-e087-4126-851b-699a38a877d8", "PumpIndex", AresDataType.Number),
                CreateParameterMetadata("7f5b2095-381e-4314-923e-13a4f35badc7", "Mode", AresDataType.Number, isOptional: true)
              }
            }
          },
          ArgumentBindings =
          {
            CreateLiteralParameter("02656187-22ec-46c4-838a-f8ea6f517fed", "412dd184-b607-4af7-bf98-dc88c429aab0", "PumpIndex", 0, AresDataType.Number),
            CreateLiteralParameter("39349b63-f6fd-469e-9c49-94a58b2a2754", "93e33a34-c7e4-4995-94c7-22be992bfe5c", "Mode", 0, AresDataType.Number, isOptional: true)
          }
        },
        // 4: System Command - GetTime (growth-start-time)
        new CommandTemplate
        {
          UniqueId = "3b7d06f1-008d-4acc-9729-c9563f2e9d3c",
          Index = 4,
          OutputVarName = "growth-start-time",
          SystemCommand = new SystemCommand { Operation = (SystemOperation)6 }
        },
        // 5: System Command - Delay (10s)
        new CommandTemplate
        {
          UniqueId = "0c453598-1640-4e22-be7b-e4568c8a5a8b",
          Index = 5,
          SystemCommand = new SystemCommand { Operation = (SystemOperation)2 },
          ArgumentBindings = 
          { 
            CreateLiteralParameter("ccca8b27-201f-4ca2-b087-c433a12e1430", "54f1d894-cb53-4b0b-9dec-c324583670db", "Duration", 10, AresDataType.Number, notPlannable: true) 
          }
        },
        // 6: System Command - GetTime (growth-stop-time)
        new CommandTemplate
        {
          UniqueId = "2ba49edc-0f1b-45ee-99f9-78e09a11c5a1",
          Index = 6,
          OutputVarName = "growth-stop-time",
          SystemCommand = new SystemCommand { Operation = (SystemOperation)6 }
        },

        // 7: Catalyst MFC - GetSetpoint (Setpoint-Output)
        new CommandTemplate
        {
          UniqueId = "89c59d18-32ed-4a4b-a965-4b6e7b69717d",
          Index = 7,
          OutputVarName = "Setpoint-Output",
          DeviceCommand = new DeviceCommand
          {
            Metadata = new CommandMetadata
            {
              UniqueId = "988d6172-665a-4ac5-b2b0-ea81c2c458f9",
              Name = "GetSetpoint",
              Description = "Gets the current setpoint of the MFC",
              DeviceId = "7f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8e",
              DeviceType = "Catalyst MFC",
              OutputMetadata = new OutputMetadata
              {
                  UniqueId = "d96382e8-32eb-4414-a3e5-695b7ff5571e",
                  Description = "",
                  Index = 0,
                  DataSchema = new AresValueSchema { Type = AresDataType.Number, Optional = false, Description = "Current setpoint" }
              }
            }
          }
        },

        // 8: Tube Furnace - GetCurrentTemperature (Temperature-Output)
        new CommandTemplate
        {
          UniqueId = "d16fced1-142e-441b-916a-f5e0373c5e6a",
          Index = 8,
          OutputVarName = "Temperature-Output",
          DeviceCommand = new DeviceCommand
          {
            Metadata = new CommandMetadata
            {
              UniqueId = "52723bb2-3a42-430e-9983-846557b374fc",
              Name = "GetCurrentTemperature",
              Description = "Gets the current temperature from the furnace.",
              DeviceId = "8d9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8f",
              DeviceType = "Demo Tube Furance",
              OutputMetadata = new OutputMetadata
              {
                  UniqueId = "52b9c24e-c6c7-4b03-95e7-1b67777f21a7",
                  Description = "",
                  Index = 0,
                  DataSchema = new AresValueSchema { Type = (AresDataType)15, Optional = false, Description = "Current Temperature of the Furnace" }
              }
            }
          }
        },
        // 9: Syringe Pump - Stop (PumpIndex 0)
        new CommandTemplate
        {
          UniqueId = "ebe51ad3-e2e6-41eb-978c-53c451153dfe",
          Index = 9,
          DeviceCommand = new DeviceCommand
          {
            Metadata = new CommandMetadata
            {
              UniqueId = "cfe73d1e-7495-450c-8cc7-de9d5c0141db",
              Name = "Stop",
              Description = "Stops the pump.",
              DeviceId = "8f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8f",
              DeviceType = "Demo Syringe Pump",
              ParameterMetadatas = 
              { 
                CreateParameterMetadata("0af4a5ac-b171-4be2-a58d-919f25cbe12e", "PumpIndex", AresDataType.Number) 
              }
            }
          },
          ArgumentBindings = 
          { 
            CreateLiteralParameter("3bd16432-95f7-4b8e-bd19-f5e9d45a3653", "e3a8d8d3-0e86-449a-9df4-887196d02874", "PumpIndex", 0, AresDataType.Number) 
          }
        }
    };
  }

  #endregion

  #region Closeout Template

  private static ExperimentTemplate CreateCloseoutTemplate()
  {
    var closeoutStep = new StepTemplate
    {
      UniqueId = "5c0a4fd5-05a6-4a3f-ac2e-9a8f05e3b398",
      Name = "Closeout Script Demo",
      IsParallel = false,
      Index = 0
    };

    closeoutStep.CommandTemplates.AddRange(CreateCloseoutCommandTemplates());

    var closeoutTemplate = new ExperimentTemplate
    {
      UniqueId = "c98e3707-2f51-4c1a-ab4d-a56f62319fea",
      Name = "Closeout Template",
      Resolved = false
    };

    closeoutTemplate.StepTemplates.Add(closeoutStep);
    return closeoutTemplate;
  }

  private static IEnumerable<CommandTemplate> CreateCloseoutCommandTemplates()
  {
    return new List<CommandTemplate>
    {
      // 0: Catalyst MFC - NewSetpoint (0)
      new CommandTemplate
      {
        UniqueId = "b517a4f6-a809-4e54-bc1b-00a12e93ec49",
        Index = 0,
        DeviceCommand = new DeviceCommand
        {
          Metadata = new CommandMetadata
          {
            UniqueId = "f0a03bd4-6b4d-4546-9d7d-a4d1cfb6df87",
            Name = "NewSetpoint",
            Description = "Sets a new target mass flow",
            DeviceId = "7f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8e",
            DeviceType = "Catalyst MFC",
            ParameterMetadatas = 
            { 
              CreateSetpointMetadata("3b78f6b9-ffcb-44c1-b277-be0e76a9ca90", AresDataType.Number) 
            }
          }
        },
        ArgumentBindings = 
        { 
          CreateLiteralParameter("ec1d3624-dc48-4d28-b462-212efcdee4a5", "57d7643f-249e-42f5-b3b6-c4a7746542ef", "Setpoint", 0, AresDataType.Number) 
        }
      },
      // 1: Tube Furnace - SetSetpoint (20)
      new CommandTemplate
      {
        UniqueId = "f6a9cae0-22cd-4d3b-a168-5c4d5836c1c0",
        Index = 1,
        DeviceCommand = new DeviceCommand
        {
          Metadata = new CommandMetadata
          {
            UniqueId = "d53e4242-40be-4977-934a-576ff7b3f6c5",
            Name = "SetSetpoint",
            Description = "Sets a new target setpoint for the furnace.",
            DeviceId = "8d9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e8f",
            DeviceType = "Demo Tube Furance",
            ParameterMetadatas = 
            { 
              CreateSetpointMetadata("cc36e97a-c2a7-4b29-a642-eee501f08a98", AresDataType.Number) 
            }
          }
        },
        ArgumentBindings = { CreateLiteralParameter("9aed0fcc-b293-420b-a372-4bc44c7c483c", "34669ee2-bca9-4e7f-990a-e2b53f312b54", "Setpoint", 20, AresDataType.Number) }
      },
      // 2: Nitrogen MFC - NewSetpoint (0)
      new CommandTemplate
      {
        UniqueId = "46006f37-9545-4c14-a01e-a31893a1fadc",
        Index = 2,
        DeviceCommand = new DeviceCommand
        {
          Metadata = new CommandMetadata
          {
            UniqueId = "dc83fd8a-9cb0-40b3-8803-78934d530d47",
            Name = "NewSetpoint",
            Description = "Sets a new target mass flow",
            DeviceId = "7f9c8b28-4c2e-4d6f-9f1a-3a2b1c0d9e6e",
            DeviceType = "Nitrogen MFC",
            ParameterMetadatas = 
            { 
              CreateSetpointMetadata("16a98dda-cb4a-4ba2-838f-e3457b3cdf3a", AresDataType.Number) 
            }
          }
        },
        ArgumentBindings = 
        { 
          CreateLiteralParameter("8561fb7e-a70e-49d9-90a5-2bbb0ecd18c8", "cacb5f34-9411-4037-8d4b-28397167bbbe", "Setpoint", 0, AresDataType.Number) 
        }
      }
    };
  }

  #endregion

  #region Plannable Parameters & Allocations

  private static List<ParameterMetadata> CreatePlannableParameters()
  {
    var tempParamMetadata = new ParameterMetadata
    {
      UniqueId = "5c4a2b3f-55a3-4ed3-88d1-f1c8ec6008a8",
      Name = "Temperature Param",
      Index = 0,
      Unit = "",
      PlannerName = "Hill Climbing Planner",
      PlannerDescription = "A planner that uses a simple hill-climbing step based on previous parameter history.",
      Schema = new AresValueSchema { Type = (AresDataType)15, Optional = false, MinNumberValue = 10, MaxNumberValue = 200 }
    };

    tempParamMetadata.Constraints.Add(new Limits { Maximum = 200, Minimum = 10 });

    var flowRateParamMetadata = new ParameterMetadata
    {
      UniqueId = "dc65f42f-5640-4ffe-8ce7-1654e4148949",
      Name = "Flow Rate Param",
      Index = 0,
      Unit = "",
      PlannerName = "Hill Climbing Planner",
      PlannerDescription = "A planner that uses a simple hill-climbing step based on previous parameter history.",
      Schema = new AresValueSchema { Type = (AresDataType)15, Optional = false, MinNumberValue = 10, MaxNumberValue = 200 }
    };

    flowRateParamMetadata.Constraints.Add(new Limits { Maximum = 200, Minimum = 10 });

    return [tempParamMetadata, flowRateParamMetadata];
  }

  private static IEnumerable<PlannerAllocation> CreatePlannerAllocations(List<ParameterMetadata> parameters)
  {
    var planner = new PlannerServiceInfo
    {
      UniqueId = "4b14d5e9-1c9f-4f01-8b2b-4d4d1e2e3e4e",
      Name = "Demo Remote Planner",
      Type = "Demo Planner Service",
      Version = "1.0.0",
      Address = "http://localhost:5036/",
      Description = "A demo of the ARES remote planenr service capabilities"
    };

    return new List<PlannerAllocation>
    {
      new PlannerAllocation
      {
          UniqueId = "474caa81-616b-49f6-b20c-b60ca0c31d6d",
          Planner = planner,
          Parameter = parameters.First(p => p.Name == "Temperature Param")
      },
      new PlannerAllocation
      {
          UniqueId = "f49c3a09-ccc5-40ab-a716-3c17111a5576",
          Planner = planner,
          Parameter = parameters.First(p => p.Name == "Flow Rate Param")
      }
    };
  }

  #endregion

  #region Factory Helper Methods

  private static ParameterMetadata CreateSetpointMetadata(string uniqueId, AresDataType dataType) 
    => CreateParameterMetadata(uniqueId, "Setpoint", dataType);

  private static ParameterMetadata CreateParameterMetadata(string uniqueId, string name, AresDataType dataType, bool isOptional = false) =>
    new ParameterMetadata
    {
      UniqueId = uniqueId,
      Name = name,
      Index = 0,
      Unit = "",
      NotPlannable = false,
      UseDefault = false,
      PlannerName = "",
      PlannerDescription = "",
      Schema = new AresValueSchema { Type = dataType, Optional = isOptional }
    };

  private static Parameter CreateLiteralParameter(string uniqueId, string metadataId, string name, double value, AresDataType dataType, bool isOptional = false, bool notPlannable = false) =>
    new Parameter
    {
      UniqueId = uniqueId,
      Index = 0,
      Metadata = new ParameterMetadata
      {
        UniqueId = metadataId,
        Name = name,
        Index = 0,
        Unit = "",
        NotPlannable = notPlannable,
        UseDefault = false,
        PlannerName = "",
        PlannerDescription = "",
        Schema = new AresValueSchema { Type = dataType, Optional = isOptional }
      },
      LiteralSource = new LiteralParameterSource
      {
        Value = AresValueHelper.CreateNumber(value)
      }
    };

  private static Parameter CreatePlannedParameter(string uniqueId, string metadataId, string name, string plannedParamId, string plannedParamName, double? maxValue = null, double? minValue = null)
  {
    var parameter = new Parameter
    {
      UniqueId = uniqueId,
      Index = 0,
      Metadata = new ParameterMetadata
      {
        UniqueId = metadataId,
        Name = name,
        Index = 0,
        Unit = "",
        NotPlannable = false,
        UseDefault = false,
        PlannerName = "",
        PlannerDescription = "",
        Schema = new AresValueSchema
        {
          Type = AresDataType.Number,
          Optional = false
        }
      },
      PlannedSource = new PlannedParameterSource
      {
        PlanningMetadata = new ParameterMetadata
        {
          UniqueId = plannedParamId,
          Name = plannedParamName,
          Index = 0,
          Unit = "",
          NotPlannable = false,
          UseDefault = false,
          PlannerName = "Hill Climbing Planner",
          PlannerDescription = "A planner that uses a simple hill-climbing step based on previous parameter history.",
          Schema = new AresValueSchema { Type = (AresDataType)15, Optional = false, MaxNumberValue = 200, MinNumberValue = 10 }
        }
      }
    };

    if(maxValue is not null && minValue is not null)
    {
      parameter.Metadata.Schema.MaxNumberValue = (double)maxValue;
      parameter.Metadata.Schema.MinNumberValue = (double)minValue;
      parameter.Metadata.Constraints.Add(new Limits() { Maximum = (double)maxValue, Minimum = (double)minValue });
    }

    return parameter;
  }
  #endregion
}