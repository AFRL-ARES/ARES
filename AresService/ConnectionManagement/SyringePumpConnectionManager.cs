using System;
using System.Linq;
using SyringePumpNE1000;
using SyringePumpNE1000.Simulation;

namespace AresService.ConnectionManagement;

public class SyringePumpConnectionManager : ISerialConnectionManager<ISyringePumpConnection>
{
  private readonly ISerialConnectionRepository _connectionRepository;

  public SyringePumpConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public ISyringePumpConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if(existingConnections.Any(conn => conn is not ISyringePumpConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if(simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimSyringePumpConnection>().FirstOrDefault();
      if(simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimSyringePumpConnection(portName, "SimSyringePump");
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<SyringePumpConnection>().FirstOrDefault();
    if(hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new SyringePumpConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public void RemoveConnection(ISyringePumpConnection connection)
  {
    connection.Dispose();
    _connectionRepository.Remove(connection);
  }

  public void RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if(simulated)
      existingConnections = existingConnections.OfType<SimSyringePumpConnection>();
    else
      existingConnections = existingConnections.OfType<SyringePumpConnection>();

    var connection = existingConnections.FirstOrDefault();
    if(connection is null)
      return;

    connection.Dispose();
    _connectionRepository.Remove(connection);
  }
}
