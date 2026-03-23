using Ares.Core.Device.Repos;
using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class StepComposer : ICommandComposer<StepTemplate, StepExecutor>
{
  private readonly INotifier _notifier;
  private readonly IAresDeviceRepo _deviceRepo;

  public StepComposer(IAresDeviceRepo deviceRepo, INotifier notifier)
  {
    _deviceRepo = deviceRepo;
    _notifier = notifier;
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
            var deviceId = commandTemplate.Metadata?.DeviceId;
            if(deviceId is null)
              throw new InvalidOperationException("Device ID was null when attempting to retrieve the command interpreter");

            var device = _deviceRepo.FirstOrDefault(d => d.UniqueId == deviceId);

            if(device is not null && commandTemplate.Metadata is not null)
            {
              var commandArgs = new List<DeviceCommandArgument>();
              commandArgs.AddRange(commandTemplate.Parameters.Select(p => new DeviceCommandArgument() { ArgName = p.Metadata.Name, ArgValue = p.Value }));

              Func<CancellationToken, Task<CommandResult>> internalAction = async (ct)
                => await device.ExecuteCommand(commandTemplate.Metadata.Name, commandArgs, ct);

              return new CommandExecutor(internalAction, commandTemplate, _notifier);
            }

            throw new InvalidOperationException("I'm not certain what to do here yet :(");
          }
        )
        .ToArray();


    return template.IsParallel
      ? new ParallelStepExecutor(template, executables)
      : new SequentialStepExecutor(template, executables);
  }

  public StepExecutor Compose(StepTemplate template, ExperimentExecutionStatus experimentExecutionStatus)
  {
    throw new NotImplementedException();
  }
}
