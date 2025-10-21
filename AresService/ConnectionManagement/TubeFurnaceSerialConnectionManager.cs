using System;
using System.Linq;
using LindbergFurnace;

namespace AresService.ConnectionManagement
{
  public class TubeFurnaceSerialConnectionManager : ISerialConnectionManager<ITubeFurnaceConnection>
  {
    readonly ISerialConnectionRepository _connectionRepository;
    public TubeFurnaceSerialConnectionManager(ISerialConnectionRepository connectionRepository)
    {
      _connectionRepository = connectionRepository;
    }

    public ITubeFurnaceConnection GetConnection(string portName, bool simulated = false)
    {
      var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

      if (existingConnections.Any(conn => conn is not TubeFurnaceConnection))
        throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

      if (simulated)
      {
        var simulatedConnection = existingConnections.OfType<SimTubeFurnaceConnection>().FirstOrDefault();
        if (simulatedConnection is not null)
          return simulatedConnection;

        simulatedConnection = new SimTubeFurnaceConnection(portName);
        _connectionRepository.Add(simulatedConnection);
        return simulatedConnection;
      }

      var hardwareConnection = existingConnections.OfType<TubeFurnaceConnection>().FirstOrDefault();
      if (hardwareConnection is not null)
        return hardwareConnection;

      hardwareConnection = new TubeFurnaceConnection(portName);
      _connectionRepository.Add(hardwareConnection);
      return hardwareConnection;
    }

    public void RemoveConnection(string portName, bool simulated = false)
    {
      var existingConnections = _connectionRepository.Where(port => port.Name == portName);
      if (simulated)
        existingConnections = existingConnections.OfType<SimTubeFurnaceConnection>();
      else
        existingConnections = existingConnections.OfType<TubeFurnaceConnection>();

      var connection = existingConnections.FirstOrDefault();
      if (connection is null)
        return;

      connection.Dispose();
      _connectionRepository.Remove(connection);
    }

    public void RemoveConnection(ITubeFurnaceConnection connection)
    {
      connection.Dispose();
      _connectionRepository.Remove(connection);
    }
  }
}
