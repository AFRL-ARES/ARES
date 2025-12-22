using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Simulation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.ConnectionManagement;

public class ChemyxPumpSerialConnectionManager : ISerialConnectionManager<IChemyxPumpConnection>
{
  private readonly ISerialConnectionRepository _connectionRepository;

  public ChemyxPumpSerialConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public IChemyxPumpConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if(existingConnections.Any(conn => conn is not IChemyxPumpConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if(simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimChemyxPumpConnection>().FirstOrDefault();
      if(simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimChemyxPumpConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<ChemyxPumpConnection>().FirstOrDefault();
    if(hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new ChemyxPumpConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public async Task RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if(simulated)
      existingConnections = existingConnections.OfType<SimChemyxPumpConnection>();
    else
      existingConnections = existingConnections.OfType<ChemyxPumpConnection>();

    var connection = existingConnections.FirstOrDefault();
    if(connection is null)
      return;

    await connection.DisposeAsync();
    _connectionRepository.Remove(connection);
  }

  public async Task RemoveConnection(IChemyxPumpConnection connection)
  {
    await connection.DisposeAsync();
    _connectionRepository.Remove(connection);
  }
}
