using Ares.Datamodel.Planning;
using Grpc.Net.Client;

namespace Ares.Core.Planning.AresPlanner
{
  public static class ClientStore
  {
    public static AresPlannerGrpc.AresPlannerGrpcClient? AresPlanningClient { get; private set; }

    public static void CreateClient(Uri address)
    {
      var channel = GrpcChannel.ForAddress(address);
      AresPlanningClient = new AresPlannerGrpc.AresPlannerGrpcClient(channel);
    }
  }
}
