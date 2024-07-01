using Ares.Device.Serial;
using Ares.SyringePump.Ne1000.Messaging;
using SyringePumpNE1000.Commands;
using SyringePumpNE1000.Commands.Requests;
using SyringePumpNE1000.Commands.Responses;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using UnitsNet;

namespace SyringePumpNE1000;

public class SyringePump : SerialDevice<ISyringePumpConnection>, ISyringePump
{
  private readonly ISubject<StateResponse> _statePublisher = new BehaviorSubject<StateResponse>(new StateResponse());


  // < safe command protocol> => < STX > < length > < command data > < CRC 16 > < ETX >
  public SyringePump(string identifier, uint address, ISyringePumpConnection connection) : base(identifier, connection)
  {
    StateStream = _statePublisher.AsObservable();
    AssumedAddress = address;

    var initialState = new StateResponse { Address = (int)AssumedAddress };
    _statePublisher.OnNext(initialState);
  }

  public IObservable<StateResponse> StateStream { get; }

  public async Task SetPhase(int phase)
  {
    var currentState = GetCurrentState();
    var request = new Commands.Requests.SetPhaseNumberRequest(currentState.Address, phase);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
    await QueryPhase();
  }

  public async Task SetPhaseFunction(Ares.SyringePump.Ne1000.Messaging.Commands function)
  {
    var currentState = GetCurrentState();
    var request = new Commands.Requests.SetPhaseFunctionRequest(currentState.Address, function);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
    await QueryPhaseFunction();
  }

  public async Task SetDiameter(Length diameter)
  {
    var currentState = GetCurrentState();
    var request = new SetDiameterRequest(currentState.Address, diameter);
    var response = await Connection
      .Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
    await GetDiameter();
  }

  public async Task GetDiameter()
  {
    var currentState = GetCurrentState();
    var request = new Commands.Requests.GetDiameterRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(6));
    UpdateState(response);
  }

  /// <summary>
  /// Syringe pumps appear unresponsive until they get some sort of RS232 command.
  /// So we just send a random one and expect an exception
  /// </summary>
  /// <returns></returns>
  private async Task Awaken()
  {
    var currentState = GetCurrentState();
    var request = new Commands.Requests.GetDiameterRequest(currentState.Address);
    try
    {
      var response = await Connection.Send(request, TimeSpan.FromSeconds(1));
    }
    catch (TimeoutException)
    {
      // Ignore the exception, should be expected the first time
    }
  }

  // Note: There IS an 'I' instead of 'C' rate argument that could be used, but it doesn't sound like we will
  public async Task SetProgramFunctionRate(Speed rate)
  {
    var currentState = GetCurrentState();
    var request = new SetPhaseFunctionRateRequest(currentState.Address, rate);

    var setResponse = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(setResponse);
    await GetProgramFunctionRate();
  }

  public Task GetProgramFunctionRate()
  {
    var currentState = GetCurrentState();
    var request = new GetPhaseFunctionRate(currentState.Address);
    return Connection.Send(request, TimeSpan.FromSeconds(3)).ContinueWith(response => UpdateState(response.Result),
      TaskContinuationOptions.OnlyOnRanToCompletion);
  }

  public Task QueryPhaseFunction()
  {
    var currentState = GetCurrentState();
    var request = new Commands.Requests.QueryPhaseFunctionRequest(currentState.Address);
    return Connection.Send(request, TimeSpan.FromSeconds(3))
      .ContinueWith(response => UpdateState(response.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
  }

  public async Task SetProgramFunctionVolumeToBeDispensed(Volume volume)
  {
    var currentState = GetCurrentState();
    var request = new SetPhaseFunctionVolumeRequest(currentState.Address, volume);

    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
    await GetProgramFunctionVolumeToBeDispensed();
  }

  public Task GetProgramFunctionVolumeToBeDispensed()
  {
    var currentState = GetCurrentState();
    var request = new GetPhaseFunctionVolumeRequest(currentState.Address);
    return Connection
      .Send(request, TimeSpan.FromSeconds(3))
      .ContinueWith(response => UpdateState(response.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
  }

  public async Task SetProgramFunctionPumpingDirection(Direction direction)
  {
    var currentState = GetCurrentState();
    var request = new SetPhaseFunctionDirectionRequest(currentState.Address, direction);

    var response = await Connection
      .Send(request, TimeSpan.FromSeconds(3))
      .ContinueWith(response => UpdateState(response.Result), TaskContinuationOptions.OnlyOnRanToCompletion)
      .ContinueWith(_ => GetProgramFunctionPumpingDirection());
  }

  public Task GetProgramFunctionPumpingDirection()
  {
    var currentState = GetCurrentState();
    var request = new GetPhaseFunctionDirectionRequest(currentState.Address);
    return Connection
      .Send(request, TimeSpan.FromSeconds(3))
      .ContinueWith(response => UpdateState(response.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
  }

  public async Task StartPumpingProgram()
  {
    var currentState = GetCurrentState();
    var request = new StartRequest(currentState.Address);

    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
    await MonitorDispense();
  }

  public Task QueryPhase()
  {
    var currentState = GetCurrentState();
    var request = new PhaseQueryRequest(currentState.Address);
    return Connection
      .Send(request, TimeSpan.FromSeconds(3))
      .ContinueWith(response => UpdateState(response.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
  }

  public async Task PurgePump()
  {
    var currentState = GetCurrentState();
    var request = new PurgeRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
    await MonitorPurge();
  }

  public async Task StopPumpingProgram()
  {
    var currentState = GetCurrentState();
    var request = new StopRequest(currentState.Address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
  }

  public Task GetVolumeDispensed()
  {
    var currentState = GetCurrentState();
    var request = new Commands.Requests.GetVolumeDispensedRequest(currentState.Address);
    return Connection
      .Send(request, TimeSpan.FromSeconds(3))
      .ContinueWith(response => UpdateState(response.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
  }

  public async Task ClearVolumeDispensed(Direction direction)
  {
    if (direction == Direction.UndefinedDirection)
      throw new InvalidOperationException("Cannot Clear Volume Dispensed with undefined direction");

    var currentState = GetCurrentState();
    var request = new ClearVolumeRequest(currentState.Address, direction);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
    await GetVolumeDispensed();
  }

  public async Task SetAddress(int address)
  {
    var request = new Commands.Requests.SetAddressRequest(address);
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
  }

  public async Task GetAddress()
  {
    var request = new Commands.Requests.GetAddressRequest();
    var response = await Connection.Send(request, TimeSpan.FromSeconds(3));
    UpdateState(response);
  }

  public StateResponse GetCurrentState()
  {
    var getCurrentStateTask = StateStream.Take(1).ToTask();
    getCurrentStateTask.Wait();
    var currentState = getCurrentStateTask.Result;
    return currentState;
  }

  public StateResponse GetUpdatedState()
  {
    var getNextStateTask = Task.Run(() => StateStream.Take(2).ToTask());
    getNextStateTask.Wait();
    var currentState = getNextStateTask.Result;
    return currentState;
  }

  private void UpdateState(SetAddressResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    currentState.Address = response.Address;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(AddressQueryResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    currentState.Address = response.Address;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(DiameterResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    currentState.DiameterMm = (float)response.Diameter.Millimeters;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(PhaseNumberResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    if (currentState.Phase == null)
      currentState.Phase = new Phase();

    currentState.Phase.Number = response.Phase;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(PhaseFunctionDirectionResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    if (currentState.Phase == null)
      currentState.Phase = new Phase();

    currentState.Phase.Direction = response.Direction;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(PhaseFunctionResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    if (currentState.Phase == null)
      currentState.Phase = new Phase();

    currentState.Phase.Function = response.Function;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(PhaseFunctionRateResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    if (currentState.Phase == null)
      currentState.Phase = new Phase();

    var unit = response.SystemRateUnit.ToUnitsNet();
    currentState.RateUnits = response.SystemRateUnit;
    var rate = response.Rate.As(unit);
    currentState.Phase.Rate = (float)rate;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(PhaseFunctionVolumeResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    if (currentState.Phase == null)
      currentState.Phase = new Phase();

    currentState.VolumeUnits = response.SystemVolumeUnit;
    var unit = response.SystemVolumeUnit.ToUnitsNet();
    var volume = response.Volume.As(unit);
    currentState.Phase.Volume = (float)volume;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(VolumeDispensedResponse response)
  {
    UpdateStatus(response.Status);
    var currentState = GetCurrentState();
    currentState.VolumeUnits = response.SystemVolumeUnit;
    var unit = response.SystemVolumeUnit.ToUnitsNet();
    var withdrawnVolume = response.Withdrawn.As(unit);
    var infusedVolume = response.Infused.As(unit);
    currentState.WithdrawnVolume = (float)withdrawnVolume;
    currentState.DispensedVolume = (float)infusedVolume;
    _statePublisher.OnNext(currentState);
  }

  private void UpdateState(IgnorableResponse response)
  {
    UpdateStatus(response.Status);
  }

  private void UpdateState(Response response)
  {
    UpdateStatus(response.Status);
    var derivedType = response.GetType();
    if (derivedType != typeof(Response))
      throw new NotImplementedException($"UpdateState({derivedType.Name} response) not implemented or failed to access");

    if (response.Error != null)
      throw new InvalidOperationException($"{response.Error:G}");
  }

  private void UpdateStatus(StatusPrompt status)
  {
    var currentState = GetCurrentState();
    currentState.Status = status;
    _statePublisher.OnNext(currentState);
  }

  private async Task MonitorDispense()
  {
    var currentState = GetCurrentState();
    while (currentState.Status is StatusPrompt.PromptI or StatusPrompt.PromptW)
    {
      await GetVolumeDispensed();
      await Task.Delay(TimeSpan.FromSeconds(0.5));
      currentState = GetCurrentState();
    }
  }

  private async Task MonitorPurge()
  {
    var currentState = GetCurrentState();
    while (currentState.Status == StatusPrompt.PromptX)
    {
      await GetVolumeDispensed();
      await Task.Delay(TimeSpan.FromSeconds(0.5));
      currentState = GetCurrentState();
    }
  }


  protected override async Task<DeviceValidationResult> Validate()
  {
    // await GetAddress();
    await Awaken();
    await GetDiameter();
    await QueryPhase();
    await QueryPhaseFunction();
    await GetProgramFunctionRate();
    await GetProgramFunctionVolumeToBeDispensed();
    await GetVolumeDispensed();
    await GetProgramFunctionPumpingDirection();

    var result = new DeviceValidationResult(true, "Probably OK if we got to this without an exception crashing the system");
    return result;
  }

  public void Dispose()
  {
    _statePublisher.OnCompleted();
  }

  public uint AssumedAddress { get; private set; }
}
