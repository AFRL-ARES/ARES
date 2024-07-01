using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Analyzing;

/// <summary>
/// Analyzer that returns a 0 as its analysis result.
/// Used as a default analyzer in case no actual analyzers are present
/// </summary>
internal class NoneAnalyzer : AnalyzerBase<Any>
{

  public NoneAnalyzer() : base("NONE", new Version(1, 0))
  {
  }

  protected override Task<Analysis> AnalyzeMessage(Any _, CancellationToken __)
  {
    var analysis = new Analysis
    {
      UniqueId = Guid.NewGuid().ToString(),
      Analyzer = new AnalyzerInfo
      {
        Name = Name,
        UniqueId = Guid.NewGuid().ToString(),
        Version = Version.ToString()
      },
      Result = 0
    };

    return Task.FromResult(analysis);
  }

  public override bool InputSupported(string fullTypeName)
    => true;
}
