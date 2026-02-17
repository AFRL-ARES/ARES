using Ares.Core.Device.Repos;

namespace Ares.Core.Execution.Safety;

public class ExecutionSafetyManager : IExecutionSafetyManager
{
  private readonly IAresDeviceRepo _deviceRepo;
  
  public ExecutionSafetyManager(IAresDeviceRepo deviceRepo)
  {
    _deviceRepo = deviceRepo;
  }

  public async Task EnterSafeMode()
  {
    foreach(var device in _deviceRepo)
    {
      await device.EnterSafeMode();
    }
  }
}
