using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Templates;
using Ares.Device;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UnitsNet;

namespace Ares.Core.CoreDevice;

public class AresCoreDevice : AresDevice
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());

  public AresCoreDevice() : base("ARES", "ARES-CORE-DEVICE")
  {
    Status = new DeviceOperationalStatus()
    {
      OperationalState = OperationalState.Active
    };

    StateStream = _stateSubject.AsObservable();
    CommandDescriptors = BuildCommandDescriptors();
  }

  public override Task<bool> Activate(CancellationToken ct)
  {
    return Task.FromResult(true);
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public override Task<AresStruct> GetState()
  {
    return Task.FromResult(new AresStruct());
  }

  public Task Sleep(TimeSpan timeSpan, CancellationToken ct)
  {
    return Task.Delay(timeSpan, ct);
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    var result = new CommandResult();

    if(!Enum.TryParse(command, out AresCoreDeviceCommand commandEnum))
      return new CommandResult { Error = "Unrecognized Command Received in Core Device!", Success = false };

    var durationParam = arguments.FirstOrDefault(param => param.ArgName == AresCoreDeviceCommandParameter.Duration.ToString())?.ArgValue;
    
    switch(commandEnum)
    {
      case AresCoreDeviceCommand.SleepForMilliseconds:
        if(durationParam is not null && durationParam.HasNumberValue)
        {
          var millisecondsDuration = UnitsNet.Duration.FromMilliseconds(durationParam.NumberValue);
          await Sleep(millisecondsDuration.ToTimeSpan(), token);
          result.Success = true;
          break;
        }

        else
        {
          result.Error = "Cannot use Sleep command without specifying a duration!";
          result.Success = false;
          break;
        }
      case AresCoreDeviceCommand.SleepForSeconds:
        if(durationParam is not null && durationParam.HasNumberValue)
        {
          var secondsDuration = Duration.FromSeconds(durationParam.NumberValue);
          await Sleep(secondsDuration.ToTimeSpan(), token);
          result.Success = true;
          break;
        }

        else
        {
          result.Error = "Cannot use Sleep command without specifying a duration!";
          result.Success = false;
          break;
        }
      case AresCoreDeviceCommand.SleepForMinutes:
        if(durationParam is not null && durationParam.HasNumberValue)
        {
          var minutesDuration = Duration.FromMinutes(durationParam.NumberValue);
          await Sleep(minutesDuration.ToTimeSpan(), token);
          result.Success = true;
          break;
        }

        else
        {
          result.Error = "Cannot use Sleep command without specifying a duration!";
          result.Success = false;
          break;
        }
      case AresCoreDeviceCommand.WaitForUser:
        result.Success = true;
        result.AwaitUserInput = true;
        break;
    }

    return result;
  }

  private List<DeviceCommandDescriptor> BuildCommandDescriptors()
  {
    return
    [
      new()
      {
        Name = AresCoreDeviceCommand.SleepForMilliseconds.ToString(),
        Description = "Sleep for a given amount of milliseconds",
        InputSchema = AresSchemaBuilder.Create(AresCoreDeviceCommandParameter.Duration.ToString(), AresDataType.Number).Build()
      },

      new()
      {
        Name = AresCoreDeviceCommand.SleepForSeconds.ToString(),
        Description = "Sleep for a given amount of seconds",
        InputSchema = AresSchemaBuilder.Create(AresCoreDeviceCommandParameter.Duration.ToString(), AresDataType.Number).Build(),
      },

      new()
      {
        Name = AresCoreDeviceCommand.SleepForMinutes.ToString(),
        Description = "Sleep for a given amount of minutes",
        InputSchema = AresSchemaBuilder.Create(AresCoreDeviceCommandParameter.Duration.ToString(), AresDataType.Number).Build()
      },
      
      new()
      {
        Name = AresCoreDeviceCommand.WaitForUser.ToString(),
        Description = "Have ARES request user confirmation before continuing."
      }
    ];
  }

  public override Task UpdateSettings(AresStruct settings)
  {
    return Task.FromResult(new AresStruct());
  }

  public override IObservable<AresStruct> StateStream { get; }
}
