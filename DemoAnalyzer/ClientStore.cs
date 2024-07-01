using Grpc.Net.Client;

namespace DemoAnalyzer;

internal static class ClientStore
{
  public static DemoAnalyzerGrpc.DemoAnalyzerGrpcClient? DemoPlanningClient { get; private set; }

  public static void CreateClient(Uri address)
  {
    var channel = GrpcChannel.ForAddress(address);
    DemoPlanningClient = new DemoAnalyzerGrpc.DemoAnalyzerGrpcClient(channel);
  }
}
