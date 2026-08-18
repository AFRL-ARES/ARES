using Ares.Core.Device.Repos;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Execution.Safety;

public class ExecutionSafetyManager : IExecutionSafetyManager
{
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly ILogger<ExecutionSafetyManager> _logger;
  
  public ExecutionSafetyManager(IAresDeviceRepo deviceRepo, ILogger<ExecutionSafetyManager> logger)
  {
    _deviceRepo = deviceRepo;
    _logger = logger;
  }

  public async Task<bool> EnterSafeMode()
  {
    try
    {
      foreach(var device in _deviceRepo)
      {
        await device.EnterSafeMode();
      }

      return true;
    }

    catch(Exception ex)
    {
      _logger.LogError($"FAILED TO ENTER SAFE MODE! REASON: {ex.Message}");
      return false;
    }
  }
}
