using DemoDevice;
using DemoDeviceSim;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace RepeaterDeviceTest.Services;

public class DemoDeviceSimService : DemoDeviceGrpc.DemoDeviceGrpcBase
{
  private readonly PillarRobot _pillarRobot;
  public DemoDeviceSimService()
  {
    _pillarRobot = RobotStore.PillarRobot;
  }

  public override async Task<Empty> SetTemperature(Temperature request, ServerCallContext context)
  {
    await _pillarRobot.SetTemperature(request.Value);
    return new Empty();
  }

  public override Task<Temperature> GetTemperature(Empty request, ServerCallContext context)
  {
    Console.WriteLine("Current Temperature Retrieved");
    return Task.FromResult(new Temperature { Value = _pillarRobot.CurrentPillarTemp });
  }

  public override async Task<CurrentPillarResponse> MoveToNextPillar(Empty request, ServerCallContext context)
  {
    await _pillarRobot.NextPillar();
    return new CurrentPillarResponse { Pillar = _pillarRobot.CurrentPillarIndex };
  }

  public override Task<GrowthResponse> GetCurrentGrowth(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new GrowthResponse { Growth = _pillarRobot.CurrentPillarSize });
  }

  public override Task<CurrentPillarResponse> GetCurrentPillar(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new CurrentPillarResponse { Pillar = _pillarRobot.CurrentPillarIndex });
  }
}
