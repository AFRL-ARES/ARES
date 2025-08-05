using Ares.Messaging;
using Ares.Messaging.Analyzing;
using Google.Protobuf.Collections;

namespace Ares.Core.Analyzing;

public class AnalysisHelper
{
  readonly IAnalyzerRepo _analyzerRepo;
  public AnalysisHelper(IAnalyzerRepo analyzerRepo)
  {
    _analyzerRepo = analyzerRepo;
  }

  public async Task<Analysis> Analyze(ExperimentTemplate template, ExperimentExecutionSummary experimentSummary, CancellationToken cancellationToken)
  {
    var analyzer = GetAnalyzer(template.AnalyzerId);
    var analyzerInputs = ExperimentOutputToAnalyzerInputs(
      experimentSummary.CompletedExperiment.Result,
      template.AnalyzerMaps);
    // TODO: Add support for settings
    var analysis = await analyzer.Analyze(analyzerInputs, cancellationToken);
    experimentSummary.CompletedExperiment.AnalysisResult = analysis.Result;
    return analysis;
  }

  private IAnalyzer GetAnalyzer(string? analyzerId)
  {
    if(analyzerId is null)
    {
      var noneAnalyzer = _analyzerRepo.GetAnalyzerByName("NONE");
      if(noneAnalyzer is null)
      {
        throw new InvalidOperationException(
          "No analyzer provided and the default NONE analyzer was not found.");
      }

      return noneAnalyzer;
    }

    return _analyzerRepo
    .GetAnalyzerById(analyzerId) ?? throw new InvalidOperationException($"Could not find desired analyzer with id {analyzerId}");
  }

  private AresStruct ExperimentOutputToAnalyzerInputs(AresStruct experimentResult, MapField<string, string> analyzerMappings)
  {
    var mappedStruct = new AresStruct();
    foreach(var map in analyzerMappings)
    {
      var expResultValue = experimentResult.Fields[map.Value];
      mappedStruct.Fields[map.Key] = expResultValue;
    }

    return mappedStruct;
  }
}
