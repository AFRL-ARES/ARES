using Ares.Core.CoreDevice;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Interaction;
using Ares.Core.Execution.System;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using YamlDotNet.Core.Tokens;

namespace Ares.Core.Execution.Executors;

public class SystemExecutor : IExecutor<CommandExecutionSummary, CommandExecutionStatus>
{
  private readonly BehaviorSubject<CommandExecutionStatus> _stateSubject;
  private readonly INotifier _notifier;
  private readonly ISystemSettingsManager _settingsManager;
  private readonly IUserInteractionBroker _interactionBroker;
  private readonly SystemTemplate _template;

  public SystemExecutor(SystemTemplate template, IUserInteractionBroker interactionBroker, INotifier notifier, ISystemSettingsManager settingsManager)
  {
    _interactionBroker = interactionBroker;
    _settingsManager = settingsManager;
    _notifier = notifier;
    _template = template;

    var executionStatus = new CommandExecutionStatus
    {
      CommandId = template.UniqueId,
      CommandName = template.Metadata.Name,
      DeviceName = template.Metadata.DeviceType,
      State = ExecutionState.Undefined
    };

    _stateSubject = new BehaviorSubject<CommandExecutionStatus>(executionStatus);
    ExperimentStatusObservable = _stateSubject.AsObservable();
  }

  public IObservable<CommandExecutionStatus> ExperimentStatusObservable { get; }

  public CommandExecutionStatus Status => _stateSubject.Value;

  public async Task<CommandExecutionSummary> Execute(ExecutionControlToken token)
  => await Execute(token, new Dictionary<string, AresValue>());

  public async Task<CommandExecutionSummary> Execute(ExecutionControlToken token, IReadOnlyDictionary<string, AresValue> variableScope)
  {
    Status.State = ExecutionState.Running;
    _stateSubject.OnNext(Status);

    var timeStarted = DateTime.UtcNow;
    var variableResolutionError = CommandVariableResolver.ResolveParameters(_template.Parameters, variableScope);
    var arguments = _template.Parameters.Select(p => new DeviceCommandArgument() { ArgName = p.Metadata.Name, ArgValue = p.GetValue() }).ToList();
    CommandResult result = new CommandResult { Success = true };

    double duration = 0;
    var durationParam = arguments.FirstOrDefault(param => param.ArgName == AresCoreDeviceCommandParameter.Duration.ToString())?.ArgValue;
    var found = durationParam?.TryGetNumericValue(out duration);

    try
    {
      // Execute the system logic natively
      switch(_template.Operation)
      {
        case SystemOperation.SleepForMilliseconds:
          if(found is null || found == false)
          {
            result.Success = false;
            result.Error = "Invalid duration parameter, no numeric value was provided";
            break;
          }

          await Task.Delay(TimeSpan.FromMilliseconds(duration), token.CancellationToken);
          break;

        case SystemOperation.SleepForSeconds:
          if(found is null || found == false)
          {
            result.Success = false;
            result.Error = "Invalid duration parameter, no numeric value was provided";
            break;
          }

          await Task.Delay(TimeSpan.FromSeconds(duration), token.CancellationToken);
          break;

        case SystemOperation.SleepForMinutes:
          if(found is null || found == false)
          {
            result.Success = false;
            result.Error = "Invalid duration parameter, no numeric value was provided";
            break;
          }

          await Task.Delay(TimeSpan.FromMinutes(duration), token.CancellationToken);
          break;

        case SystemOperation.WaitForUserInput:
          Status.State = ExecutionState.Waiting;
          _stateSubject.OnNext(Status);

          var promptArg = arguments.FirstOrDefault(a => a.ArgName == SystemCommandParameters.UserPrompt.ToString())?.ArgValue;

          if(promptArg is null || !promptArg.HasStringValue)
          {
            result.Success = false;
            result.Error = "Invalid user prompt parameter, no string value was provided";
            break;
          }

          var userInput = await _interactionBroker.RequestInputAsync(promptArg.StringValue, token.CancellationToken);
          result.Result = AresValueHelper.CreateString(userInput);
          break;

        case SystemOperation.WaitForUser:
          Status.State = ExecutionState.AwaitingUser;
          _stateSubject.OnNext(Status);

          var userPromptArg = arguments.FirstOrDefault(a => a.ArgName == SystemCommandParameters.UserPrompt.ToString())?.ArgValue;

          if(userPromptArg is null || !userPromptArg.HasStringValue)
          {
            result.Success = false;
            result.Error = "Invalid user prompt parameter, no string value was provided";
            break;
          }

          var userConfirmation = await _interactionBroker.RequestConfirmation(userPromptArg.StringValue, token.CancellationToken);

          if(!userConfirmation)
          {
            result.Success = false;
            result.Error = "User Confirmation Rejected";
          }

          break;
      }
    }
    catch(Exception ex)
    {
      result.Success = false;
      result.Error = ex.Message;
    }

    Status.State = result.Success ? ExecutionState.Succeeded : ExecutionState.Failed;
    Status.Result = result.Result;
    _stateSubject.OnNext(Status);

    return ExecutorSummaryHelpers.CreateCommandExecutionSummary(_template, result, timeStarted, DateTime.UtcNow);
  }
}
