using Ares.Core.Device.Repos;
using Ares.Core.Execution;
using Ares.Services;
using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace Ares.Core.Grpc.Services.Safety;

public class AresSafetyManagementService : AresSafetyService.AresSafetyServiceBase
{
  private readonly IExecutionManager _executionManager;
  private readonly IAresDeviceRepo _deviceRepo;

  public AresSafetyManagementService(IExecutionManager executionManager, IAresDeviceRepo deviceRepo)
  {
    _executionManager = executionManager;
    _deviceRepo = deviceRepo;
  }

  public override Task<EmergencyStopResponse> RequestEmergencyStop(EmergencyStopRequest request, ServerCallContext context)
  {
    EmergencyStopResponse response;

    try
    {
      //Stop current campaign execution
      _executionManager.Stop();

      foreach(var device in _deviceRepo)
      {
        device.EnterSafeMode();
      }

      response = new EmergencyStopResponse()
      {
        Status = EmergencyStopStatus.Success,
        StatusMessage = "Emergency stop successfully processed. Any running campaigns have been paused, and all devices have entered safe mode."
      };
    }

    catch(Exception e)
    {
      response = new EmergencyStopResponse()
      {
        Status = EmergencyStopStatus.Error,
        StatusMessage = e.Message
      };
    }

    return Task.FromResult(response);
  }
}
