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
  private readonly IUiNotificationService _notificationService;
  public AnalyzerSettingsListViewModel(AnalyzerService analyzerManagerService, IUiNotificationService notificationService)
  {
    _analyzerManagerService = analyzerManagerService;
    _notificationService = notificationService;
  }

  public AnalyzerConfigEditViewModel GetNewConfigEditViewModel() => new(_analyzerManagerService);

  public async Task UpdateAvailableAnalyzers()
  {
    IsLoading = true;

    try
    {
      var response = await _analyzerManagerService.GetAllAnalyzers(new Empty(), null);
      UpdateViewModels(response.Analyzers);
    }
    catch(Exception ex)
    {
      PushNotification(new UiNotificationMessage
      {
        Summary = "Error fetching analyzers",
        Detail = ex.Message,
        Severity = UiNotificationSeverity.Error
      });
    }
    finally
    {
      IsLoading = false;
    }
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
      PushNotification(new UiNotificationMessage() 
      { 
        Detail = $"Added new analyzer {analyzerConfig.Name}", 
        Severity = UiNotificationSeverity.Success, 
        Summary = "Successfully Added Remote Analyzer" 
      });
      await UpdateAvailableAnalyzers();
    }
    else
    {
      PushNotification(
        new UiNotificationMessage() 
        {
          Detail = response.ErrorMessage,
          Summary = $"Failed to Add Analyzer {analyzerConfig.Name}.", 
          Severity = UiNotificationSeverity.Error 
        });
    }
  }

  private async Task OnAnalyzerRemoved()
  {
    SettingsViewModels = null;
    await UpdateAvailableAnalyzers();
  }

  public void PushNotification(UiNotificationMessage notification) => _notificationService.Notify(notification);

  [Reactive]
  public partial IEnumerable<AnalyzerSettingsViewModel>? SettingsViewModels { get; private set; }

  [Reactive]
  public partial bool IsLoading { get; private set; }
}


