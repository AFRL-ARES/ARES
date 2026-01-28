using Ares.Device.Serial;
using TubeFurnace.Messaging;
using UnitsNet;

namespace LindbergFurnace
{
  public interface ITubeFurnace : ISerialDevice<ITubeFurnaceConnection>, IDisposable
  {
    Task GetSetpoint();
    Task GetCurrentTemperature();
    Task SetSetpoint(Temperature targetTemperature);
    Task<int> GetCurrentAddress();
    Task SetAndWaitForSetpoint(Temperature targetTemperature, double delta, double timeout, CancellationToken ct = default);
    IObservable<TubeFurnaceState> InternalStateStream { get; }
  }
}
