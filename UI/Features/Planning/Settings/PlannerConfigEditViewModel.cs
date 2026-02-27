using Ares.Datamodel.Planning;
using Ares.Services;
using Ares.Core.Grpc.Services;
using ReactiveUI;


namespace UI.Features.Planning.Settings
{
  public class PlannerConfigEditViewModel : ReactiveObject
  {
    private readonly PlannerService _client;
    private readonly PlannerServiceInfo _plannerService;
    public PlannerConfigEditViewModel(PlannerService client)
    {
      _client = client;
      _plannerService = new PlannerServiceInfo();
      NewConfig = true;
    }

    public PlannerConfigEditViewModel(PlannerService client, PlannerServiceInfo planner)
    {
      _client = client;
      _plannerService = planner;
      Name = planner.Name;
      Address = planner.Address;
    }

    public string? Name { get; set; }

    public bool NewConfig { get; set; }

    public string Address { get; set; } = "http://localhost";

    public bool Modified => _plannerService.Name != Name || _plannerService.Address != Address;

    public PlannerServiceInfo Save()
      => Modified ? new PlannerServiceInfo { Name = Name, Address = Address } : _plannerService;
  }
}
