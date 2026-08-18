using Ares.Datamodel.Planning;

namespace UI.Features.Execution.Planning;

public class ManualPlannerDisplayObject
{
  public string ExperimentNumber { get; set; } = string.Empty;
  public ManualPlannerSet Parameters { get; set; } = new();
}
