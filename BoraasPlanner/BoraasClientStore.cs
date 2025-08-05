using Grpc.Net.Client;

namespace BoraasPlanner;
public static class BoraasClientStore
{
  public static BoraasPlannerGrpc.BoraasPlannerGrpcClient? BoraasPlanningClient { get; private set; }

  public static void CreateClient(Uri address)
  {
    var channel = GrpcChannel.ForAddress(address);
    BoraasPlanningClient = new BoraasPlannerGrpc.BoraasPlannerGrpcClient(channel);
  }
}
