using Ares.Device.Serial;
using LaserChiller.Commands.Responses;

namespace LaserChiller;

public interface ILaserChiller : ISerialDevice<ILaserChillerConnection>, IAsyncDisposable
{
  Task SetStabilizedTemperature(double targetTemperature);

  Task SetChillerRunMode();

  Task SetChillerStandbyMode();

  Task<double?> GetManifoldTemperature();

  Task StartStateUpdater();

  GetManifoldTemperatureResponse? GetState();

  public double CurrentTemperature { get; }

  public double TargetTemperature { get; }

  IObservable<GetManifoldTemperatureResponse?> StateStream { get; }
}
