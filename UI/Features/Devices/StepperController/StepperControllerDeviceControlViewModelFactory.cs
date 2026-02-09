using Ares.Services.Device;
using DynamicData;
using Google.Protobuf.WellKnownTypes;
using TicStepperController.Messaging;
using UI.Backend.Factories;
using UI.Infrastructure.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.StepperController;

namespace UI.Features.Devices.StepperController;

public class StepperControllerDeviceControlViewModelFactory : DeviceConnectorViewModelFactory<StepperControllerViewModel>
{
  private readonly StepperControllerRpc.StepperControllerRpcClient _client;
  private IDeviceControlViewModelRepo _deviceControlViewModelRepo;

  public StepperControllerDeviceControlViewModelFactory(StepperControllerRpc.StepperControllerRpcClient client,
    AresDevices.AresDevicesClient devicesClient,
    IDeviceControlViewModelRepo deviceControlViewModelRepo) : base(devicesClient, deviceControlViewModelRepo)
  {
    _client = client;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
  }

  protected override void CreateAndAddViewModel(string deviceId, string deviceName)
    => _deviceControlViewModelRepo.Add(new StepperControllerViewModel(deviceId, deviceName, _client));
  

  protected override async Task<IEnumerable<AresDeviceDescription>> GetAvailableDevices()
  {
    var stepperInfo = await _client.GetAllControllersAsync(new Empty());
    var steppers = stepperInfo.TicControllers.Select(s => new AresDeviceDescription(s.Id, s.Name)).ToArray();
    return steppers;
  }
}
