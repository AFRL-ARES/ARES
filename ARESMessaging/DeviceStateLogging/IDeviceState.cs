using Google.Protobuf.WellKnownTypes;

namespace Ares.Messages.DeviceStates;
public interface IDeviceState
{
  string DeviceId { get; set; }
  Timestamp Timestamp { get; set; }
}
