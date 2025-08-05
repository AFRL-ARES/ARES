using HerkulexDRS;
using System;
using System.Linq;
using VerdiV6Laser;
using VerdiV6Laser.Simulated;

namespace AresService.ConnectionManagement;

public class VerdiLaserSerialConnectionManager : ISerialConnectionManager<ILaserConnection>
{
  private readonly ISerialConnectionRepository _connectionRepository;
  public VerdiLaserSerialConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public ILaserConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if(existingConnections.Any(conn => conn is not ILaserConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if(simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimulatedLaserConnection>().FirstOrDefault();
      if(simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimulatedLaserConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<LaserConnection>().FirstOrDefault();
    if(hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new LaserConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public void RemoveConnection(ILaserConnection connection)
  {
    connection.Dispose();
    _connectionRepository.Remove(connection);
  }

  public void RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if(simulated)
      existingConnections = existingConnections.OfType<SimulatedLaserConnection>();
    else
      existingConnections = existingConnections.OfType<LaserConnection>();

    var connection = existingConnections.FirstOrDefault();
    if(connection is null)
      return;

    connection.Dispose();
    _connectionRepository.Remove(connection);
  }
}
