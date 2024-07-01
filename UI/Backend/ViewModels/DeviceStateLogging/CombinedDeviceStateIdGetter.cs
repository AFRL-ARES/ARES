using Ares.Messages.DeviceStates.Mfc;
using Ares.Messages.DeviceStates.SyringePump;
using Ares.Messages.DeviceStates.Tc0304;
using Ares.Messages.DeviceStates.TicStepperController;
using Ares.Messages.DeviceStates.TubeFurnace;
using Google.Protobuf.WellKnownTypes;

namespace UI.Backend.ViewModels.DeviceStateLogging;

public class CombinedDeviceStateIdGetter : ICombinedDeviceStateIdGetter
{
  private readonly MfcStateLogging.MfcStateLoggingClient _mfcClient;
  private readonly TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient _ticClient;
  private readonly TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient _tubeClient;
  private readonly SyringePumpStateLogging.SyringePumpStateLoggingClient _syringeClient;
  private readonly Tc0304StateLogging.Tc0304StateLoggingClient _tcClient;

  public CombinedDeviceStateIdGetter(
      MfcStateLogging.MfcStateLoggingClient mfcClient,
      TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient ticClient,
      TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient tubeClient,
      SyringePumpStateLogging.SyringePumpStateLoggingClient syringeClient,
      Tc0304StateLogging.Tc0304StateLoggingClient tcClient)

  {
    _mfcClient = mfcClient;
    _ticClient = ticClient;
    _tubeClient = tubeClient;
    _syringeClient = syringeClient;
    _tcClient = tcClient;
  }

  public async Task<IEnumerable<string>> GetAvailableIds()
  {
    var mfcResponses = await _mfcClient.GetAvailableDevicesAsync(new Empty());
    var ticResponses = await _ticClient.GetAvailableDevicesAsync(new Empty());
    var tubeResponses = await _tubeClient.GetAvailableDevicesAsync(new Empty());
    var syringeResponses = await _syringeClient.GetAvailableDevicesAsync(new Empty());
    var tcResponses = await _tcClient.GetAvailableDevicesAsync(new Empty());

    var deviceIds = new List<string>();
    deviceIds.AddRange(mfcResponses.DeviceIds);
    deviceIds.AddRange(ticResponses.DeviceIds);
    deviceIds.AddRange(tubeResponses.DeviceIds);
    deviceIds.AddRange(syringeResponses.DeviceIds);
    deviceIds.AddRange(tcResponses.DeviceIds);

    return deviceIds;
  }
}
