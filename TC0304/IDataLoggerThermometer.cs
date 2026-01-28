using Ares.Device.Serial;
using TC0304.Commands;

namespace TC0304;

public interface IDataloggerThermometer : ISerialDevice<IDataloggerThermometerConnection>, IAsyncDisposable
{
  IObservable<DataResponse?> StateStream { get; }
  Task<DataResponse> GetAndUpdateState();
  Task<double?[]> GetTemperatures();
  DataResponse? GetInternalState();
  Task StartStateUpdater(TimeSpan interval);
  Task StartStateUpdater();
  Task StopStateUpdater();
  void Hold();
}
