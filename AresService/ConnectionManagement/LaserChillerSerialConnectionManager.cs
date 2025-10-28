using System;
using System.Linq;
using LaserChiller;
using LaserChiller.Simulated;
using VerdiV6Laser;

namespace AresService.ConnectionManagement;

public class LaserChillerSerialConnectionManager : ISerialConnectionManager<ILaserChillerConnection>
{
  private readonly ISerialConnectionRepository _connectionRepository;

  public LaserChillerSerialConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public ILaserChillerConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if(existingConnections.Any(conn => conn is not ILaserConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if(simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimLaserChillerConnection>().FirstOrDefault();
      if(simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimLaserChillerConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<LaserChillerConnection>().FirstOrDefault();
    if(hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new LaserChillerConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public void RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if(simulated)
      existingConnections = existingConnections.OfType<SimLaserChillerConnection>();
    else
      existingConnections = existingConnections.OfType<LaserChillerConnection>();

    var connection = existingConnections.FirstOrDefault();
    if(connection is null)
      return;

    connection.Dispose();
    _connectionRepository.Remove(connection);
  }

  public void RemoveConnection(ILaserChillerConnection connection)
  {
    connection.Dispose();
    _connectionRepository.Remove(connection);
  }
}
