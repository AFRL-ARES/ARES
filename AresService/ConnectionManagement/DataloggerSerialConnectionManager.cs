using System;
using System.Linq;
using TC0304;

namespace AresService.ConnectionManagement;

public class DataloggerSerialConnectionManager : ISerialConnectionManager<IDataloggerThermometerConnection>
{
  private readonly ISerialConnectionRepository _connectionRepository;

  public DataloggerSerialConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public IDataloggerThermometerConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if (existingConnections.Any(conn => conn is not IDataloggerThermometerConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if (simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimDataloggerThermometerConnection>().FirstOrDefault();
      if (simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimDataloggerThermometerConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<DataloggerThermometerConnection>().FirstOrDefault();
    if (hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new DataloggerThermometerConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public void RemoveConnection(IDataloggerThermometerConnection connection)
  {
    connection.Dispose();
    _connectionRepository.Remove(connection);
  }

  public void RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if (simulated)
      existingConnections = existingConnections.OfType<SimDataloggerThermometerConnection>();
    else
      existingConnections = existingConnections.OfType<DataloggerThermometerConnection>();

    var connection = existingConnections.FirstOrDefault();
    if (connection is null)
      return;

    connection.Dispose();
    _connectionRepository.Remove(connection);
  }
}
