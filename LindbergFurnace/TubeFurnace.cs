using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Intrinsics.Arm;
using Ares.Device.Serial;
using LindbergFurnace.Commands;
using LindbergFurnace.Commands.Requests;
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
        Name = name,
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
    var temperatureAsciiHEx = temperatureData.Select(b => (char)b).ToArray();
    var temperatureInt = int.Parse(temperatureAsciiHEx, NumberStyles.HexNumber);
    var temperature = Temperature.FromDegreesCelsius(temperatureInt);

    var currentState = await StateStream.Take(1);
    currentState.CurrentTemperature = temperature.DegreesCelsius;
    _statePublisher.OnNext(currentState);
  }


  public IObservable<TubeFurnaceState> StateStream { get; }

  protected override Task<DeviceValidationResult> Validate()
  {
    // TODO
    var result = new DeviceValidationResult(true, "TODO: Dont cheat");
    return Task.FromResult(result);
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
}
