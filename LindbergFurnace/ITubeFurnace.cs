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
    IObservable<TubeFurnaceState> StateStream { get; }
  }
}
