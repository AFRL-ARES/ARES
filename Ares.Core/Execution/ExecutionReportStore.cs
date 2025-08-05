using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Messaging;

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
  private readonly ISubject<CampaignStartupStatus?> _campaignStartupStatusSubject = new BehaviorSubject<CampaignStartupStatus?>(null);
  private readonly ISubject<CampaignCloseoutStatus?> _campaignCloseoutStatusSubject = new BehaviorSubject<CampaignCloseoutStatus?>(null);

  private CampaignExecutionStatus? _campaignExecutionStatus;
  private ExperimentExecutionStatus? _experimentExecutionStatus;
  private CampaignStartupStatus? _campaignStartupStatus;
  private CampaignCloseoutStatus? _campaignCloseoutStatus;

  public ExecutionReportStore()
  {
    CampaignStatusObservable = _campaignExecutionStatusSubject.AsObservable();
    ExperimentStatusObservable = _experimentExecutionStatusSubject.AsObservable();
    CampaignStartupStatusObservable = _campaignStartupStatusSubject.AsObservable();
    CampaignCloseoutStatusObservable = _campaignCloseoutStatusSubject.AsObservable();
  }

  public IObservable<CampaignExecutionStatus?> CampaignStatusObservable { get; }
  public IObservable<ExperimentExecutionStatus?> ExperimentStatusObservable { get; }
  public IObservable<CampaignStartupStatus?> CampaignStartupStatusObservable { get; }
  public IObservable<CampaignCloseoutStatus?> CampaignCloseoutStatusObservable { get; }

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

  public CampaignStartupStatus? CampaignStartupStatus 
  {
    get => _campaignStartupStatus; 
    
    set 
    { 
      _campaignStartupStatus = value;
      _campaignStartupStatusSubject.OnNext(value);
    } 
  }

  public CampaignCloseoutStatus? CampaignCloseoutStatus 
  { 
    get => _campaignCloseoutStatus;
    
    set
    {
      _campaignCloseoutStatus = value;
      _campaignCloseoutStatusSubject.OnNext(value);
    }
  }
}
