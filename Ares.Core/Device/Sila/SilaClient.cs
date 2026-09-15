using Tecan.Sila2;
using Tecan.Sila2.Discovery;

namespace Ares.Core.Device.Sila;

public class SilaClient
{
  public SilaClient()
  {

  }

  public void Init()
  {
    ExecutionManager = new DiscoveryExecutionManager();
    Connector = new ServerConnector(ExecutionManager);
    ServerFinder = new ServerDiscovery(Connector);
  }

  public IEnumerable<ServerData> DiscoverServers()
  {
    if(ServerFinder is null)
      return [];

    return ServerFinder.GetServers(TimeSpan.FromSeconds(5));
  }

  public ServerData? TryConnectToServer(string address, int port)
  {
    try
    {
      var server = Connector.Connect(address, port);
      return server;
    }

    catch(Exception)
    {
      return null;
    }
  }

  private ServerDiscovery? ServerFinder { get; set; }

  public DiscoveryExecutionManager? ExecutionManager { get; set; }
  
  public ServerConnector Connector { get; set; }
}
