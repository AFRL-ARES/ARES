using Ares.Core.Device.Repos;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Execution.Safety;

public class ExecutionSafetyManager : IExecutionSafetyManager
{
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly ILogger<ExecutionSafetyManager> _logger;
  private readonly IExecutionManager _executionManager;
  
  public ExecutionSafetyManager(IAresDeviceRepo deviceRepo,
    IExecutionManager executionManager,
    ILogger<ExecutionSafetyManager> logger)
  {
    _deviceRepo = deviceRepo;
    _executionManager = executionManager;
    _logger = logger;
  }

  public async Task<bool> EnterSafeMode()
  {
    try
    {
      List<Task> deviceSafeModeTasks = [];

      foreach(var device in _deviceRepo)
        deviceSafeModeTasks.Add(device.EnterSafeMode());

      _executionManager.Stop();
      await Task.WhenAll(deviceSafeModeTasks);
      _logger.LogCritical("Successfully Put All Devices Into Safe Mode!");
      return true;
    }

    catch(Exception ex)
    {
      _logger.LogCritical("FAILED TO ENTER SAFE MODE! REASON: {message}", ex.Message);
      return false;
    }
  }
}
