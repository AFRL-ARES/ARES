using Ares.Device;
using Google.Protobuf.WellKnownTypes;
using System.Diagnostics;
using Ares.Datamodel.Device;

namespace DemoDevice;

public class AresDemoDevice : AresDevice
{
  private readonly Uri _address;

  public AresDemoDevice(Uri address) : base("Demo Device")
  {
    _address = address;
  }

  public override Task<bool> Activate()
  {
    try
    {
      ClientStore.CreateClient(_address);
      Status = new DeviceOperationalStatus
      {
        OperationalState = OperationalState.Active,
        Message = $"Activated: {Name}"
      };
    }
    catch(Exception e)
    {
      Debug.WriteLine(e);
      return Task.FromResult(false);
    }

    return Task.FromResult(true);
  }

  public override async Task EnterSafeMode()
  {
    await SetTemperature(0);
  }

  public Task SetTemperature(double temperature)
  {
    return ClientStore.DemoDeviceClient?.SetTemperatureAsync(new Temperature { Value = temperature }).ResponseAsync ?? Task.CompletedTask;
  }

  public async Task<Temperature> GetTemperature()
  {
    if(ClientStore.DemoDeviceClient is null)
      return new Temperature { Value = -1 };

    var response = await ClientStore.DemoDeviceClient.GetTemperatureAsync(new Empty());
    return response;
  }

  public async Task<GrowthResponse> GetGrowth()
  {
    if(ClientStore.DemoDeviceClient is null)
      return new GrowthResponse { Growth = -1 };

    var response = await ClientStore.DemoDeviceClient.GetCurrentGrowthAsync(new Empty());
    return response;
  }

  public async Task<CurrentPillarResponse> GetCurrentPillar()
  {
    if(ClientStore.DemoDeviceClient is null)
      return new CurrentPillarResponse { Pillar = -1 };

    var response = await ClientStore.DemoDeviceClient.GetCurrentPillarAsync(new Empty());
    return response;
  }

  public async Task<CurrentPillarResponse> MoveToNextPillar()
  {
    if(ClientStore.DemoDeviceClient is null)
      return new CurrentPillarResponse { Pillar = -1 };

    var response = await ClientStore.DemoDeviceClient.MoveToNextPillarAsync(new Empty());
    return response;
  }
}
