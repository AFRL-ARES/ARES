using Ares.Core.Device;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using TicStepperController;
using TicStepperController.Config;
using TicStepperController.Messaging;
using TicStepperController.Proto.Extensions;

namespace AresService.Services.Devices;

public class StepperControllerService : StepperControllerRpc.StepperControllerRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<StepperControllerConfig, IStepperController> _stepperManager;
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IDeviceConfigManager<StepperControllerConfig> _configManager;

  public StepperControllerService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<StepperControllerConfig, IStepperController> stepperManager,
    IDbContextFactory<AresDbContext> dbContextFactory,
    IDeviceConfigManager<StepperControllerConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _stepperManager = stepperManager;
    _dbContextFactory = dbContextFactory;
    _configManager = configManager;
  }

  public override async Task<Empty> AddStepperController(StepperControllerConfig request, ServerCallContext context)
  {
    await _stepperManager.Load(request);
    var device = GetStepperController(request.Name);
    request = await FillConfig(device, request);
    await _configManager.Add(request.Name, request);

    return new Empty();
  }

  /// <summary>
  /// Fills the config automatically based on the values we get directly from the step controller.
  /// That way the user does not have to manually look up the default/current values when
  /// first hooking up the controller
  /// </summary>
  /// <param name="controller"></param>
  /// <param name="config"></param>
  /// <returns></returns>
  private static async Task<StepperControllerConfig> FillConfig(IStepperController controller, StepperControllerConfig config)
  {
    var state = await controller.StateStream.Where(s => s.Valid).Take(1);
    var newConfig = config.Clone();
    newConfig.MaxAcceleration = state.MaxAcceleration;
    newConfig.MaxDeceleration = state.MaxDeceleration;
    newConfig.MaxSpeed = state.MaxSpeed;
    newConfig.StartingSpeed = state.StartingSpeed;
    newConfig.StepMode = state.StepMode;

    return newConfig;
  }

  public override async Task<Empty> DeEnergize(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.DeEnergize();

    return new Empty();
  }

  public override async Task<Empty> Energize(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.Energize();

    return new Empty();
  }

  public override async Task<Empty> EnterSafeStart(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.EnterSafeStart();

    return new Empty();
  }

  public override async Task<Empty> ExitSafeStart(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.ExitSafeStart();

    return new Empty();
  }

  public override async Task<TicState> GetState(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    var currentState = await controller.StateStream.Where(state => state.Valid).Take(1);

    return currentState;
  }

  public override async Task<Empty> HaltAndHold(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.HaltAndHold();

    return new Empty();
  }

  public override async Task<Empty> HaltAndSetPosition(PositionCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.HaltAndSetPosition(request.Position);

    return new Empty();
  }

  public override async Task<Empty> NextStep(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.NextStep();

    return new Empty();
  }

  public override async Task<Empty> PreviousStep(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.PreviousStep();

    return new Empty();
  }

  public override async Task<Empty> HalfStep(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.HalfStep();

    return new Empty();
  }

  public override async Task<Empty> RemoveStepperController(TicRequest request, ServerCallContext context)
  {
    await _stepperManager.Remove(request.TicName);
    await _configManager.Remove(request.TicName);

    return new Empty();
  }

  public override async Task<Empty> Reset(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.Reset();

    return new Empty();
  }

  public override async Task<Empty> ResetCommandTimeout(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.ResetCommandTimeout();

    return new Empty();
  }

  public override Task<Empty> SetCustomStepSize(UnsignedCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    controller.UserStepSize = request.Argument;

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> SetMaxAcceleration(AccelerationCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.SetMaxAcceleration(request.Acceleration);

    return new Empty();
  }

  public override async Task<Empty> SetCurrentLimit(CurrentLimitCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.SetCurrentLimit(request.Limit);

    return new Empty();
  }

  public override async Task<Empty> SetMaxDeceleration(AccelerationCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.SetMaxDeceleration(request.Acceleration);

    return new Empty();
  }

  public override async Task<Empty> SetMaxSpeed(UnsignedCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.SetMaxSpeed(request.Argument);

    return new Empty();
  }

  public override async Task<Empty> SetStartingSpeed(UnsignedCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.SetStartingSpeed(request.Argument);

    return new Empty();
  }

  public override async Task<Empty> SetStepMode(StepModeCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.SetStepMode(request.StepMode.ToInternal());

    return new Empty();
  }

  public override async Task<Empty> SetTargetPosition(PositionCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicName);
    await controller.SetTargetPosition(request.Position);

    return new Empty();
  }

  public override async Task<Empty> UpdateStepperController(StepperControllerConfig request, ServerCallContext context)
  {
    await _stepperManager.Update(request);
    await _configManager.Update(request.Name, request);

    return new Empty();
  }

  public override Task<GetAllControllersResponse> GetAllControllers(Empty request, ServerCallContext context)
  {
    var devices = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IStepperController>()
      .Select(controller => controller.Name);

    var response = new GetAllControllersResponse();
    response.TicControllers.AddRange(devices);

    return Task.FromResult(response);
  }

  private IStepperController GetStepperController(string name)
  {
    var controller = _deviceCommandInterpreterRepo
      .Select(dci => dci.Device)
      .OfType<IStepperController>()
      .FirstOrDefault(sc => sc.Name == name);

    if(controller is null)
      throw new InvalidOperationException($"Could not find TIC Stepper Controller ({name})");

    return controller;
  }
}
