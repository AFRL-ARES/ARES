using Ares.Messages.DeviceStates;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class DeviceStateFilterViewModel : ReactiveObject
{
  readonly AresAutomation.AresAutomationClient _automationClient;

  public DeviceStateFilterViewModel(
    AresAutomation.AresAutomationClient automationClient,
    ICombinedDeviceStateIdGetter idGetter)
  {
    _automationClient = automationClient;
    _automationClient.GetAvailableCampaignResultsAsync(new Empty()).ResponseAsync
      .ContinueWith(task => UpdateCampaigns(task.Result));
    idGetter.GetAvailableIds()
      .ContinueWith(task => AvailableDevices = task.Result);

    var currentTime = DateTime.Now;
    // probably don't need millisecond precision
    var truncatedTime = new DateTime(currentTime.Ticks - (currentTime.Ticks % TimeSpan.TicksPerMinute));
    StartTime = truncatedTime - TimeSpan.FromHours(1);
    EndTime = truncatedTime;
  }

  private void UpdateCampaigns(AvailableCampaignResultsResponse response)
  {
    Campaigns = response.AvailableCampaignResults.Select(result => new CampaignResultMetadata { ResultId = result.ResultId, CampaignName = $"result.CampaignName-{result.CompletionTime}", CompletionTime = result.CompletionTime });
  }

  [Reactive]
  public IEnumerable<string>? AvailableDevices { get; private set; }

  public IEnumerable<string>? SelectedDevices { get; set; }

  [Reactive]
  public IEnumerable<CampaignResultMetadata>? Campaigns { get; private set; }

  public async Task UpdateExperiments(string? campaignResultId)
  {
    Experiments = null;
    var campaignResult = await _automationClient.GetCampaignResultAsync(
      new CampaignResultRequest { ResultId = campaignResultId });
    Experiments = campaignResult.ExperimentResults;
  }

  [Reactive]
  public IEnumerable<ExperimentResult>? Experiments { get; private set; }

  public string? SelectedExperimentId { get; set; }

  public DateTime StartTime { get; set; }

  public DateTime EndTime { get; set; }

  public TimeSpan Interval { get; set; }

  public bool UseStartTime { get; set; }
  public bool UseEndTime { get; set; }
  public bool UseExperiment { get; set; }

  public StateRequest GetStateRequest()
  {
    var request = new StateRequest
    {
      Start = UseStartTime ? StartTime.ToUniversalTime().ToTimestamp() : default,
      End = UseEndTime ? EndTime.ToUniversalTime().ToTimestamp() : default,
      CompletedExperimentId = UseExperiment ? SelectedExperimentId : string.Empty,
      Interval = Interval.ToDuration()
    };

    if (SelectedDevices is not null)
      request.DeviceIds.AddRange(SelectedDevices);

    return request;
  }
}


