using Ares.Core.CoreDevice;
using Ares.Core.Device.Repos;
using Ares.Core.Execution.Interaction;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class StepComposer : IStepComposer
{
  private readonly INotifier _notifier;
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly ISystemSettingsManager _settingsManager;
  private readonly IUserInteractionBroker _userInteractionBroker;

  public StepComposer(IAresDeviceRepo deviceRepo, INotifier notifier, ISystemSettingsManager settingsManager, IUserInteractionBroker userInteractionBroker)
  {
    _deviceRepo = deviceRepo;
    _notifier = notifier;
    _settingsManager = settingsManager;
    _userInteractionBroker = userInteractionBroker;
  }

  public StepExecutor ComposeNodes(IEnumerable<ExecutionNode> executionNodes, bool isParallel)
  {
    var executables = executionNodes
      .OrderBy(t => t.Index)
      .Select<ExecutionNode, IExecutor<CommandExecutionSummary, CommandExecutionStatus>>(node =>
      {
        switch(node.TemplateTypeCase)
        {
          // Standard hardware communications
          case ExecutionNode.TemplateTypeOneofCase.CommandTemplate:
            var commandTemplate = node.CommandTemplate;
            var deviceId = commandTemplate.Metadata?.DeviceId;

            if(deviceId is null)
              throw new InvalidOperationException("Device ID was null when attempting to retrieve the command interpreter");

            var device = _deviceRepo.FirstOrDefault(d => d.UniqueId == deviceId);

            if(device is not null && commandTemplate.Metadata is not null)
            {
              Func<CancellationToken, Task<CommandResult>> internalAction = async (ct)
                => await device.ExecuteCommand(
                  commandTemplate.Metadata.Name,
                  commandTemplate.Parameters.Select(p => new DeviceCommandArgument() { ArgName = p.Metadata.Name, ArgValue = p.GetValue() }).ToList(),
                  ct);

              return new CommandExecutor(internalAction, commandTemplate, _notifier, _settingsManager);
            }

            throw new InvalidOperationException($"Could not resolve device {deviceId} for hardware command.");

          // System control (Sleep, Waiting for Input, etc.)
          case ExecutionNode.TemplateTypeOneofCase.SystemTemplate:
            var systemTemplate = node.SystemTemplate;
            return new SystemExecutor(systemTemplate, _userInteractionBroker, _notifier, _settingsManager);

          case ExecutionNode.TemplateTypeOneofCase.LogicGate:


          default:
            throw new InvalidOperationException($"Unrecognized execution node type: {node.TemplateTypeCase}");
        }
      })
      .ToArray();

    // The Parallel/Sequential executors will now take an array of IExecutor rather than CommandExecutor
    return isParallel
      ? new ParallelStepExecutor(executables)
      : new SequentialStepExecutor(executables, _settingsManager, _notifier);
  }

  public StepExecutor Compose(StepTemplate template, ExperimentExecutionStatus experimentExecutionStatus)
  {
    throw new NotImplementedException();
  }
}
