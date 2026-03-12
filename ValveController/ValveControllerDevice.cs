using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using ValveController.Commands;
using ValveController.Commands.RelayOne;
using ValveController.Commands.RelayTwo;
using ValveController.Commands.Responses;
using ValveController.Connection;
using ValveController.Enums;
using ValveController.Interfaces;
using ValveController.Simulation;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ValveController;

public class ValveControllerDevice : AresDevice, IValveController
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly IValveControllerConnection _connection;

  public ValveControllerDevice(DeviceConnectionInfo connectionInfo) : base(connectionInfo)
  {
    var serialInfo = connectionInfo.SerialConnectionInfo;
    
    if (connectionInfo.Simulated)
    {
      _connection = new SimValveControllerConnection(serialInfo.PortName);
    }
    else
    {
      _connection = new ValveControllerConnection(serialInfo.PortName);
    }
  }

  public async Task<RelayStatusResponse> GetRelayStatus()
  {
    await _connection.Send(new EnterCommandModeCommand());
    var response = await _connection.Send(new GetRelayStatusCommand());
    
    if (response != null)
    {
      RelayOneEngaged = response.RelayOneOn;
      RelayTwoEngaged = response.RelayTwoOn;
      UpdateState();
    }
    else
    {
      throw new Exception("No Status Response from Relay Board!");
    }

    return response;
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      await EnableRelays();
      await GetRelayStatus();
      UpdateState();
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Valve Controller is active" };
      return true;
    }
    catch (Exception e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to activate: {e.Message}" };
      return false;
    }
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    await DisengageRelayOne();
    await DisengageRelayTwo();
  }

  public async Task EngageRelayOne()
  {
    await _connection.Send(new EnterCommandModeCommand());
    await _connection.Send(new EngageRelayOneCommand());
    RelayOneEngaged = true;
    UpdateState();
  }

  public async Task EngageRelayTwo()
  {
    await _connection.Send(new EnterCommandModeCommand());
    await _connection.Send(new EngageRelayTwoCommand());
    RelayTwoEngaged = true;
    UpdateState();
  }

  public async Task DisengageRelayOne()
  {
    await _connection.Send(new EnterCommandModeCommand());
    await _connection.Send(new DisengageRelayOneCommand());
    RelayOneEngaged = false;
    UpdateState();
  }

  public async Task DisengageRelayTwo()
  {
    await _connection.Send(new EnterCommandModeCommand());
    await _connection.Send(new DisengageRelayTwoCommand());
    RelayTwoEngaged = false;
    UpdateState();
  }

  public async Task EnableRelays()
  {
    await _connection.Send(new EnterCommandModeCommand());
    await _connection.Send(new EnableAllDevicesCommand());
  }

  public async ValueTask DisposeAsync()
  {
    await _connection.DisposeAsync();
    _stateSubject.OnCompleted();
  }

  public bool RelayOneEngaged { get; private set; } = false;

  public bool RelayTwoEngaged { get; private set; } = false;

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(_stateSubject.Value);
  }

  private void UpdateState()
  {
    _stateSubject.OnNext(
      AresStateBuilder.Create()
      .Add("Relay One Engaged", RelayOneEngaged)
      .Add("Relay Two Engaged", RelayTwoEngaged)
      .Build());
  }

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();

  public override async Task UpdateSettings(AresStruct settings)
  {
    await Task.CompletedTask;
  }

  public override Task<AresStruct> GetSettings()
  {
    return Task.FromResult(new AresStruct());
  }

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = ValveControllerCommand.GetRelayStatus.ToString(),
        Description = "Determines the status of the relay board.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct)
        .WithDescription("A custom struct containing two boolean flags")
        .WithStructSchema(
        AresSchemaBuilder.Empty()
            .AddEntry("RelayOne", AresSchemaBuilder.Entry(AresDataType.Boolean)
                .WithDescription("Is Relay One Active")
                .Build())
            .AddEntry("RelayTwo", AresSchemaBuilder.Entry(AresDataType.Boolean)
                .WithDescription("Is Relay Two Active")
                .Build())
            .Build()).Build()
      },
      new()
      {
        Name = ValveControllerCommand.EngageRelayOne.ToString(),
        Description = "Engage relay one."
      },
      new()
      {
        Name = ValveControllerCommand.EngageRelayTwo.ToString(),
        Description = "Engage relay two."
      },
      new()
      {
        Name = ValveControllerCommand.DisengageRelayOne.ToString(),
        Description = "Disengage relay one."
      },
      new()
      {
        Name = ValveControllerCommand.DisengageRelayTwo.ToString(),
        Description = "Disengage relay two."
      },
      new()
      {
        Name = ValveControllerCommand.EnableRelays.ToString(),
        Description = "Enable all relays."
      }
    });
  }

  public override async Task<CommandResult> ExecuteCommand(string commandName, List<DeviceCommandArgument> arguments, CancellationToken ct)
  {
    if (!Enum.TryParse<ValveControllerCommand>(commandName, out var deviceCommandEnum))
    {
      return new CommandResult { Success = false, Error = $"Unknown command: {commandName}" };
    }

    try
    {
      switch (deviceCommandEnum)
      {
        case ValveControllerCommand.GetRelayStatus:
          var data = await GetRelayStatus();
          return new CommandResult
          {
            Result = AresValueHelper.CreateStruct(
              AresStructHelper.CreateBoolStruct("RelayOne", data.RelayOneOn)
              .AddBool("RelayTwo", data.RelayTwoOn)),
            Success = true
          };

        case ValveControllerCommand.EngageRelayOne:
          await EngageRelayOne();
          return new CommandResult { Success = true };

        case ValveControllerCommand.EngageRelayTwo:
          await EngageRelayTwo();
          return new CommandResult { Success = true };

        case ValveControllerCommand.DisengageRelayOne:
          await DisengageRelayOne();
          return new CommandResult { Success = true };

        case ValveControllerCommand.DisengageRelayTwo:
          await DisengageRelayTwo();
          return new CommandResult { Success = true };

        case ValveControllerCommand.EnableRelays:
          await EnableRelays();
          return new CommandResult { Success = true };

        default:
          return new CommandResult { Success = false, Error = "Command not supported" };
      }
    }
    catch (Exception e)
    {
      return new CommandResult { Success = false, Error = e.Message };
    }
  }
}
