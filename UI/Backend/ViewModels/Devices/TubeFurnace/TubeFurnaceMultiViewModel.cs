using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;
using TubeFurnace.Messaging;

namespace UI.Backend.ViewModels.TubeFurnace
{
  public class TubeFurnaceMultiViewModel : SerialDeviceConnectorViewModel<TubeFurnaceViewModel>
  {
    private readonly TubeFurnaceRpc.TubeFurnaceRpcClient _tubeFurnaceClient;

    public TubeFurnaceMultiViewModel(AresDevices.AresDevicesClient devicesClient, TubeFurnaceRpc.TubeFurnaceRpcClient tubeFurnaceClient) : base(devicesClient)
    {
      _tubeFurnaceClient = tubeFurnaceClient;
    }

    protected override TubeFurnaceViewModel CreateUnitVm(string deviceName)
    {
      var unitVm = new TubeFurnaceViewModel(deviceName, _tubeFurnaceClient);
      return unitVm;
    }

    protected override async Task<IEnumerable<string>> GetDeviceNames()
    {
      var devicesResponse = await _tubeFurnaceClient.GetAllTubeFurnacesAsync(new Empty());
      return devicesResponse.TubeFurnaces.Select(desc => desc.Name);
    }
  }
}
