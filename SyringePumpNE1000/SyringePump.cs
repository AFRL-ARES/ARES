using Ares.Datamodel;
using Ares.Device;
using Ares.Device.Serial;
using Ares.SyringePump.Ne1000.Messaging;
using SyringePumpNE1000.Commands.Requests;
using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Simulation;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using UnitsNet;

namespace SyringePumpNE1000;

public class SyringePump : SerialDevice<ISyringePumpConnection>, ISyringePump
{
  private readonly ISubject<StateResponse> _statePublisher = new BehaviorSubject<StateResponse>(new StateResponse());
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private CancellationTokenSource _stateUpdaterCancellation = new CancellationTokenSource();
  private Task _stateUpdater = Task.CompletedTask;

  // < safe command protocol> => < STX > < length > < command data > < CRC 16 > < ETX >
  public SyringePump(string identifier, uint address, ISyringePumpConnection connection) : base(identifier, connection)
  {
    InternalStateStream = _statePublisher.AsObservable();
    StateStream = _stateSubject.AsObservable();
    AssumedAddress = address;
    FirmwareVersion = string.Empty;

    if(connection is SimSyringePumpConnection)
      IsSimulated = true;

    var initialState = new StateResponse { Address = (int)AssumedAddress };
    _statePublisher.OnNext(initialState);
  }

  public IObservable<StateResponse> InternalStateStream { get; }

  public override IObservable<AresStruct> StateStream { get; }
  public async Task SetPhase(int phase)
  {
    var currentState = await GetCurrentState();
    var request = new Commands.Requests.SetPhaseNumberRequest(currentState.Address, phase);
    await Connection.Send(request, TimeSpan.FromSeconds(3));
    await QueryPhase();
  }

  public async Task SetPhaseFunction(Ares.SyringePump.Ne1000.Messaging.Commands function)
  {
    var currentState = await GetCurrentState();
    var request = new Commands.Requests.SetPhaseFunctionRequest(currentState.Address, function);
    await Connection.Send(request, TimeSpan.FromSeconds(3));
    var response = await QueryPhaseFunction();
  }

  public async Task SetDiameter(Length diameter)
  {
    var currentState = await GetCurrentState();
    var request = new SetDiameterRequest(currentState.Address, diameter);
    await Connection.Send(request, TimeSpan.FromSeconds(3));
    await GetDiameter();
  }

  public async Task<Length> GetDiameter()
  {
    var currentState = await GetCurrentState();
    var request = new GetDiameterRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(6));
    if(response.Error is not null)
      return Length.Zero;

    return response.Diameter;
  }

  /// <summary>
  /// Syringe pumps appear unresponsive until they get some sort of RS232 command.
  /// So we just send a random one and expect an exception
  /// </summary>
  /// <returns></returns>
  private async Task Awaken()
  {
    var currentState = await GetCurrentState();
    var request = new GetDiameterRequest(currentState.Address);
    try
    {
      var response = await Connection.Send(request, TimeSpan.FromSeconds(1));
    }
    catch(TimeoutException)
    {
      // Ignore the exception, should be expected the first time
    }
  }

  // Note: There IS an 'I' instead of 'C' rate argument that could be used, but it doesn't sound like we will
  public async Task SetProgramFunctionRate(Speed rate)
  {
    var currentState = await GetCurrentState();
    var request = new SetPhaseFunctionRateRequest(currentState.Address, rate);

    await Connection.Send(request, TimeSpan.FromSeconds(3));
    await GetProgramFunctionRate();
  }

  public async Task<PhaseFunctionRateResponse> GetProgramFunctionRate()
  {
    var currentState = await GetCurrentState();
    var request = new GetPhaseFunctionRate(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    return response;
  }

  public async Task<PhaseFunctionResponse> QueryPhaseFunction()
  {
    var currentState = await GetCurrentState();
    var request = new QueryPhaseFunctionRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    return response;
  }

  public async Task SetProgramFunctionVolumeToBeDispensed(Volume volume)
  {
    var currentState = await GetCurrentState();
    var request = new SetPhaseFunctionVolumeRequest(currentState.Address, volume);

    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    await GetProgramFunctionVolumeToBeDispensed();
  }

  public async Task GetProgramFunctionVolumeToBeDispensed()
  {
    var currentState = await GetCurrentState();
    var request = new GetPhaseFunctionVolumeRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
  }

  public async Task SetProgramFunctionPumpingDirection(Direction direction)
  {
    var currentState = await GetCurrentState();
    var request = new SetPhaseFunctionDirectionRequest(currentState.Address, direction);

    await Connection.Send(request, TimeSpan.FromSeconds(3));
    await GetProgramFunctionPumpingDirection();
  }

  public async Task<Direction> GetProgramFunctionPumpingDirection()
  {
    var currentState = await GetCurrentState();
    var request = new GetPhaseFunctionDirectionRequest(currentState.Address);
    var result = await Connection.Send(request, TimeSpan.FromSeconds(3));
    return result.Direction;
  }

  public async Task<int> QueryPhase()
  {
    var currentState = await GetCurrentState();
    var request = new PhaseQueryRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    return response.Phase;
  }

  public async Task PurgePump()
  {
    var currentState = await GetCurrentState();
    var request = new PurgeRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    await MonitorPurge();
  }

  public async Task StartPumpingProgram()
  {
    var currentState = await GetCurrentState();

    //If we're already pumping, don't send the start command again
    if(currentState.Status == StatusPrompt.PromptI || currentState.Status == StatusPrompt.PromptW)
      return;

    var request = new StartRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
  }

  public async Task StopPumpingProgram()
  {
    var currentState = await GetCurrentState();
    var status = currentState.Status;

    //The syringe pump seems to just not respond to messages if we tell it stop and it isn't pumping?
    //So check before we send anything
    if(status == StatusPrompt.PromptI || status == StatusPrompt.PromptW || status == StatusPrompt.PromptX)
    {
      var request = new StopRequest(currentState.Address);
      await Connection.Send(request, TimeSpan.FromSeconds(3));
    }
  }

  public async Task<VolumeDispensedResponse> GetVolumeDispensed()
  {
    var currentState = await GetCurrentState();
    var request = new GetVolumeDispensedRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    if(response.Error is not null)
      throw new InvalidOperationException("Syringe Pump responded with an error!");

    return response;
  }

  public async Task<string> GetFirmwareVersion()
  {
    var currentState = await GetCurrentState();
    var request = new GetFirmwareVersionRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));

    if(response.Error is not null)
      return string.Empty;

    return response.FirmwareVersion;
  }


  public async Task ClearVolumeDispensed(Direction direction)
  {
    if(direction == Direction.UndefinedDirection)
      throw new InvalidOperationException("Cannot Clear Volume Dispensed with undefined direction");

    var currentState = await GetCurrentState();
    var request = new ClearVolumeRequest(currentState.Address, direction);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    await GetVolumeDispensed();
  }

  public async Task SetAddress(int address)
  {
    var request = new Commands.Requests.SetAddressRequest(address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
  }

  public async Task<int> GetAddress()
  {
    var currentState = await GetCurrentState();
    var request = new GetAddressRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));

    if(response.Error != null)
      return -1;

    return response.Address;
  }

  public Task<StateResponse> GetCurrentState() => InternalStateStream.Take(1).ToTask();

  private async Task MonitorDispense()
  {
    var currentState = GetCurrentState().Result;
    while(currentState.Status is StatusPrompt.PromptI or StatusPrompt.PromptW)
    {
      await GetVolumeDispensed();
      await Task.Delay(TimeSpan.FromSeconds(0.5));
      currentState = GetCurrentState().Result;
    }
  }

  private async Task MonitorPurge()
  {
    var currentState = GetCurrentState().Result;
    while(currentState.Status == StatusPrompt.PromptX)
    {
      await GetVolumeDispensed();
      await Task.Delay(TimeSpan.FromSeconds(0.5));
      currentState = GetCurrentState().Result;
    }
  }

  private void StartStateUpdater(TimeSpan interval)
  {
    _stateUpdaterCancellation = new CancellationTokenSource();
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = "Syringe Pump State Updater Thread";
      while(!_stateUpdaterCancellation.IsCancellationRequested)
      {
        try
        {
          await UpdateState();
          await Task.Delay(interval);
        }

        catch(TimeoutException)
        {
          continue;
        }

      }
    }, _stateUpdaterCancellation.Token, TaskCreationOptions.LongRunning);
  }

  private Task StopStateUpdater()
  {
    _stateUpdaterCancellation.Cancel();
    return _stateUpdater;
  }

  private async Task UpdateState()
  {
    var state = await GetStateFromDevice();
    _statePublisher.OnNext(state);
  }

  public override async Task<AresStruct> GetState()
  {
    var state = await _statePublisher.Take(1);

    return AresStateBuilder.Create()
      .Add("Firmware Version", state.FirmwareVersion)
      .Add("DiameterMm", state.DiameterMm)
      .Add("Phase Rate", state.Phase.Rate)
      .Add("Phase Unit", state.Phase.Unit)
      .Add("Volume Units", state.VolumeUnits.ToString())
      .Add("Withdrawn Volume", state.WithdrawnVolume)
      .Add("Address", state.Address)
      .Add("Rate Units", state.RateUnits.ToString())
      .Build();
  }

  private async Task<StateResponse> GetStateFromDevice()
  {
    //if(string.IsNullOrEmpty(FirmwareVersion))
      //FirmwareVersion = await GetFirmwareVersion();

    //var address = await GetAddress();
    var diameter = await GetDiameter();
    var dispensedVolume = await GetVolumeDispensed();
    var rate = await GetProgramFunctionRate();
    var function = await QueryPhaseFunction();
    var phaseNumber = await QueryPhase();
    var direction = await GetProgramFunctionPumpingDirection();
    await GetProgramFunctionVolumeToBeDispensed();


    var state = new StateResponse()
    {
      Address = (int)AssumedAddress,
      DiameterMm = (float)diameter.Millimeters,
      DispensedVolume = (float)dispensedVolume.Infused.Value,
      FirmwareVersion = FirmwareVersion,
      RateUnits = rate.SystemRateUnit,
      VolumeUnits = dispensedVolume.SystemVolumeUnit,
      WithdrawnVolume = (float)dispensedVolume.Withdrawn.Value,
      Status = rate.Status,
      DeviceId = UniqueId,
      Phase = new Phase()
      {
        Number = phaseNumber,
        Function = function.Function,
        Direction = direction,
        Rate = (float)rate.Rate.Value,
        Unit = rate.Rate.Unit.ToString(),
      }
    };

    return state;
  }

  public async Task Start()
  {
    await StopStateUpdater();
    StartStateUpdater(TimeSpan.FromSeconds(4));
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    if(IsSimulated)
      return new SerialDeviceValidationResult(true, "Simulated Syring Pump Detected: Automatic Validation");

    await Awaken();
    FirmwareVersion = await GetFirmwareVersion();
    await GetDiameter();
    await QueryPhase();
    await QueryPhaseFunction();
    await GetProgramFunctionRate();
    await GetProgramFunctionVolumeToBeDispensed();
    await GetVolumeDispensed();
    await GetProgramFunctionPumpingDirection();

    var result = new SerialDeviceValidationResult(true);
    return result;
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await StopPumpingProgram();
  }

  public async ValueTask DisposeAsync()
  {
    _stateUpdaterCancellation.Cancel();
    await _stateUpdater;
    _statePublisher.OnCompleted();
  }

  public uint AssumedAddress { get; private set; }
  public bool IsSimulated { get; }
  public string FirmwareVersion { get; private set; } = "Unknown";
}
