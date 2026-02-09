using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using TubeFurnace.Messaging;
using UI.Backend.Factories;
using UI.Infrastructure.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.TubeFurnace;

namespace UI.Features.Devices.TubeFurnace
{
  public class TubeFurnaceDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<TubeFurnaceViewModel>
  {
    private readonly TubeFurnaceRpc.TubeFurnaceRpcClient _tubeFurnaceClient;
    private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;

    public TubeFurnaceDeviceControlViewModelFactory(AresDevices.AresDevicesClient devicesClient,
      TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient,
      IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
    {
      _tubeFurnaceClient = tubeFurnaceClient;
      _deviceControlViewModelRepo = deviceControlViewModelRepo;
    }

    protected override void CreateAndAddViewModel(string deviceId, string deviceName)
      => _deviceControlViewModelRepo.Add(new TubeFurnaceViewModel(deviceId, deviceName, _tubeFurnaceClient));

    protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
    {
      var devicesResponse = await _tubeFurnaceClient.GetAllTubeFurnacesAsync(new Empty());
      var furnaces = devicesResponse.TubeFurnaces.Select(desc => new AresDeviceDescription(desc.Id, desc.Name)).ToArray();
      return furnaces;
    }
  }
}
