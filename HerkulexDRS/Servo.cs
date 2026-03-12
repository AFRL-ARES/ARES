using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using HerkulexDRS.Commands;
using HerkulexDRS.Connection;
using HerkulexDRS.Enums;
using HerkulexDRS.Responses;
using HerkulexDRS.Simulation;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace HerkulexDRS;

public class Servo : AresDevice, IServo
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly IServoConnection _connection;
  private int _servoId;

  public Servo(DeviceConnectionInfo connectionInfo) : base(connectionInfo)
  {
    _servoId = (int)(connectionInfo.DeviceSettings.Fields.GetValueOrDefault("ServoId")?.NumberValue ?? 1);
    
    var serialInfo = connectionInfo.SerialConnectionInfo;
    if(connectionInfo.Simulated)
    {
      _connection = new SimServoConnection(serialInfo.PortName);
    }
    else
    {
      _connection = new ServoConnection(serialInfo.PortName);
    }
  }

  public async Task PistonDown()
  {
    PistonRaised = false;
    await _connection.Send(new PistonDownCommand());
    UpdateState();
  }

  public async Task PistonUp()
  {
    PistonRaised = true;
    await _connection.Send(new PistonUpCommand());
    UpdateState();
  }

  public async Task ResetServo()
  {
    await _connection.Send(new RebootCommand());
  }

  public async Task<GetPositionResponse> GetPosition()
  {
    var response = await _connection.Send(new GetPositionCommand());
    return response;
  }

  public async ValueTask DisposeAsync()
  {
    await _connection.DisposeAsync();
    _stateSubject.OnCompleted();
  }

  protected async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      await _connection.Send(new RebootCommand());
      return new SerialDeviceValidationResult(true);
    }
    catch(Exception e)
    {
      return new SerialDeviceValidationResult(false, e.Message);
    }
  }

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(_stateSubject.Value);
  }

  private void UpdateState()
  {
    _stateSubject.OnNext(
      AresStateBuilder.Create()
      .Add("Piston Raised", PistonRaised)
      .Add("ServoId", _servoId)
      .Build());
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    //Disengage Servo
    await PistonDown();
  }

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();
  public bool PistonRaised { get; private set; } = false;

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      await ResetServo();
      UpdateState();
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Servo is active" };
      return true;
    }
    catch (Exception e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to activate: {e.Message}" };
      return false;
    }
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if (!Enum.TryParse<ServoCommand>(command, out var servoCommand))
    {
      return new CommandResult { Success = false, Error = $"Unknown command: {command}" };
    }

    try
    {
      switch (servoCommand)
      {
        case ServoCommand.GetPosition:
          var data = await GetPosition();
          return new CommandResult { Result = AresValueHelper.CreateNumber(data.Position), Success = true };

        case ServoCommand.GoUp:
          await ResetServo();
          await Task.Delay(TimeSpan.FromSeconds(4), token);
          await PistonUp();
          return new CommandResult { Success = true };

        case ServoCommand.GoDown:
          await ResetServo();
          await Task.Delay(TimeSpan.FromSeconds(4), token);
          await PistonDown();
          return new CommandResult { Success = true };

        case ServoCommand.Reset:
          await ResetServo();
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

  public override async Task UpdateSettings(AresStruct settings)
  {
    if (settings.Fields.TryGetValue("ServoId", out var idVal) && idVal.HasNumberValue)
    {
      _servoId = (int)idVal.NumberValue;
      UpdateState();
    }
    await Task.CompletedTask;
  }

  public override Task<AresStruct> GetSettings()
  {
    return Task.FromResult(AresStructHelper.CreateNumberStruct("ServoId", (double)_servoId));
  }

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = ServoCommand.Reset.ToString(),
        Description = "Forces the servo to re-initialize its own state."
      },
      new()
      {
        Name = ServoCommand.GetPosition.ToString(),
        Description = "Determines the starting position of the servo device.",
        OutputSchema = AresSchemaBuilder.NumberEntry().WithDescription("Servo Position").Build()
      },
      new()
      {
        Name = ServoCommand.GoUp.ToString(),
        Description = "Moves the servo to its upward (closed) position."
      },
      new()
      {
        Name = ServoCommand.GoDown.ToString(),
        Description = "Moves the servo to its downward (open) position."
      }
    });
  }
}
