using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Device.State;
public interface IDeviceState
{
  string DeviceId { get; set; }
  Timestamp Timestamp { get; set; }
}
