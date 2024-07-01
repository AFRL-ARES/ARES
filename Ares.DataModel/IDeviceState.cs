using Google.Protobuf.WellKnownTypes;

namespace ARESMessaging.DeviceStateLogging;
public interface IDeviceState
{
  string DeviceId { get; set; }
  Timestamp Timestamp { get; set; }
}
