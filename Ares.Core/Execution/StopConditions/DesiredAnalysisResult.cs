using Ares.Core.Analyzing;
using Ares.Datamodel.Extensions;

namespace Ares.Core.Execution.StopConditions;

public class DesiredAnalysisResult : IStopCondition
{
  readonly AnalysisRepo _analyses;
  private readonly double _desiredResult;
  private readonly double _leeway;

  public DesiredAnalysisResult(AnalysisRepo analyses, double desiredResult, double leeway)
  {
    _analyses = analyses;
    _desiredResult = desiredResult;
    _leeway = leeway;
  }

  public string Message { get; private set; } = "";

  public string Description => $"Will stop when analysis reaches {_desiredResult} ± {_leeway}";

  public bool ShouldStop()
  {
    var latestAnalysis = _analyses.LastOrDefault();
    if (latestAnalysis is null)
      return false;

    //TODO: Fix to check things properly
    var objValue = latestAnalysis.Objectives.FirstOrDefault()?.ObjectiveValue ?? new();
    var gotAnalysisVal = AresValueHelper.TryGetNumericValue(objValue, out var analysisVal);

    if(!gotAnalysisVal)
    {
      Message = "Stopping because analysis value could not be parsed. I need to be updated to handle more objectives.";
      return true;
    }

    var resultAchieved = analysisVal >= _desiredResult - _leeway && analysisVal <= _desiredResult + _leeway;
    if (resultAchieved)
      Message = $"Achieved result {analysisVal} which is within {_leeway} of {_desiredResult}";

    return resultAchieved;
  }
}
