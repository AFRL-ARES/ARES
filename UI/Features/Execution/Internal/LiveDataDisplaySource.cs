using UI.Features.Execution.Enums;

namespace UI.Features.Execution.Internal;

public sealed record LiveDataDisplaySource(string Id, LiveDataDisplaySourceKind Kind, string DisplayName, string Detail)
{
  public const string PlannerSourceId = "planner";
  public const string AnalyzerSourceId = "analyzer";
}