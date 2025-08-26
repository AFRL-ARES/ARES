using Ares.Datamodel.Planning;

namespace UI.Backend.ViewModels.Automation.Planning;

public class ManualPlannerDisplayObject
{
  public string ExperimentNumber { get; set; } = string.Empty;
  public ManualPlannerSet Parameters { get; set; } = new();
}
