using Ares.Core.Execution.ControlTokens;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

public class SequentialStepExecutor : StepExecutor
{
  private readonly ISystemSettingsManager _settingsManager;
  private readonly INotifier _notifier;

  public SequentialStepExecutor(StepTemplate template, 
    CommandExecutor[] commandExecutors, 
    ISystemSettingsManager settingsManager, 
    INotifier notifier) : base(template, commandExecutors)
  {
    _settingsManager = settingsManager;
    _notifier = notifier;
  }

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token)
  {
    var startTime = DateTime.UtcNow;
    var commandSummaries = new List<CommandExecutionSummary>();
    foreach (var command in CommandExecutors)
    {
      if(token.IsCancelled)
        break;

      var commandExecutionSummary = await command.Execute(token);

      //Handle Retry if needed
      var shouldRetry = await ShouldRetry(commandExecutionSummary.StatusCode);
      if(!commandExecutionSummary.Result.Success && shouldRetry)
      {
        var msg = $"ARES attempted to run the command {commandExecutionSummary.CommandName} but it failed. Based on your settings ARES will retry running this command";
        await _notifier.Notify("Retrying Command", msg, NotificationSeverityEnum.Info);
        await Task.Delay(2000);

        var retriedCommandExecutionSummary = await command.Execute(token);

        if(retriedCommandExecutionSummary.Result.Success)
          commandExecutionSummary = retriedCommandExecutionSummary;
      }

      if(commandExecutionSummary.Result.Success)
        commandSummaries.Add(commandExecutionSummary);

      else
      {
        commandSummaries.Add(commandExecutionSummary);
        return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
      }
    }

    return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
  }

  private async Task<bool> ShouldRetry(CommandStatusCode code)
  {
    var errorHandling = await _settingsManager.GetErrorHandlingByStatusCode(code);
    return errorHandling == ErrorHandling.Retry;
  }
}
