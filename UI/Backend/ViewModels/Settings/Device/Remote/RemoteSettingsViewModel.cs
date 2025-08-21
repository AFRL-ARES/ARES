using Ares.Datamodel.Device;
using Ares.Services.Device;
using Grpc.Core;

namespace UI.Backend.ViewModels.Settings.Device.Remote;

public class RemoteSettingsViewModel(RemoteDeviceConfig _deviceConfig, AresDevices.AresDevicesClient _devicesClient, Func<Task> _onRemoveCallback)
{
  public async Task<DeviceOperationalStatus> GetDeviceOperationalStatus()
  {
    try
    {
      var status = await _devicesClient.GetDeviceStatusAsync(new DeviceStatusRequest { DeviceId = _deviceConfig.UniqueId }).ResponseAsync;
      //DeviceActive = status.OperationalState is OperationalState.Active;
      return status;
    }
    catch(RpcException)
    {
      return new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Unable to find a registered mfc with a name {1}" };
    }
  }
}
