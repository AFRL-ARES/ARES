using Google.Protobuf.WellKnownTypes;

namespace AresMessaging.DeviceStateLogging;
public interface IDeviceState
{
  string DeviceId { get; set; }
  Timestamp Timestamp { get; set; }
}
