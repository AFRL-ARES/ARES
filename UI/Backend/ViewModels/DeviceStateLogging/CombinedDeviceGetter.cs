using Ares.Datamodel.Device.Remote;
using Ares.Messages.DeviceStates;
using Ares.Messages.DeviceStates.Mfc;
using Ares.Messages.DeviceStates.SyringePump;
using Ares.Messages.DeviceStates.Tc0304;
using Ares.Messages.DeviceStates.TicStepperController;
using Ares.Messages.DeviceStates.TubeFurnace;
using Ares.Services.Device;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class CombinedDeviceGetter : ICombinedDeviceGetter
{
  private readonly MfcStateLogging.MfcStateLoggingClient _mfcClient;
  private readonly TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient _ticClient;
  private readonly TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient _tubeClient;
  private readonly SyringePumpStateLogging.SyringePumpStateLoggingClient _syringeClient;
  private readonly AresDevices.AresDevicesClient _devicesClient;
  private readonly AresRemoteDeviceService.AresRemoteDeviceServiceClient _remoteDeviceServiceClient;
  private readonly Tc0304StateLogging.Tc0304StateLoggingClient _tcClient;

  public CombinedDeviceGetter(
      MfcStateLogging.MfcStateLoggingClient mfcClient,
      TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient ticClient,
      TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient tubeClient,
      SyringePumpStateLogging.SyringePumpStateLoggingClient syringeClient,
      AresDevices.AresDevicesClient devicesClient,
      Tc0304StateLogging.Tc0304StateLoggingClient tcClient)

  {
    _mfcClient = mfcClient;
    _ticClient = ticClient;
    _tubeClient = tubeClient;
    _syringeClient = syringeClient;
    _devicesClient = devicesClient;
    _tcClient = tcClient;
  }

  public async Task<DevicesDescription[]> GetAvailableDevices()
  {
    var mfcResponses = await _mfcClient.GetAvailableDevicesAsync(new Empty());
    var ticResponses = await _ticClient.GetAvailableDevicesAsync(new Empty());
    var tubeResponses = await _tubeClient.GetAvailableDevicesAsync(new Empty());
    var syringeResponses = await _syringeClient.GetAvailableDevicesAsync(new Empty());
    var tcResponses = await _tcClient.GetAvailableDevicesAsync(new Empty());
    var remoteDevices = await _devicesClient.GetAllRemoteDevicesConfigsAsync(new Empty());
    var remoteDeviceDescriptions = remoteDevices.Configs.Select(cfg => new DevicesDescription()
      { DeviceId = cfg.UniqueId, DeviceName = cfg.Name });
    
    var devices = new List<DevicesDescription>();
    devices.AddRange(mfcResponses.Devices);
    devices.AddRange(ticResponses.Devices);
    devices.AddRange(tubeResponses.Devices);
    devices.AddRange(syringeResponses.Devices);
    devices.AddRange(tcResponses.Devices);
    devices.AddRange(remoteDeviceDescriptions);

    return devices.ToArray();
  }
}
