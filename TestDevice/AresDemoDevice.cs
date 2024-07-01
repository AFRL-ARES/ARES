using System.Diagnostics;
using Ares.Device;
using Google.Protobuf.WellKnownTypes;

namespace DemoDevice;

public class AresDemoDevice : AresDevice
{
  private readonly Uri _address;

  public AresDemoDevice(string name, Uri address) : base(name)
  {
    _address = address;
  }

  public override Task<bool> Activate()
  {
    try
    {
      ClientStore.CreateClient(_address);
      Status = new Ares.Messaging.Device.DeviceStatus
      {
        DeviceState = Ares.Messaging.Device.DeviceState.Active,
        Message = $"Activated: {Name}"
      };
    }
    catch (Exception e)
    {
      Debug.WriteLine(e);
      return Task.FromResult(false);
    }

    return Task.FromResult(true);
  }

  public Task SetTemperature(double temperature)
  {
    return ClientStore.DemoDeviceClient?.SetTemperatureAsync(new Temperature { Value = temperature }).ResponseAsync ?? Task.CompletedTask;
  }

  public async Task<Temperature> GetTemperature()
  {
    var response = await ClientStore.DemoDeviceClient.GetTemperatureAsync(new Empty());
    return response;
  }

  public async Task<GrowthResponse> GetGrowth()
  {
    var response = await ClientStore.DemoDeviceClient.GetCurrentGrowthAsync(new Empty());
    return response;
  }

  public async Task<CurrentPillarResponse> GetCurrentPillar()
  {
    var response = await ClientStore.DemoDeviceClient.GetCurrentPillarAsync(new Empty());
    return response;
  }

  public async Task<CurrentPillarResponse> MoveToNextPillar()
  {
    var response = await ClientStore.DemoDeviceClient.MoveToNextPillarAsync(new Empty());
    return response;
  }
}
