using Ares.Core.Device;
using Ares.Core.Notifications;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class StepComposer : ICommandComposer<StepTemplate, StepExecutor>
{
  private readonly IDeviceCommandInterpreterRepo _interpreterRepo;
  private readonly INotifier _notifier;


  public StepComposer(IDeviceCommandInterpreterRepo interpreterRepo, INotifier notifier)
  {
    _interpreterRepo = interpreterRepo;
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
            {
              throw new InvalidOperationException("Device ID was null when attempting to retrieve the command interpreter");
            }
            var commandInterpreter =
              _interpreterRepo
                .GetCommandInterpreterByDeviceId(deviceId);

            var command = commandInterpreter.TemplateToDeviceCommand(commandTemplate);
            var executable = new CommandExecutor(command, commandTemplate, _notifier);
            return executable;
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
