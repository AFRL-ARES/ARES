using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace Ares.Core.Campaigns;

/// <summary>
/// Builds the static demo campaign template used in demo mode. This template
/// is wired to the demo analyzer, demo planner (optionally), and demo remote
/// device so users can explore the full execution flow without touching
/// persisted templates.
/// </summary>
internal static class DemoCampaignTemplateFactory
{
  public static CampaignTemplate Create()
  {
    // Device command that calls the demo remote device's ECHO_NUMBER command
    var deviceCommand = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Index = 0,
      DeviceCommand = new DeviceCommand
      {
        Metadata = new CommandMetadata
        {
          UniqueId = Guid.NewGuid().ToString(),
          Name = "ECHO_NUMBER", // matches DemoRemoteDevice command name
          DeviceId = DemoIds.RemoteDeviceId,
          DeviceType = DemoIds.RemoteDeviceName,
          OutputMetadata = new OutputMetadata { UniqueId = Guid.NewGuid().ToString() }
        }
      }
    };

    // Simple numeric input parameter for the device command
    var parameter = new Parameter
    {
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new ParameterMetadata
      {
        UniqueId = Guid.NewGuid().ToString(),
        Name = "Input Number"
      },
      LiteralSource = new LiteralParameterSource
      {
        Value = AresValueHelper.CreateNumber(42)
      }
    };

    deviceCommand.ArgumentBindings.Add(parameter);

    // Single step containing the device command
    var step = new StepTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Demo Step"
    };

    step.CommandTemplates.Add(deviceCommand);

    // Main experiment template using the demo analyzer
    var experiment = new ExperimentTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Demo Experiment",
      AnalyzerId = DemoIds.AnalyzerId
    };

    experiment.StepTemplates.Add(step);

    // Build the campaign template itself; startup/closeout are omitted for simplicity
    var template = new CampaignTemplate
    {
      UniqueId = DemoIds.CampaignId,
      Name = DemoIds.CampaignName,
      ExperimentTemplate = experiment
    };

    return template;
  }
}
