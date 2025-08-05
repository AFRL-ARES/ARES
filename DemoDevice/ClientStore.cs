using Grpc.Net.Client;

namespace DemoDevice;

internal static class ClientStore
{
  public static DemoDeviceGrpc.DemoDeviceGrpcClient? DemoDeviceClient { get; private set; }

  public static void CreateClient(Uri address)
  {
    var channel = GrpcChannel.ForAddress(address);
    DemoDeviceClient = new DemoDeviceGrpc.DemoDeviceGrpcClient(channel);
  }
}
