namespace UI.Application.Execution;

public class CampaignSummaryDisplay
{
  public string SummaryId { get; set; } = string.Empty;
  public string CampaignName { get; set; } = string.Empty;
  public int NumExperiments { get; set; }
  public DateTime CompletionTimeDateTime { get; set; }
  public Google.Protobuf.WellKnownTypes.Timestamp? OriginalTimestamp { get; set; }
}
