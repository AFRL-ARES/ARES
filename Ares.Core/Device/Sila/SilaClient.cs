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
    var executionManager = new DiscoveryExecutionManager();
    ServerFinder = new ServerDiscovery(new ServerConnector(executionManager));
    ExecutionManager = executionManager;
  }

  public IEnumerable<ServerData> DiscoverServers()
  {
    if(ServerFinder is null)
      return Array.Empty<ServerData>();

    return ServerFinder.GetServers(TimeSpan.FromSeconds(5));
  }

  private ServerDiscovery? ServerFinder { get; set; }

  public DiscoveryExecutionManager? ExecutionManager { get; set; }  
}
