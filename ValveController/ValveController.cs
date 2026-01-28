using Ares.Datamodel;
using Ares.Device;
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
    if(response != null)
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

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      //Activate the relay, then send our status request to confirm proper activation
      await EnableRelays();
      await GetRelayStatus();

      return new SerialDeviceValidationResult(true);
    }

    catch(Exception ex)
    {
      return new SerialDeviceValidationResult(false, ex.Message);
    }
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await DisengageRelayOne();
    await DisengageRelayTwo();
  }

  public async Task EngageRelayOne()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new EngageRelayOneCommand());
    RelayOneEngaged = true;
  }

  public async Task EngageRelayTwo()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new EngageRelayTwoCommand());
    RelayTwoEngaged = true;
  }

  public async Task DisengageRelayOne()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new DisengageRelayOneCommand());
    RelayOneEngaged = false;
  }

  public async Task DisengageRelayTwo()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new DisengageRelayTwoCommand());
    RelayOneEngaged = false;
  }

  public async Task EnableRelays()
  {
    await Connection.Send(new EnterCommandModeCommand());
    await Connection.Send(new EnableAllDevicesCommand());
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(
      AresStateBuilder.Create()
      .Add("Relay One Engaged", RelayOneEngaged)
      .Add("Relay Two Engaged", RelayTwoEngaged)
      .Build());
  }

  public bool RelayOneEngaged { get; set; } = false;

  public bool RelayTwoEngaged { get; set; } = false;
}
