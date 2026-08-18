using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;

namespace Ares.Core.Tests.Data.Analyzer;

public class TestReplyAnalyzer : AnalyzerBase
{
  public TestReplyAnalyzer() : base("Test Analyzer", "TestAnalyzer", "1.0")
  {
  }

  public override Task<Analysis> Analyze(AnalysisRequest request, CancellationToken cancellationToken)
  {
    var firstData = request.Inputs.Fields["TestAnalyzerInput"];
    var analysis = new Analysis() { Result = (float)firstData.NumberValue, AnalysisOutcome = Outcome.Success };

    return Task.FromResult(analysis);
  }

  public override Task<Analysis> Analyze(AnalysisRequest request, AresStruct settings, CancellationToken cancellationToken)
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
