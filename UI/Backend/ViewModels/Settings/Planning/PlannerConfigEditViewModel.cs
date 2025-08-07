using Ares.Datamodel;
using Ares.Messaging.Planning;
using ReactiveUI;


namespace UI.Backend.ViewModels.Settings.Planning
{
  public class PlannerConfigEditViewModel : ReactiveObject
  {
    private readonly AresPlanning.AresPlanningClient _client;
    private readonly PlannerAdapterInfo _plannerAdapter;
    public PlannerConfigEditViewModel(AresPlanning.AresPlanningClient client)
    {
      _client = client;
      _plannerAdapter = new PlannerAdapterInfo();
      NewConfig = true;
    }

    public PlannerConfigEditViewModel(AresPlanning.AresPlanningClient client, PlannerAdapterInfo planner)
    {
      _client = client;
      _plannerAdapter = planner;
      Name = planner.AdapterName;
      Address = planner.Address;
    }

    public string? Name { get; set; }

    public bool NewConfig { get; set; }

    public string Address { get; set; } = "http://localhost";

    public bool Modified => _plannerAdapter.AdapterName != Name || _plannerAdapter.Address != Address;

    public PlannerAdapterInfo Save()
      => Modified ? new PlannerAdapterInfo { AdapterName = Name, Address = Address } : _plannerAdapter;
  }
}
