using UI.Backend.Devices;
using UI.Backend.Factories;
using UI.Backend.Repos;
using UI.Services.Notification;

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
    CM3CamDeviceControlViewModelFactory cm3CameraViewModelFactory)
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
  }

  public Task StartAsync(CancellationToken cancellationToken)
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
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}