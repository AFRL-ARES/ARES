using HerkulexDRS;
using System;
using System.Linq;

namespace ARESCore.ConnectionManagement;
public class ServoConnectionManager : IConnectionManager<IServoConnection>
{
  private readonly IConnectionRepository _connectionRepository;

  public ServoConnectionManager(IConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public IServoConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if (existingConnections.Any(conn => conn is not IServoConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if (simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimServoConnection>().FirstOrDefault();
      if (simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimServoConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<ServoConnection>().FirstOrDefault();
    if (hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new ServoConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public void RemoveConnection(IServoConnection connection)
  {
    connection.Dispose();
    _connectionRepository.Remove(connection);
  }

  public void RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if (simulated)
      existingConnections = existingConnections.OfType<SimServoConnection>();
    else
      existingConnections = existingConnections.OfType<ServoConnection>();

    var connection = existingConnections.FirstOrDefault();
    if (connection is null)
      return;

    connection.Dispose();
    _connectionRepository.Remove(connection);
  }
}
