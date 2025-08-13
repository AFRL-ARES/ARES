using Ares.Datamodel.Analyzing;

namespace Ares.Core.Analyzing;

public static class AnalyzerExtensions
{
  public static async Task<AnalyzerInfo> CreateAnalyzerInfo(this IAnalyzer analyzer) =>
    new AnalyzerInfo
    {
      Capabilities = await analyzer.GetCapabilities(),
      Description = analyzer.Description,
      Name = analyzer.Name,
      Type = analyzer.Type,
      UniqueId = analyzer.UniqueId,
      Url = analyzer is RemoteAnalyzer remoteAnalyzer ? remoteAnalyzer.Address.ToString() : string.Empty,
      Version = analyzer.Version
    };
}
