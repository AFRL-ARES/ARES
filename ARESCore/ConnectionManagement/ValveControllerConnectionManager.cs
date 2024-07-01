using System;
using System.Linq;
using ValveController;

namespace ARESCore.ConnectionManagement;
public class ValveControllerConnectionManager : IConnectionManager<IValveControllerConnection>
{
  private readonly IConnectionRepository _connectionRepository;

  public ValveControllerConnectionManager(IConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public IValveControllerConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if (existingConnections.Any(conn => conn is not IValveControllerConnection))
    {
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");
    }

    if (simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimValveControllerConnection>().FirstOrDefault();
      if (simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimValveControllerConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<ValveControllerConnection>().FirstOrDefault();
    if (hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new ValveControllerConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public void RemoveConnection(IValveControllerConnection connection)
  {
    connection.Dispose();
    _connectionRepository.Remove(connection);
  }

  public void RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if (simulated)
      existingConnections = existingConnections.OfType<SimValveControllerConnection>();
    else
      existingConnections = existingConnections.OfType<ValveControllerConnection>();

    var connection = existingConnections.FirstOrDefault();
    if (connection is null)
      return;

    connection.Dispose();
    _connectionRepository.Remove(connection);
  }
}