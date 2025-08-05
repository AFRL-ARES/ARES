using RestSerialDevice;
using RestSerialDevice.Simulation;
using System;
using System.Linq;

namespace AresService.ConnectionManagement;

public class SerialRestDeviceConnectionManager : ISerialConnectionManager<ISerialRestDeviceConnection>
{
  private readonly ISerialConnectionRepository _connectionRepository;

  public SerialRestDeviceConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public ISerialRestDeviceConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if(existingConnections.Any(conn => conn is not ISerialRestDeviceConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if(simulated)
    {
      var simConnection = existingConnections.OfType<SimRestSerialConnection>().FirstOrDefault();
      if(simConnection is not null)
        return simConnection;

      simConnection = new SimRestSerialConnection(portName);
      _connectionRepository.Add(simConnection);
      return simConnection;
    }

    var hardwareConnection = existingConnections.OfType<SerialRestDeviceConnection>().FirstOrDefault();
    if(hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new SerialRestDeviceConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public void RemoveConnection(ISerialRestDeviceConnection hardwareConnection)
  {
    hardwareConnection.Dispose();
    _connectionRepository?.Remove(hardwareConnection);
  }

  public void RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if(simulated)
      existingConnections = existingConnections.OfType<SimRestSerialConnection>();

    else
      existingConnections = existingConnections.OfType<SerialRestDeviceConnection>();

    var connection = existingConnections.FirstOrDefault();
    if(connection is null)
      return;

    connection.Dispose();
    _connectionRepository.Remove(connection);
  }
}
