using Ares.Datamodel;
using Ares.Device;
using Ares.Device.Serial;
using LindbergFurnace.Commands;
using LindbergFurnace.Commands.Requests;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using TubeFurnace.Messaging;
using UnitsNet;

namespace LindbergFurnace;

public class TubeFurnace : SerialDevice<ITubeFurnaceConnection>, ITubeFurnace
{
  private readonly ISubject<TubeFurnaceState> _statePublisher = new BehaviorSubject<TubeFurnaceState>(new TubeFurnaceState());

  public TubeFurnace(string name, int address, ITubeFurnaceConnection connection) : base(name, connection)
  {
    StateStream = _statePublisher.AsObservable();
    var initialState =
      new TubeFurnaceState
      {
        Id = UniqueId,
        AssumedAddress = address,
      };
    _statePublisher.OnNext(initialState);
  }

  public async Task GetSetpoint()
  {
    var currentState = await StateStream.Take(1);

    var request = new ReadMultipleRegistersRequest(currentState.AssumedAddress, Register.SP1, 1);
    var response = await Connection.Send(request);
    var setpointData = response.RegisterContents.First();
    var setpointAsciiHEx = setpointData.Select(b => (char)b).ToArray();
    var setpointInt = int.Parse(setpointAsciiHEx, NumberStyles.HexNumber);
    var setpoint = Temperature.FromDegreesCelsius(setpointInt);

    currentState.Setpoint = setpoint.DegreesCelsius;
    _statePublisher.OnNext(currentState);
  }

  public async Task GetCurrentTemperature()
  {
    var request = new ReadMultipleRegistersRequest(1, Register.PV, 1);
    var response = await Connection.Send(request);
    var temperatureData = response.RegisterContents.First();
    var temperatureAsciiHex = temperatureData.Select(b => (char)b).ToArray();
    var temperatureInt = int.Parse(temperatureAsciiHex, NumberStyles.HexNumber);
    var temperature = Temperature.FromDegreesCelsius(temperatureInt);

    var currentState = await StateStream.Take(1);
    currentState.CurrentTemperature = temperature.DegreesCelsius;
    _statePublisher.OnNext(currentState);
  }

  public async Task<int> GetCurrentAddress()
  {
    var currentState = await StateStream.Take(1);
    if(currentState is not null)
      return currentState.AssumedAddress;

    return -1;
  }

  public async Task SetAndWaitForSetpoint(Temperature targetTemperature, double delta, double timeout, CancellationToken ct = default)
  {
    await SetSetpoint(targetTemperature);

    var task = StateStream
      .Where(state => Math.Abs(targetTemperature.DegreesCelsius - state.CurrentTemperature) <= delta)
      .FirstAsync()
      .ToTask(ct);

    if(timeout == -1)
    {
      await task;
    }

    else
    {
      try
      {
        var timespan = TimeSpan.FromSeconds(timeout);
        await task.WaitAsync(timespan, ct);
      }
      catch (TimeoutException)
      {
        throw new Exception($"Setpoint of {targetTemperature.DegreesCelsius} not reached within the given timeout value of {timeout} seconds");
      }
    }

  }

  protected override Task<SerialDeviceValidationResult> Validate()
  {
    // TODO
    var result = new SerialDeviceValidationResult(true, "TODO: Dont cheat");
    return Task.FromResult(result);
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await SetSetpoint(Temperature.FromDegreesCelsius(25.0));
  }

  public void Dispose()
  {
    _statePublisher.OnCompleted();
  }

  public async Task SetSetpoint(Temperature targetTemperature)
  {
    var degreesCelsius = (int)targetTemperature.DegreesCelsius;
    var setpointWrite = new RegisterReadWrite { Register = Register.SP1, UpperDigit = (byte)(degreesCelsius >> 8), LowerDigit = (byte)degreesCelsius };
    var request = new WriteMultipleRegistersRequest(1, setpointWrite);
    var response = await Connection.Send(request);
  }

  public override async Task<AresStruct> GetState()
  {
    var currentState = await StateStream.Take(1);

    return
      AresStateBuilder.Create()
      .Add("Current Temperature", currentState.CurrentTemperature)
      .Add("Target Setpoint", currentState.TargetSetpoint)
      .Add("Setpoint", currentState.Setpoint)
      .Add("Assumed Address", currentState.AssumedAddress)
      .Add("Id", currentState.Id)
      .Build();
  }
  public IObservable<TubeFurnaceState> StateStream { get; }
}
