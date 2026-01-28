using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Device;
using Ares.Device.Serial;
using LaserChiller.Commands.Requests;
using LaserChiller.Commands.Responses;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace LaserChiller;

public class LaserChiller : SerialDevice<ILaserChillerConnection>, ILaserChiller
{
  private readonly ISubject<GetManifoldTemperatureResponse?> _stateSubject = new BehaviorSubject<GetManifoldTemperatureResponse?>(default);
  private CancellationTokenSource _internalStateUpdateTokenSource = new();
  private Task? _stateUpdater;

  public LaserChiller(string name, ILaserChillerConnection connection) : base(name, connection)
  {
    StateStream = _stateSubject.AsObservable();
  }

  public async Task SetStabilizedTemperature(double targetTemperature)
  {
    await Connection.Send(new SetStabilizedTemperatureCommand(targetTemperature));
    TargetTemperature = targetTemperature;
  }

  public async Task SetChillerRunMode()
  {
    await Connection.Send(new SetRunModeCommand());
  }

  public async Task SetChillerStandbyMode()
  {
    await Connection.Send(new SetStandbyModeCommand());
  }

  public async Task<double?> GetManifoldTemperature()
  {
    var temp = await Connection.Send(new GetManifoldTemperatureCommand());

    if(temp is not null)
    {
      CurrentTemperature = temp.Temperature;
      return CurrentTemperature;
    }

    return null;
  }

  public async Task<GetManifoldTemperatureResponse> GetAndUpdateState()
  {
    var response = await Connection.Send(new GetManifoldTemperatureCommand());
    _stateSubject.OnNext(response);
    return response;
  }

  public GetManifoldTemperatureResponse? GetInternalState()
  => StateStream.Take(1).Wait();

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(
      AresStateBuilder.Create()
      .Add("CurrentTemperature", CurrentTemperature)
      .Add("TargetTemperature", TargetTemperature)
      .Build());
  }

  private async Task StartStateUpdater(TimeSpan interval, CancellationToken token)
  {
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {
      try
      {
        while(!token.IsCancellationRequested)
        {
          try
          {
            var response = await Connection.Send(new GetManifoldTemperatureCommand(), TimeSpan.FromSeconds(5));
            _stateSubject.OnNext(response);
          }
          catch(TimeoutException)
          { }
          await Task.Delay(interval, token);
        }
      }
      catch(ObjectDisposedException)
      {
      }
    },
      token,
      TaskCreationOptions.LongRunning);
  }

  public async Task StartStateUpdater(TimeSpan interval)
  {
    await StopStateUpdater();
    _internalStateUpdateTokenSource = new CancellationTokenSource();
    await StartStateUpdater(interval, _internalStateUpdateTokenSource.Token);
  }

  public async Task StartStateUpdater()
  {
    await StopStateUpdater();
    await StartStateUpdater(TimeSpan.FromMilliseconds(250));
  }

  public async Task StopStateUpdater()
  {
    _internalStateUpdateTokenSource.Cancel();
    if(_stateUpdater is not null)
      await _stateUpdater;
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      var temp = await Connection.Send(new GetManifoldTemperatureCommand(), TimeSpan.FromSeconds(10));
      return new SerialDeviceValidationResult(true);
    }

    catch(Exception ex)
    {
      return new SerialDeviceValidationResult(false, ex.Message);
    }
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    var activated = await base.Activate(ct);
    if(!activated)
      return false;

    await StartStateUpdater();
    return true;
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    //TODO: IMPLEMENT ME!!
    throw new NotImplementedException();
  }

  public IObservable<GetManifoldTemperatureResponse?> StateStream { get; }

  public double CurrentTemperature { get; set; }

  public double TargetTemperature { get; set; }
}
