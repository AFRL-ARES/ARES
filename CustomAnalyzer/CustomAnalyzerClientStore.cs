using Grpc.Net.Client;

namespace CustomAnalyzer;
internal static class CustomAnalyzerClientStore
{
  public static Analyzer.AnalyzerClient? CustomAnalyzerPlanningClient { get; private set; }

  public static void CreateClient(Uri address)
  {
    var channel = GrpcChannel.ForAddress(address);
    CustomAnalyzerPlanningClient = new Analyzer.AnalyzerClient(channel);
  }
}
