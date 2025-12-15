using System;
using System.Linq;
using System.Threading.Tasks;
using AlicatMFC;
using AlicatMFC.Simulation;

namespace AresService.ConnectionManagement;

public class MfcSerialConnectionManager : ISerialConnectionManager<IMfcConnection>
{
  private readonly ISerialConnectionRepository _connectionRepository;

  public MfcSerialConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public IMfcConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if (existingConnections.Any(conn => conn is not IMfcConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if (simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimMassFlowControllerConnection>().FirstOrDefault();
      if (simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimMassFlowControllerConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<MassFlowControllerConnection>().FirstOrDefault();
    if (hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new MassFlowControllerConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public async Task RemoveConnection(IMfcConnection connection)
  {
    await connection.DisposeAsync();
    _connectionRepository.Remove(connection);
  }

  public async Task RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if (simulated)
      existingConnections = existingConnections.OfType<SimMassFlowControllerConnection>();
    else
      existingConnections = existingConnections.OfType<MassFlowControllerConnection>();

    var connection = existingConnections.FirstOrDefault();
    if (connection is null)
      return;

    await connection.DisposeAsync();
    _connectionRepository.Remove(connection);
  }
}
