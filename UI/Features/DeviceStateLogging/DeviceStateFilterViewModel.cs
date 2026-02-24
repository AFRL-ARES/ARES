using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Messages.DeviceStates;
using Ares.Services;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.DeviceStateLogging;

public partial class DeviceStateFilterViewModel : ReactiveObject
{
  private readonly AresDevices.AresDevicesClient _deviceClient;
  readonly AresAutomation.AresAutomationClient _automationClient;

  public DeviceStateFilterViewModel(AresDevices.AresDevicesClient devicesClient, AresAutomation.AresAutomationClient automationClient)
  {
    _automationClient = automationClient;
    _deviceClient = devicesClient;
    _automationClient.GetAvailableCampaignExecutionSummariesAsync(new Empty()).ResponseAsync
      .ContinueWith(task => UpdateCampaigns(task.Result));
    _deviceClient.GetAllAvailableDevicesAsync(new Empty()).ResponseAsync
      .ContinueWith(task => UpdateDevices(task.Result));

    var currentTime = DateTime.Now;
    // probably don't need millisecond precision
    var truncatedTime = new DateTime(currentTime.Ticks - (currentTime.Ticks % TimeSpan.TicksPerMinute));
    StartTime = truncatedTime - TimeSpan.FromHours(1);
    EndTime = truncatedTime;
  }

  private void UpdateCampaigns(AvailableCampaignExecutionSummariesResponse response)
  {
    Campaigns = response.AvailableCampaignSummaries.Select(result => new CampaignExecutionSummaryMetadata { SummaryId = result.SummaryId, CampaignName = $"result.CampaignName-{result.CompletionTime}", CompletionTime = result.CompletionTime });
  }

  private void UpdateDevices(AvailableDevicesResponse response)
  {
    AvailableDevices = response.Devices.ToList();
  }

  [Reactive]
  public partial IEnumerable<AresDeviceDescription>? AvailableDevices { get; private set; }

  public IEnumerable<DevicesDescription>? SelectedDevices { get; set; }

  [Reactive]
  public partial IEnumerable<CampaignExecutionSummaryMetadata>? Campaigns { get; private set; }

  public async Task UpdateExperiments(string? campaignResultId)
  {
    Experiments = null;
    var campaignResult = await _automationClient.GetCampaignSummaryAsync(
      new CampaignExecutionSummaryRequest { SummaryId = campaignResultId });
    Experiments = campaignResult.ExperimentSummaries;
  }

  [Reactive]
  public partial IEnumerable<ExperimentExecutionSummary>? Experiments { get; private set; }

  public string? SelectedExperimentId { get; set; }

  public DateTime StartTime { get; set; }

  public DateTime EndTime { get; set; }

  public TimeSpan Interval { get; set; }

  public bool UseStartTime { get; set; }
  public bool UseEndTime { get; set; }
  public bool UseExperiment { get; set; }

  public DeviceStateRequestFilter GetStateRequestFilter()
  {
    var request = new DeviceStateRequestFilter
    {
      Start = UseStartTime ? StartTime.ToUniversalTime().ToTimestamp() : default,
      End = UseEndTime ? EndTime.ToUniversalTime().ToTimestamp() : default,
      CompletedExperimentId = UseExperiment ? SelectedExperimentId : string.Empty,
      Interval = Interval.ToDuration()
    };

    if(SelectedDevices is not null)
      request.DeviceIds.AddRange(SelectedDevices.Select(d => d.DeviceId));

    return request;
  }
}


