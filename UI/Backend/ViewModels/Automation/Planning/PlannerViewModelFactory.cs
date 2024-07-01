using Ares.Messaging;

namespace UI.Backend.ViewModels.Automation.Planning;

public class PlannerViewModelFactory
{
  private readonly AresPlanning.AresPlanningClient _client;

  public PlannerViewModelFactory(AresPlanning.AresPlanningClient client)
  {
    _client = client;
  }
}
