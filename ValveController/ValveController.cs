using Ares.Device.Serial;
using ValveController.Commands;
using ValveController.Commands.RelayOne;
using ValveController.Commands.RelayTwo;
using ValveController.Commands.Responses;

namespace ValveController;
public class ValveController : SerialDevice<IValveControllerConnection>, IValveController
{
  public ValveController(string name, IValveControllerConnection connection) : base(name, connection)
  {

  }

  public async Task<RelayStatusResponse> GetRelayStatus()
  {
    await Connection.Send(new EnterCommandModeCommand());
    var response = await Connection.Send(new GetRelayStatusCommand());
    if (response != null)
    {
      RelayOneEngaged = response.RelayOneOn;
      RelayTwoEngaged = response.RelayTwoOn;
    }

    else
    {
      throw new Exception("No Status Response from Relay Board!");
    }

    return response;
  }

  protected override async Task<DeviceValidationResult> Validate()
  {
    try
    {
      //Activate the relay, then send our status request to confirm proper activation
      EnableRelays();
      await GetRelayStatus();

      return new DeviceValidationResult(true);
    }

    catch (Exception ex)
    {
      return new DeviceValidationResult(false, ex.Message);
    }
  }

  public async void EngageRelayOne()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new EngageRelayOneCommand());
    RelayOneEngaged = true;
  }

  public async void EngageRelayTwo()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new EngageRelayTwoCommand());
    RelayTwoEngaged = true;
  }

  public async void DisengageRelayOne()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new DisengageRelayOneCommand());
    RelayOneEngaged = false;
  }

  public async void DisengageRelayTwo()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new DisengageRelayTwoCommand());
    RelayOneEngaged = false;
  }

  public async void EnableRelays()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new EnableAllDevicesCommand());
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  public bool RelayOneEngaged { get; set; } = false;

  public bool RelayTwoEngaged { get; set; } = false;
}
