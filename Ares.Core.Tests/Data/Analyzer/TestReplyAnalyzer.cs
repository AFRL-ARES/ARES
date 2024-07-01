using Ares.Core.Analyzing;
using Ares.Messaging;
using Ares.Test;

namespace Ares.Core.Tests.Data.Analyzer;

public class TestReplyAnalyzer : AnalyzerBase<TestReply>
{
  public TestReplyAnalyzer() : base("Test Analyzer", new Version(1, 0))
  {
  }

  protected override Task<Analysis> AnalyzeMessage(TestReply input, CancellationToken cancellationToken)
  {
    var analysis = new Analysis
    {
      Analyzer = new AnalyzerInfo
      {
        Name = Name,
        Type = nameof(TestReplyAnalyzer),
        UniqueId = Guid.NewGuid().ToString(),
        Version = Version.ToString()
      },
      Result = input.Number,
      UniqueId = Guid.NewGuid().ToString()
    };

    return Task.FromResult(analysis);
  }
}
