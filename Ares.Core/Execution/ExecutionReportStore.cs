using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Datamodel;

namespace Ares.Core.Execution;

/// <summary>
/// Used by the execution reporter to store the state of currently running campaigns
/// Has both a campaign execution status as well as an experiment execution status to make it easier to keep track of
/// status
/// because campaign execution status will keep growing in size with every completed experiment, so it might be inefficient
/// to grab the whole campaign status and then find the latest experiment status within it.
/// </summary>
internal class ExecutionReportStore : IExecutionReportStore
{
  private readonly ISubject<CampaignExecutionStatus?> _campaignExecutionStatusSubject = new BehaviorSubject<CampaignExecutionStatus?>(null);
  private readonly ISubject<ExperimentExecutionStatus?> _experimentExecutionStatusSubject = new BehaviorSubject<ExperimentExecutionStatus?>(null);

  private CampaignExecutionStatus? _campaignExecutionStatus;
  private ExperimentExecutionStatus? _experimentExecutionStatus;

  public ExecutionReportStore()
  {
    CampaignStatusObservable = _campaignExecutionStatusSubject.AsObservable();
    ExperimentStatusObservable = _experimentExecutionStatusSubject.AsObservable();
  }

  public IObservable<CampaignExecutionStatus?> CampaignStatusObservable { get; }
  public IObservable<ExperimentExecutionStatus?> ExperimentStatusObservable { get; }

  public CampaignExecutionStatus? CampaignExecutionStatus
  {
    get => _campaignExecutionStatus;

    set
    {
      _campaignExecutionStatus = value;
      _campaignExecutionStatusSubject.OnNext(value);
    }
  }
  public ExperimentExecutionStatus? ExperimentExecutionStatus
  {
    get => _experimentExecutionStatus;

    set
    {
      _experimentExecutionStatus = value;
      _experimentExecutionStatusSubject.OnNext(value);
    }
  }
}
