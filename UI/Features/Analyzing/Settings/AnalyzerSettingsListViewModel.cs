using Ares.Datamodel.Analyzing;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Notifications;

namespace UI.Features.Analyzing.Settings;

public partial class AnalyzerSettingsListViewModel : ReactiveObject
{
  private readonly AnalyzerService _analyzerManagerService;
  private readonly INotificationReceivingService _notificationService;
  public AnalyzerSettingsListViewModel(AnalyzerService analyzerManagerService, INotificationReceivingService notificationService)
  {
    _analyzerManagerService = analyzerManagerService;
    _notificationService = notificationService;
    UpdateAvailableAnalyzers();
  }

  public AnalyzerConfigEditViewModel GetNewConfigEditViewModel() => new(_analyzerManagerService);

  private Task UpdateAvailableAnalyzers()
  {
    SettingsViewModels = null;
    return _analyzerManagerService
      .GetAllAnalyzers(new Empty(), null)
      .ContinueWith(task => { if(task.IsCompletedSuccessfully) UpdateViewModels(task.Result.Analyzers); });
  }

  private void UpdateViewModels(IEnumerable<AnalyzerInfo> analyzers)
  {
    analyzers = [.. analyzers.Where(a => !a.Name.Equals("NONE"))];
    var viewModels = analyzers.Select(info => new AnalyzerSettingsViewModel(_analyzerManagerService, _notificationService, info, OnAnalyzerRemoved)).ToArray();
    SettingsViewModels = viewModels;
  }

  public async Task AddNewAnalyzer(AnalyzerConfig analyzerConfig)
  {
    var request = new AddRemoteAnalyzerRequest() { Name = analyzerConfig.Name, Url = analyzerConfig.Url };
    var response = await _analyzerManagerService.AddRemoteAnalyzer(request, null);
    if(response.Success)
    {
      PushNotification(new AresNotification() { Message = $"Added new analyzer {analyzerConfig.Name}", NotificationSeverity = Severity.Success, Title = "Successfully Added Remote Analyzer" });
      await UpdateAvailableAnalyzers();
    }
    else
    {
      PushNotification(
        new AresNotification() { Message = $"Failed to add analyzer {analyzerConfig.Name}. {response.ErrorMessage}", NotificationSeverity = Severity.Error });
    }
  }

  private async Task OnAnalyzerRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailableAnalyzers();
  }

  public void PushNotification(AresNotification notification) => _notificationService.PushNotification(notification);

  [Reactive]
  public partial IEnumerable<AnalyzerSettingsViewModel>? SettingsViewModels { get; private set; }
}


