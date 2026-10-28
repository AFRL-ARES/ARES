using System.Reactive.Linq;
using Ares.Core.CustomCommands;
using Ares.Core.Execution;
using Ares.Core.Execution.Executors;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Ares.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;

namespace UI.Features.CustomCommand;

public sealed partial class CustomCommandDashboardViewModel : ReactiveObject, IDisposable
{
  private readonly ICustomCommandPersistenceService _customCommandPersistenceService;
  private readonly CustomCommandExecutor _customCommandExecutor;
  private readonly IExecutionReportStore _executionReportStore;
  private readonly IDisposable _campaignStatusSubscription;
  private CancellationTokenSource? _executionCancellation;
  private bool _campaignCancellationRequested;
  private readonly IAresUINotificationService _notificationService;
  private readonly ILogger<CustomCommandDashboardViewModel> _logger;

  public CustomCommandDashboardViewModel(
    ICustomCommandPersistenceService customCommandPersistenceService,
    CustomCommandExecutor customCommandExecutor,
    IExecutionReportStore executionReportStore,
    IAresUINotificationService notificationService,
    ILogger<CustomCommandDashboardViewModel> logger)
  {
    _customCommandPersistenceService = customCommandPersistenceService;
    _customCommandExecutor = customCommandExecutor;
    _executionReportStore = executionReportStore;
    UpdateCampaignStatus(executionReportStore.CampaignExecutionStatus);
    _campaignStatusSubscription = executionReportStore.CampaignStatusObservable.Subscribe(UpdateCampaignStatus);
    _notificationService = notificationService;
    _logger = logger;
  }

  public async Task InitializeAsync()
  {
    if(AvailableCommands.Count == 0 && !IsLoading)
      await RefreshAsync();
  }

  public async Task RefreshAsync()
  {
    if(IsLoading || IsRunning)
      return;

    IsLoading = true;
    LoadError = null;
    var selectedCommandId = SelectedCommandId;

    try
    {
      AvailableCommands = (await _customCommandPersistenceService.GetCommandsAsync())
        .OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

      var selectedCommand = AvailableCommands.FirstOrDefault(command => string.Equals(command.CustomCommandId, selectedCommandId, StringComparison.OrdinalIgnoreCase));
      SelectCommand(selectedCommand?.CustomCommandId);
    }
    catch(Exception exception)
    {
      AvailableCommands = [];
      SelectCommand(null);
      LoadError = $"Failed to load custom commands. {exception.Message}";
    }
    finally
    {
      IsLoading = false;
    }
  }

  public void SelectCommand(string? customCommandId)
  {
    if(IsRunning)
      return;

    SelectedCommand = AvailableCommands.FirstOrDefault(command =>
      string.Equals(command.CustomCommandId, customCommandId, StringComparison.OrdinalIgnoreCase));
    Arguments = SelectedCommand?.InputParameters
      .Select((parameter, index) => new CustomCommandDashboardArgument(
        parameter.Name,
        parameter.Schema?.Clone() ?? new AresValueSchema(),
        AresValueHelper.CreateDefaultValue(parameter.Schema ?? new AresValueSchema()),
        index))
      .ToList() ?? [];
    Result = null;
    ExecutionError = null;
    ExecutionDuration = null;
  }

  public void UpdateArgument(string name, AresValue value)
  {
    var argument = Arguments.FirstOrDefault(argument => argument.Name == name);
    argument?.Value = value;
  }

  public async Task RunAsync()
  {
    if(!CanRun || SelectedCommand is null)
      return;

    if(IsCampaignStateActive(_executionReportStore.CampaignExecutionStatus?.State))
    {
      IsCampaignActive = true;
      ExecutionError = "Custom commands are unavailable while a campaign is active.";
      return;
    }

    IsRunning = true;
    Result = null;
    ExecutionError = null;
    ExecutionDuration = null;
    ExecutionStarted = DateTime.UtcNow;
    _campaignCancellationRequested = false;
    _executionCancellation = new CancellationTokenSource();

    try
    {
      if(IsCampaignStateActive(_executionReportStore.CampaignExecutionStatus?.State))
      {
        IsCampaignActive = true;
        ExecutionError = "Custom commands are unavailable while a campaign is active.";
        return;
      }

      var result = await _customCommandExecutor.Execute(SelectedCommand.CustomCommandId, BuildBindings(), _executionCancellation.Token);

      if(result.Success)
      {
        Result = result;

        _logger.LogInformation("Successfully Executed Custom Command {command} from Dashboard", SelectedCommand.Name);

        _notificationService.PushNotification(new AresNotification()
        {
          Title = "Successfully Executed Custom Command",
          Message = $"ARES successfully executed the custom command {SelectedCommand.Name} from the dashboard.",
          NotificationSeverity = Severity.Success
        });
      }
        

      else
        ExecutionError = result.Error;
    }
    catch(OperationCanceledException) when(_executionCancellation.IsCancellationRequested)
    {
      ExecutionError = _campaignCancellationRequested ? "Execution was cancelled because a campaign became active." : "Execution was cancelled.";
      _logger.LogWarning("Failed to execute custom command {command} from dashboard due to it being cancelled", SelectedCommand.Name);
      _notificationService.PushNotification(new AresNotification
      {
        Title = "Custom Command Execution Failed",
        Message = ExecutionError,
        NotificationSeverity = Severity.Warning
      });
    }
    catch(Exception exception)
    {
      ExecutionError = exception.Message;

      _logger.LogError(exception, "Failed to execute custom command {command} from dashboard", SelectedCommand.Name);
      _notificationService.PushNotification(new AresNotification
      {
        Title = "Custom Command Execution Failed",
        Message = ExecutionError,
        NotificationSeverity = Severity.Warning
      });
    }
    finally
    {
      if(ExecutionStarted is not null)
        ExecutionDuration = DateTime.UtcNow - ExecutionStarted.Value;

      _executionCancellation.Dispose();
      _executionCancellation = null;
      IsRunning = false;
    }
  }

  public void Cancel()
  {
    _campaignCancellationRequested = false;
    _executionCancellation?.Cancel();
  }

  public void Dispose()
  {
    _campaignStatusSubscription.Dispose();
    _executionCancellation?.Cancel();
    _executionCancellation?.Dispose();
  }

  private Parameter[] BuildBindings()
  {
    return Arguments
      .OrderBy(argument => argument.Index)
      .Select(argument => new Parameter
      {
        UniqueId = Guid.NewGuid().ToString(),
        Index = argument.Index,
        Metadata = new ParameterMetadata
        {
          UniqueId = Guid.NewGuid().ToString(),
          Name = argument.Name,
          Index = argument.Index,
          Schema = argument.Schema.Clone()
        },
        LiteralSource = new LiteralParameterSource { Value = argument.Value.Clone() }
      })
      .ToArray();
  }

  private void UpdateCampaignStatus(CampaignExecutionStatus? status)
  {
    var isActive = IsCampaignStateActive(status?.State);
    IsCampaignActive = isActive;
    if(!isActive || !IsRunning)
      return;

    _campaignCancellationRequested = true;
    _executionCancellation?.Cancel();
  }

  internal static bool IsCampaignStateActive(ExecutionState? state)
  {
    return state switch
    {
      null => false,
      ExecutionState.Undefined => false,
      ExecutionState.Succeeded => false,
      ExecutionState.Failed => false,
      ExecutionState.Waiting => true,
      ExecutionState.AwaitingUser => true,
      ExecutionState.Running => true,
      ExecutionState.Paused => true,
      ExecutionState.WaitingForUserDecision => true,
      ExecutionState.Replanning => true,
      ExecutionState.Retrying => true,
      ExecutionState.EnteringSafeMode => true,
      ExecutionState.Planning => true,
      ExecutionState.Analyzing => true,
      ExecutionState.GenerateExecutor => true,
      ExecutionState.InitializeExperiment => true,
      _ => true
    };
  }

  private void RaiseCanRunChanged()
    => this.RaisePropertyChanged(nameof(CanRun));

  [Reactive]
  public partial List<CustomCommandVersion> AvailableCommands { get; private set; } = [];

  [Reactive]
  public partial List<CustomCommandDashboardArgument> Arguments { get; private set; } = [];

  [Reactive]
  public partial CommandResult? Result { get; private set; }

  [Reactive]
  public partial string? LoadError { get; private set; }

  [Reactive]
  public partial string? ExecutionError { get; private set; }

  [Reactive]
  public partial DateTime? ExecutionStarted { get; private set; }

  [Reactive]
  public partial TimeSpan? ExecutionDuration { get; private set; }

  public bool CanRun => SelectedCommand is not null && !IsLoading && !IsRunning && !IsCampaignActive;

  public CustomCommandVersion? SelectedCommand
  {
    get;
    private set
    {
      this.RaiseAndSetIfChanged(ref field, value);
      this.RaisePropertyChanged(nameof(SelectedCommandId));
      RaiseCanRunChanged();
    }
  }

  public string? SelectedCommandId => SelectedCommand?.CustomCommandId;

  public bool IsLoading
  {
    get;
    private set
    {
      this.RaiseAndSetIfChanged(ref field, value);
      RaiseCanRunChanged();
    }
  }

  public bool IsRunning
  {
    get;
    private set
    {
      this.RaiseAndSetIfChanged(ref field, value);
      RaiseCanRunChanged();
    }
  }

  public bool IsCampaignActive
  {
    get;
    private set
    {
      this.RaiseAndSetIfChanged(ref field, value);
      RaiseCanRunChanged();
    }
  }
}