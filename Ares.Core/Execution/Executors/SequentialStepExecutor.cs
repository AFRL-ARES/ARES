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
    IEnumerable<IExecutor<CommandExecutionSummary, CommandExecutionStatus>> executionNodes, 
    ISystemSettingsManager settingsManager, 
    INotifier notifier) : base(template, executionNodes)
  {
    _settingsManager = settingsManager;
    _notifier = notifier;
  }

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token)
    => await Execute(token, new Dictionary<string, AresValue>());

  public override async Task<StepExecutionSummary> Execute(ExecutionControlToken token, IReadOnlyDictionary<string, AresValue> variableScope)
  {
    var startTime = DateTime.UtcNow;
    var commandSummaries = new List<CommandExecutionSummary>();
    var combinedScope = new Dictionary<string, AresValue>(variableScope);
    var currentSettings = await _settingsManager.GetAresGeneralSettings();

    foreach (var command in CommandExecutors)
    {
      if(token.IsCancelled)
        break;

      var commandExecutionSummary = await command.Execute(token, combinedScope);

      //Handle Retry if needed
      var shouldRetry = await ShouldRetry(commandExecutionSummary.StatusCode);

      if(!commandExecutionSummary.Result.Success && shouldRetry)
      {
        var commandRetries = 0;
        var retryLimit = currentSettings?.CommandRetryLimit ?? 1;

        while(commandRetries < retryLimit)
        {
          commandRetries++;
          var msg = $"ARES attempted to run the command {commandExecutionSummary.CommandName} but it failed. " +
            $"Based on your settings ARES will retry running this command up to {retryLimit} times, this is attempt {commandRetries}";
          
          await _notifier.Notify("Retrying Command", msg, NotificationSeverityEnum.Info);


          if(currentSettings is not null)
            await Task.Delay(currentSettings.RetryCooldown.ToTimeSpan());

          var retriedCommandExecutionSummary = await command.Execute(token, combinedScope);

          if(retriedCommandExecutionSummary.Result.Success)
          {
            commandExecutionSummary = retriedCommandExecutionSummary;
            break;
          }
        }

        if(commandRetries == retryLimit && !commandExecutionSummary.Result.Success)
        {
          await _notifier.Notify("Maximum Command Retries Exceeded", 
            "ARES retried a failed command based on your settings, but exceeded the maximum number of allowed retries. Execution will stop.", 
            NotificationSeverityEnum.Error);
        }
      }

      commandSummaries.Add(commandExecutionSummary);

      if(commandExecutionSummary.Result.Success)
      {
        foreach(var variable in CommandVariableResolver.CreateVariableScope([commandExecutionSummary]))
          combinedScope[variable.Key] = variable.Value;
      }

      else
        return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
    }

    return ExecutorSummaryHelpers.CreateStepExecutionSummary(startTime, DateTime.UtcNow, commandSummaries);
  }

  private async Task<bool> ShouldRetry(CommandStatusCode code)
  {
    var errorHandling = await _settingsManager.GetErrorHandlingByStatusCode(code);
    return errorHandling == ErrorHandling.RetryCommand;
  }
}
