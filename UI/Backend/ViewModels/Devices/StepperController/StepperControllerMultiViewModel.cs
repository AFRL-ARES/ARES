using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using TicStepperController.Messaging;

namespace UI.Backend.ViewModels.StepperController;

public class StepperControllerMultiViewModel : SerialDeviceConnectorViewModel<StepperControllerViewModel>
{
  private readonly StepperControllerRpc.StepperControllerRpcClient _client;

  public StepperControllerMultiViewModel(StepperControllerRpc.StepperControllerRpcClient client, AresDevices.AresDevicesClient devicesClient)
    : base(devicesClient)
  {
    _client = client;
  }

  protected override StepperControllerViewModel CreateUnitVm(string deviceName)
    => new(deviceName, _client);

  protected override async Task<IEnumerable<string>> GetDeviceNames()
  {
    var devicesResponse = await _client.GetAllControllersAsync(new Empty());
    return devicesResponse.TicControllers;
  }
}
