using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace UI.Backend.Devices;

public interface IAresDeviceAdapter
{
  string Id { get; }
  string Name { get; }
  string Description { get; }
  string Type { get; }
  string Version { get; }
  AresStruct? State { get; }
  IObservable<AresStruct?> StateStream { get; }
  DeviceOperationalStatus OperationalStatus { get; }
}
