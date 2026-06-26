using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Extensions;

namespace Ares.Core.Tests.Data.Analyzer;

public class TestReplyAnalyzer : AnalyzerBase
{
  public TestReplyAnalyzer() : base("Test Analyzer", "TestAnalyzer", "1.0")
  {
  }

  public override Task<AnalysisResponse> Analyze(AnalysisRequest request, CancellationToken cancellationToken)
  {
    var firstData = request.Inputs.Fields["TestAnalyzerInput"];
    var objective = new Objective() { ObjectiveName = "Objective", ObjectiveValue = firstData };
    var analysis = new AnalysisResponse() { Objectives = { objective }, AnalysisOutcome = Outcome.Success };


    return Task.FromResult(analysis);
  }

  public override Task<AnalysisResponse> Analyze(AnalysisRequest request, AresStruct settings, CancellationToken cancellationToken)
  {
    return Analyze(request, cancellationToken);
  }

  public override Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken)
  {
    return Task.FromResult(new AnalyzerCapabilities());
  }

  public override Task<AresStructSchema> GetParameters(CancellationToken cancellationToken)
  {
    var schema = new AresStructSchema();
    var testReplySchema = new AresValueSchema() { Optional = false, Type = AresDataType.Number };

    schema.Fields["TestReply"] = testReplySchema;

    return Task.FromResult(schema);
  }
}
