namespace Ares.Core.Execution.StopConditions;
public interface IDesiredAnalysisResultFactory
{
  DesiredAnalysisResult Create(double desiredResult, double leeway);
}
