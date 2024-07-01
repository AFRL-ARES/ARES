using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    IObservable<TubeFurnaceState> StateStream { get; }
  }
}
