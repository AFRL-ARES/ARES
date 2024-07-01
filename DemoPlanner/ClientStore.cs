using Grpc.Net.Client;

namespace DemoPlanner;

internal static class ClientStore
{
  public static DemoPlannerGrpc.DemoPlannerGrpcClient? DemoPlanningClient { get; private set; }

  public static void CreateClient(Uri address)
  {
    var channel = GrpcChannel.ForAddress(address);
    DemoPlanningClient = new DemoPlannerGrpc.DemoPlannerGrpcClient(channel);
  }
}
