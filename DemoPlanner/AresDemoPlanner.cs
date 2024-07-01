using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Core.Planning;
using Ares.Messaging;

namespace DemoPlanner;
public class AresDemoPlanner : IPlanner
{
  private readonly ISubject<PlannerState> _plannerStateSubject = new BehaviorSubject<PlannerState>(Ares.Core.Planning.PlannerState.Disconnected);
  readonly Uri _address;
  public AresDemoPlanner(string name, Uri address)
  {
    _address = address;
    Name = name;
    PlannerState = _plannerStateSubject.AsObservable();
  }
  public string Name { get; }

  public Version Version { get; } = new Version(1, 0);

  public IObservable<PlannerState> PlannerState { get; }

  public async Task<IEnumerable<Ares.Core.Planning.PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<Analysis> experimentAnalyses, CancellationToken cancellationToken)
  {
    var client = ClientStore.DemoPlanningClient;
    var planRequest = new PlanRequest();
    planRequest.Analyses.AddRange(experimentAnalyses);
    planRequest.Metadata.AddRange(plannableParameters);
    var result = await client.PlanAsync(planRequest);
    return result.PlanResults.Select(result => new Ares.Core.Planning.PlanResult(result.Metadata, result.Value));
  }

  public void Init()
  {
    ClientStore.CreateClient(_address);
    _plannerStateSubject.OnNext(Ares.Core.Planning.PlannerState.Connected);
  }
}
