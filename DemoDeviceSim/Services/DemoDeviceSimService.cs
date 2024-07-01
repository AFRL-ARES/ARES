using DemoDevice;
using DemoDeviceSim;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoDeviceSim.Services;

public class DemoDeviceSimService : DemoDeviceGrpc.DemoDeviceGrpcBase
{
  private readonly PillarRobot _pillarRobot;
  public DemoDeviceSimService()
  {
    _pillarRobot = RobotStore.PillarRobot;
  }

  public override Task<Empty> SetTemperature(Temperature request, ServerCallContext context)
  {
    _ = _pillarRobot.SetTemperature(request.Value);
    return Task.FromResult(new Empty());
  }

  public override Task<Temperature> GetTemperature(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new Temperature { Value = _pillarRobot.CurrentPillarTemp });
  }

  public override Task<GrowthResponse> GetCurrentGrowth(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new GrowthResponse { Growth = _pillarRobot.CurrentPillarSize });
  }

  public override Task<CurrentPillarResponse> GetCurrentPillar(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new CurrentPillarResponse { Pillar = _pillarRobot.CurrentPillarIndex });
  }

  public override Task<CurrentPillarResponse> MoveToNextPillar(Empty request, ServerCallContext context)
  {
    _pillarRobot.NextPillar();
    return Task.FromResult(new CurrentPillarResponse { Pillar = _pillarRobot.CurrentPillarIndex });
  }
}
