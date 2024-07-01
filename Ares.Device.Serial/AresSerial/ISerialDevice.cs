namespace Ares.Device.Serial;

public interface ISerialDevice<out TConnection> : IAresDevice where TConnection : IAresSerialConnection
{
  TConnection Connection { get; }
}
