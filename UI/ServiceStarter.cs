using UI.Infrastructure.Repos;
using UI.Features.Devices.ChemyxPump;
using UI.Features.Devices.CM3Camera;
using UI.Features.Devices.Mfc;
using UI.Features.Devices.Remote;
using UI.Features.Devices.Servo;
using UI.Features.Devices.Shared;
using UI.Features.Devices.StepperController;
using UI.Features.Devices.SyringePump;
using UI.Features.Devices.Tc0304;
using UI.Features.Devices.TubeFurnace;
using UI.Features.Devices.ValveController;
using UI.Infrastructure.Notification;

namespace UI;

public class ServiceStarter : IHostedService
{
  private readonly INotificationReceivingService _notificationReceivingService;
  private readonly IDeviceControlViewModelRepo _deviceControlViewModelRepo;
  private readonly DeviceAdapterManager _deviceAdapterManager;
  private readonly MFCDeviceControlViewModelFactory _mfcViewModelFactory;
  private readonly ServoDeviceControlViewModelFactory _servoViewModelFactory;
  private readonly StepperControllerDeviceControlViewModelFactory _stepperControllerViewModelFactory;
  private readonly SyringePumpDeviceControlViewModelFactory _syringePumpViewModelFactory;
  private readonly Tc0304DeviceControlViewModelFactory _tc0304ViewModelFactory;
  private readonly TubeFurnaceDeviceControlViewModelFactory _tubeFurnaceViewModelFactory;
  private readonly ValveControllerDeviceControlViewModelFactory _valveControllerViewModelFactory;
  private readonly CM3CamDeviceControlViewModelFactory _cm3CameraViewModelFactory;
  private readonly RemoteDeviceControlViewModelFactory _remoteDeviceViewModelFactory;
  private readonly ChemyxPumpControlViewModelFactory _chemyxPumpViewModelFactory;

  public ServiceStarter(
    INotificationReceivingService notificationReceivingService,
    IServiceProvider serviceProvider,
    IDeviceControlViewModelRepo deviceControlViewModelRepo,
    DeviceAdapterManager deviceAdapterManager,
    MFCDeviceControlViewModelFactory mfcViewModelFactory, 
    ServoDeviceControlViewModelFactory servoViewModelFactory,
    StepperControllerDeviceControlViewModelFactory stepperControllerViewModelFactory,
    SyringePumpDeviceControlViewModelFactory syringePumpViewModelFactory,
    Tc0304DeviceControlViewModelFactory tc0304ViewModelFactory,
    TubeFurnaceDeviceControlViewModelFactory tubeFurnaceViewModelFactory,
    ValveControllerDeviceControlViewModelFactory valveControllerViewModelFactory,
    CM3CamDeviceControlViewModelFactory cm3CameraViewModelFactory,
    RemoteDeviceControlViewModelFactory remoteDeviceViewModelFactory,
    ChemyxPumpControlViewModelFactory chemyxPumpViewModelFactory)
  {
    _notificationReceivingService = notificationReceivingService;
    _deviceControlViewModelRepo = deviceControlViewModelRepo;
    _deviceAdapterManager = deviceAdapterManager;
    _mfcViewModelFactory = mfcViewModelFactory;
    _servoViewModelFactory = servoViewModelFactory;
    _stepperControllerViewModelFactory = stepperControllerViewModelFactory;
    _syringePumpViewModelFactory = syringePumpViewModelFactory;
    _tc0304ViewModelFactory = tc0304ViewModelFactory;
    _tubeFurnaceViewModelFactory = tubeFurnaceViewModelFactory;
    _valveControllerViewModelFactory = valveControllerViewModelFactory;
    _cm3CameraViewModelFactory = cm3CameraViewModelFactory;
    _remoteDeviceViewModelFactory = remoteDeviceViewModelFactory;
    _chemyxPumpViewModelFactory = chemyxPumpViewModelFactory;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    _notificationReceivingService.StartNotificationStream();
    _deviceControlViewModelRepo.Initialize();
    _deviceAdapterManager.Activate();
    _mfcViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _servoViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _stepperControllerViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _syringePumpViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _tc0304ViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _tubeFurnaceViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _valveControllerViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _cm3CameraViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _remoteDeviceViewModelFactory.Start(TimeSpan.FromSeconds(5));
    _chemyxPumpViewModelFactory.Start(TimeSpan.FromSeconds(5));
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    _deviceControlViewModelRepo.Dispose();
    await _deviceAdapterManager.DisposeAsync();
    await _mfcViewModelFactory.DisposeAsync();
    await _servoViewModelFactory.DisposeAsync();
    await _stepperControllerViewModelFactory.DisposeAsync();
    await _syringePumpViewModelFactory.DisposeAsync();
    await _tc0304ViewModelFactory.DisposeAsync();
    await _tubeFurnaceViewModelFactory.DisposeAsync();
    await _valveControllerViewModelFactory.DisposeAsync();
    await _cm3CameraViewModelFactory.DisposeAsync();
    await _remoteDeviceViewModelFactory.DisposeAsync();
    await _chemyxPumpViewModelFactory.DisposeAsync();
  }
}