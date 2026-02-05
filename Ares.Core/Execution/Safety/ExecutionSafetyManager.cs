using Ares.Core.Device.Repos;

namespace Ares.Core.Execution.Safety;

public class ExecutionSafetyManager : IExecutionSafetyManager
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  
  public ExecutionSafetyManager(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public async Task EnterSafeMode()
  {
    foreach(var device in _deviceCommandInterpreterRepo.GetAresDevices())
    {
      await device.EnterSafeMode();
    }
  }
}
