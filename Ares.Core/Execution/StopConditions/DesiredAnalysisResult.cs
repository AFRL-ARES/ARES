using Ares.Core.Analyzing;

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

    var analysisVal = latestAnalysis.Result;
    var resultAchieved = analysisVal >= _desiredResult - _leeway && analysisVal <= _desiredResult + _leeway;
    if (resultAchieved)
      Message = $"Achieved result {analysisVal} which is within {_leeway} of {_desiredResult}";

    return resultAchieved;
  }
}
