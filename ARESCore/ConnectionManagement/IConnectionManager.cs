using Ares.Device.Serial;

namespace ARESCore.ConnectionManagement;

public interface IConnectionManager<TConnection> where TConnection : IAresSerialConnection
{
  TConnection GetConnection(string portName, bool simulated = false);
  void RemoveConnection(string portName, bool simulated = false);
  void RemoveConnection(TConnection connection);
}
