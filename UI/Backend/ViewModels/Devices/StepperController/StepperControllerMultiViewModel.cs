using Ares.Services.Device;
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

  protected override StepperControllerViewModel CreateUnitVm(AresDeviceDescription description)
    => new(description.Id, description.Name, _client);

  protected override async Task<AresDeviceDescription[]> GetDeviceDescriptions()
  {
    var devicesResponse = await _client.GetAllControllersAsync(new Empty());
    var descriptions = devicesResponse.TicControllers.Select(tc => new AresDeviceDescription(tc.Id, tc.Name)).ToArray();
    return descriptions;
  }
}
