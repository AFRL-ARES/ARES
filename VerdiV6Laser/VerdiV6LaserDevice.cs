using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Factories;
using Ares.Device;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VerdiV6Laser.Commands;
using VerdiV6Laser.Enums;
using VerdiV6Laser.Simulation;

namespace VerdiV6Laser;

public class VerdiV6LaserDevice : AresDevice, IVerdiV6Laser
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly ILaserConnection _connection;

  public VerdiV6LaserDevice(DeviceConnectionInfo connectionInfo) : base(connectionInfo)
  {
    var serialInfo = connectionInfo.SerialConnectionInfo;
    
    if (connectionInfo.Simulated)
    {
      _connection = new SimLaserConnection(serialInfo.PortName);
    }
    else
    {
      _connection = new LaserConnection(serialInfo.PortName);
    }
    
    UpdateState();
  }

  public async Task ActivateLaser()
  {
    await _connection.Send(new SetPowerRequest(DesiredPower));
    await GetLaserPower();
  }

  public async Task DeactivateLaser()
  {
    await _connection.Send(new SetPowerRequest(0.1));
    await GetLaserPower();
  }

  public async Task SetLaserPower(double desiredPower)
  {
    DesiredPower = desiredPower;
    await _connection.Send(new SetPowerRequest(DesiredPower));
    await GetLaserPower();
  }

  public async Task SetLaserShutter(bool shutter)
  {
    await _connection.Send(new SetShutterRequest(shutter));
    await GetLaserShutter();
  }

  public async Task<bool> GetLaserShutter()
  {
    try
    {
      var response = await _connection.Send(new GetShutterRequest());
      Shutter = response.Shutter;
      UpdateState();
    }
    catch (Exception)
    {
      // Handle or log error
    }
    return Shutter;
  }

  public async Task<double> GetLaserPower()
  {
    try
    {
      var response = await _connection.Send(new GetPowerRequest());
      CurrentPower = response.Power;
      UpdateState();
    }
    catch (Exception)
    {
      // Handle or log error
    }
    return CurrentPower;
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      await GetLaserPower();
      await GetLaserShutter();
      UpdateState();
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Laser is active" };
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
    await SetLaserShutter(false);
    await DeactivateLaser();
  }

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(_stateSubject.Value);
  }

  private void UpdateState()
  {
    _stateSubject.OnNext(
      AresStateBuilder.Create()
      .Add("Current Laser Power", CurrentPower)
      .Add("Desired Laser Power", DesiredPower)
      .Add("Shutter", Shutter)
      .Build());
  }

  public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();

  public double CurrentPower { get; private set; } = 0.01;
  public double DesiredPower { get; private set; } = 0.1;
  public bool Shutter { get; private set; } = false;

  public async ValueTask DisposeAsync()
  {
    await _connection.DisposeAsync();
    _stateSubject.OnCompleted();
  }

  public override async Task UpdateSettings(AresStruct settings)
    => await Task.CompletedTask;
  

  public override Task<AresStruct> GetSettings()
    => Task.FromResult(new AresStruct());
  

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = VerdiV6LaserCommand.SetPower.ToString(),
        Description = "Sets the laser power level.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(VerdiV6LaserCommandParameter.LaserPower.ToString(), 
            AresSchemaBuilder.NumberEntry().WithDescription("The desired power level.").Build())
          .Build()
      },
      new()
      {
        Name = VerdiV6LaserCommand.SetShutter.ToString(),
        Description = "Opens or closes the laser shutter.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(VerdiV6LaserCommandParameter.Shutter.ToString(), 
            AresSchemaBuilder.Entry(AresDataType.Boolean).WithDescription("True to open, False to close.").Build())
          .Build()
      },
      new()
      {
        Name = VerdiV6LaserCommand.ActivateLaser.ToString(),
        Description = "Activates the laser with the desired power level."
      },
      new()
      {
        Name = VerdiV6LaserCommand.DeactivateLaser.ToString(),
        Description = "Deactivates the laser by setting it to minimum power."
      }
    });
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if (!Enum.TryParse<VerdiV6LaserCommand>(command, out var laserCommand))
      return new CommandResult { Success = false, Error = $"Unknown command: {command}" };
    

    try
    {
      switch (laserCommand)
      {
        case VerdiV6LaserCommand.SetPower:
          var powerArg = arguments.FirstOrDefault(a => a.ArgName == VerdiV6LaserCommandParameter.LaserPower.ToString());
          if (powerArg != null && powerArg.ArgValue.HasNumberValue)
          {
            await SetLaserPower(powerArg.ArgValue.NumberValue);
            return new CommandResult { Success = true };
          }
          return new CommandResult { Success = false, Error = "Missing or invalid power argument" };

        case VerdiV6LaserCommand.SetShutter:
          var shutterArg = arguments.FirstOrDefault(a => a.ArgName == VerdiV6LaserCommandParameter.Shutter.ToString());
          if (shutterArg != null && shutterArg.ArgValue.HasBoolValue)
          {
            await SetLaserShutter(shutterArg.ArgValue.BoolValue);
            return new CommandResult { Success = true };
          }
          return new CommandResult { Success = false, Error = "Missing or invalid shutter argument" };

        case VerdiV6LaserCommand.ActivateLaser:
          await ActivateLaser();
          return new CommandResult { Success = true };

        case VerdiV6LaserCommand.DeactivateLaser:
          await DeactivateLaser();
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
