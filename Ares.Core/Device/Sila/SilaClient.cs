using Grpc.Core;
using Grpc.Net.Client;
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

  public bool TestServerConnection(string address, int port)
  {
    try
    {
      var fetchedData = Connector.Connect(address, port);

      if(fetchedData is null)
        return false;

      return true;
    }

    catch(Exception)
    {
      return false;
    }
  }

  public async Task<ServerData?> TryConnectToServer(string address, int port)
  {
    try
    {
      var task = Task.Run(() => Connector.Connect(address, port));
      var delayTask = Task.Delay(TimeSpan.FromSeconds(5));

      var completedTask = await Task.WhenAny(task, delayTask);

      if(completedTask == delayTask)
        throw new TimeoutException("The synchronous operation timed out.");

      // Retrieve the result (will rethrow any exceptions from the method)
      var result = await task;
      return result;
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
