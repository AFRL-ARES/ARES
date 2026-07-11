using Ares.Core.Device.Repos;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class StepComposer : ICommandComposer<StepTemplate, StepExecutor>
{
  private readonly INotifier _notifier;
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly ISystemSettingsManager _settingsManager;
  private readonly CustomCommandExecutor? _customCommandExecutor;
  private readonly ICommandDisplayNameResolver _commandDisplayNameResolver;
  public StepComposer(
    IAresDeviceRepo deviceRepo,
    INotifier notifier,
    ISystemSettingsManager settingsManager,
    ICommandDisplayNameResolver commandDisplayNameResolver,
    CustomCommandExecutor? customCommandExecutor = null)
  {
    _deviceRepo = deviceRepo;
    _notifier = notifier;
    _settingsManager = settingsManager;
    _commandDisplayNameResolver = commandDisplayNameResolver;
    _customCommandExecutor = customCommandExecutor;
  }

  public StepExecutor Compose(StepTemplate template)
  {
    var executables =
      template
        .CommandTemplates
        .OrderBy(t => t.Index)
        .Select
        (
          commandTemplate =>
          {
            var commandName = _commandDisplayNameResolver.Resolve(commandTemplate);

            if(commandTemplate.CommandTypeCase == CommandTemplate.CommandTypeOneofCase.SystemCommand)
            {
              Func<CancellationToken, Task<CommandResult>> systemAction = ct => SystemOperationExecutor.Execute(
                commandTemplate.SystemCommand.Operation,
                commandTemplate.ArgumentBindings,
                ct);

              return new CommandExecutor(systemAction, commandTemplate, commandName, _notifier, _settingsManager);
            }

            if(commandTemplate.CommandTypeCase == CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation)
            {
              if(_customCommandExecutor is null)
                throw new InvalidOperationException("Custom command execution has not been configured.");

              Func<CancellationToken, Task<CommandResult>> customCommandAction = ct => _customCommandExecutor.Execute(
                commandTemplate.CustomCommandInvocation.CustomCommandId,
                commandTemplate.ArgumentBindings,
                ct);

              return new CommandExecutor(customCommandAction, commandTemplate, commandName, _notifier, _settingsManager);
            }

            if(commandTemplate.CommandTypeCase != CommandTemplate.CommandTypeOneofCase.DeviceCommand)
              throw new InvalidOperationException($"Unsupported command type: {commandTemplate.CommandTypeCase}");

            var metadata = commandTemplate.DeviceCommand?.Metadata;
            var deviceId = metadata?.DeviceId;
            if(deviceId is null)
              throw new InvalidOperationException("Device ID was null when attempting to retrieve the command interpreter");

            var device = _deviceRepo.FirstOrDefault(d => d.UniqueId == deviceId);

            if(device is not null && metadata is not null)
            {
              Func<CancellationToken, Task<CommandResult>> internalAction = async (ct)
                => await device.ExecuteCommand(
                  metadata.Name,
                  commandTemplate.ArgumentBindings.Select(p => new DeviceCommandArgument() { ArgName = p.Metadata.Name, ArgValue = p.GetValue() }).ToList(),
                  ct);

              return new CommandExecutor(internalAction, commandTemplate, commandName, _notifier, _settingsManager);
            }

            throw new InvalidOperationException("I'm not certain what to do here yet :(");
          }
        )
        .ToArray();


    return template.IsParallel
      ? new ParallelStepExecutor(template, executables)
      : new SequentialStepExecutor(template, executables, _settingsManager, _notifier);
  }

  public StepExecutor Compose(StepTemplate template, ExperimentExecutionStatus experimentExecutionStatus)
  {
    throw new NotImplementedException();
  }
}
