using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.ViewModels.Devices.Remote;

public class RemoteDeviceDirectorViewModel(AresDevices.AresDevicesClient devicesClient)
  : SerialDeviceConnectorViewModel<RemoteDeviceUnitViewModel>(devicesClient)
{
  protected override RemoteDeviceUnitViewModel CreateUnitVm(AresDeviceDescription description)
    => new(description.Name, description.Id, DevicesClient);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await DevicesClient.ListRemoteAresDevicesAsync(new Empty());
    var descriptions = devicesResponse.Devices.Select(d =>  new AresDeviceDescription(d.UniqueId, d.Name));
    return descriptions.ToArray();
  }
}
