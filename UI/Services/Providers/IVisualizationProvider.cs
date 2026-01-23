using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace UI.Services.Providers;

public interface IVisualizationProvider : IDisposable
{
  Task<AresDataSchema> GetDeviceStateOptions(string deviceId);
  IObservable<IReadOnlyList<DeviceInfo>> AvailableDevicesStream { get; }
}
