using System;
using System.Linq;
using System.Threading.Tasks;
using TicStepperController;

namespace AresService.ConnectionManagement;
public class StepperControllerSerialConnectionManager : ISerialConnectionManager<IStepperControllerConnection>
{
  readonly ISerialConnectionRepository _connectionRepository;
  public StepperControllerSerialConnectionManager(ISerialConnectionRepository connectionRepository)
  {
    _connectionRepository = connectionRepository;
  }

  public IStepperControllerConnection GetConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName).ToArray();

    if (existingConnections.Any(conn => conn is not StepperControllerConnection))
      throw new InvalidOperationException($"Port name {portName} is already in use by connection {existingConnections.First().GetType().FullName}");

    if (simulated)
    {
      var simulatedConnection = existingConnections.OfType<SimStepperControllerConnection>().FirstOrDefault();
      if (simulatedConnection is not null)
        return simulatedConnection;

      simulatedConnection = new SimStepperControllerConnection(portName);
      _connectionRepository.Add(simulatedConnection);
      return simulatedConnection;
    }

    var hardwareConnection = existingConnections.OfType<StepperControllerConnection>().FirstOrDefault();
    if (hardwareConnection is not null)
      return hardwareConnection;

    hardwareConnection = new StepperControllerConnection(portName);
    _connectionRepository.Add(hardwareConnection);
    return hardwareConnection;
  }

  public async Task RemoveConnection(string portName, bool simulated = false)
  {
    var existingConnections = _connectionRepository.Where(port => port.Name == portName);
    if (simulated)
      existingConnections = existingConnections.OfType<SimStepperControllerConnection>();
    else
      existingConnections = existingConnections.OfType<StepperControllerConnection>();

    var connection = existingConnections.FirstOrDefault();
    if (connection is null)
      return;

    await connection.DisposeAsync();
    _connectionRepository.Remove(connection);
  }

  public async Task RemoveConnection(IStepperControllerConnection connection)
  {
    await connection.DisposeAsync();
    _connectionRepository.Remove(connection);
  }
}
