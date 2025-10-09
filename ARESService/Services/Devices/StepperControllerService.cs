using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.Data;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
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
    var device = await _stepperManager.Create(request);
    request = await FillConfig(device, request);
    await _configManager.Add(device.UniqueId, device.Name, request);

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
    var controller = GetStepperController(request.TicId);
    await controller.DeEnergize();

    return new Empty();
  }

  public override async Task<Empty> Energize(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.Energize();

    return new Empty();
  }

  public override async Task<Empty> EnterSafeStart(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.EnterSafeStart();

    return new Empty();
  }

  public override async Task<Empty> ExitSafeStart(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.ExitSafeStart();

    return new Empty();
  }

  public override async Task<TicState> GetState(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    var currentState = await controller.StateStream.Where(state => state.Valid).Take(1);

    return currentState;
  }

  public override async Task<Empty> HaltAndHold(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.HaltAndHold();

    return new Empty();
  }

  public override async Task<Empty> HaltAndSetPosition(PositionCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.HaltAndSetPosition(request.Position);

    return new Empty();
  }

  public override async Task<Empty> NextStep(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.NextStep();

    return new Empty();
  }

  public override async Task<Empty> PreviousStep(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.PreviousStep();

    return new Empty();
  }

  public override async Task<Empty> HalfStep(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.HalfStep();

    return new Empty();
  }

  public override async Task<Empty> RemoveStepperController(TicRequest request, ServerCallContext context)
  {
    await _stepperManager.Remove(request.TicId);
    await _configManager.Remove(request.TicId);

    return new Empty();
  }

  public override async Task<Empty> Reset(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.Reset();

    return new Empty();
  }

  public override async Task<Empty> ResetCommandTimeout(TicRequest request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.ResetCommandTimeout();

    return new Empty();
  }

  public override Task<Empty> SetCustomStepSize(UnsignedCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    controller.UserStepSize = request.Argument;

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> SetMaxAcceleration(AccelerationCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.SetMaxAcceleration(request.Acceleration);

    return new Empty();
  }

  public override async Task<Empty> SetCurrentLimit(CurrentLimitCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.SetCurrentLimit(request.Limit);

    return new Empty();
  }

  public override async Task<Empty> SetMaxDeceleration(AccelerationCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.SetMaxDeceleration(request.Acceleration);

    return new Empty();
  }

  public override async Task<Empty> SetMaxSpeed(UnsignedCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.SetMaxSpeed(request.Argument);

    return new Empty();
  }

  public override async Task<Empty> SetStartingSpeed(UnsignedCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.SetStartingSpeed(request.Argument);

    return new Empty();
  }

  public override async Task<Empty> SetStepMode(StepModeCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.SetStepMode(request.StepMode.ToInternal());

    return new Empty();
  }

  public override async Task<Empty> SetTargetPosition(PositionCommand request, ServerCallContext context)
  {
    var controller = GetStepperController(request.TicId);
    await controller.SetTargetPosition(request.Position);

    return new Empty();
  }

  public override async Task<Empty> UpdateStepperController(StepperControllerUpdateRequest request, ServerCallContext context)
  {
    await _stepperManager.Update(request.Id, request.Config);
    await _configManager.Update(request.Id, request.Config);

    return new Empty();
  }

  public override Task<GetAllControllersResponse> GetAllControllers(Empty request, ServerCallContext context)
  {
    var devices = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IStepperController>()
      .Select(controller => new DeviceDescription { Id = controller.UniqueId, Name = controller.Name });

    var response = new GetAllControllersResponse();
    response.TicControllers.AddRange(devices);

    return Task.FromResult(response);
  }

  private IStepperController GetStepperController(string id)
  {
    var controller = _deviceCommandInterpreterRepo
      .Select(dci => dci.Device)
      .OfType<IStepperController>()
      .FirstOrDefault(sc => sc.UniqueId == id);

    if(controller is null)
      throw new InvalidOperationException($"Could not find TIC Stepper Controller ({id})");

    return controller;
  }
}
