using Ares.Core.Device.Providers;
using Ares.Core.Execution.Interaction;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class ExecutionNodeComposer : IExecutionNodeComposer
{
  // Inject all the low-level dependencies here
  private readonly IAresDeviceProvider _deviceRepo;
  private readonly IUserInteractionBroker _userBroker;
  private readonly ISystemSettingsManager _settingsManager;
  private readonly INotifier _notifier;

  public ExecutionNodeComposer(IAresDeviceProvider deviceRepo, IUserInteractionBroker userBroker, ISystemSettingsManager settingsManager, INotifier notifier)
  {
    _deviceRepo = deviceRepo;
    _userBroker = userBroker;
    _settingsManager = settingsManager;
    _notifier = notifier;
  }

  public IExecutor<CommandExecutionSummary, CommandExecutionStatus> ComposeSequence(
      string id,
      string name,
      IEnumerable<ExecutionNode> nodes)
  {
    var executables = nodes.OrderBy(n => n.Index).Select(node =>
    {
      switch(node.TemplateTypeCase)
      {
        case ExecutionNode.TemplateTypeOneofCase.CommandTemplate:
          // Return new CommandExecutor(...)
          break;

        case ExecutionNode.TemplateTypeOneofCase.SystemTemplate:
          // Return new SystemExecutor(...)
          break;

        case ExecutionNode.TemplateTypeOneofCase.LogicGate:
          var trueExecutor = node.LogicGate.TrueBranch.Any() ? ComposeSequence($"{id}-T", $"{name}-True", node.LogicGate.TrueBranch) : null;
          var falseExecutor = node.LogicGate.FalseBranch.Any() ? ComposeSequence($"{id}-F", $"{name}-False", node.LogicGate.FalseBranch) : null;
          return new LogicExecutor(node.LogicGate, trueExecutor, falseExecutor, _settingsManager, _notifier);

        default:
          throw new InvalidOperationException("Unrecognized node");
      }
    }).ToArray();

    // Wrap whatever list of nodes we just built into a Sequential manager
    return new SequentialStepExecutor(id, name, executables, _settingsManager, _notifier);
  }
}
