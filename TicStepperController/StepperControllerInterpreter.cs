using System.Reactive.Linq;
using Ares.Datamodel;
using Ares.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace TicStepperController;
public class StepperControllerInterpreter : DeviceCommandInterpreter<IStepperController, StepperControllerCommand>
{
  public StepperControllerInterpreter(IStepperController device) : base(device)
  {
  }

  protected override CommandMetadata[] CommandsToMetadatas()
  {
    return new CommandMetadata[]
    {
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.Reset.ToString(),
        Description = "This command makes the Tic forget most parts of its current state. Specifically, it does the following: " +
        "Reloads all settings from the Tic’s non-volatile memory and discards any temporary changes to the settings previously made with serial commands(this applies to the step mode, current limit, decay mode, max speed, starting speed, max acceleration, and max deceleration settings)." +
        "Abruptly halts the motor. " +
        "Resets the motor driver. " +
        "Sets the Tic’s operation state to “reset”." +
        "Clears the last movement command and the current position. " +
        "Clears the encoder position. " +
        "Clears the serial and “command timeout” errors and the “errors occurred” bits. " +
        "Enters safe start if configured to do so"
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.EnterSafeStart.ToString(),
        Description = "If safe start is enabled and the control mode is Serial / I²C / USB, RC speed, analog speed, or encoder speed, this command causes the Tic to stop the motor (using the configured soft error response behavior) and set its “safe start violation” error bit. If safe start is disabled, or if the Tic is not in one of the listed modes, this command will cause a brief interruption in motor control (during which the soft error response behavior will be triggered) but otherwise have no effect."
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.ExitSafeStart.ToString(),
        Description = "In Serial / I²C / USB control mode, this command causes the “safe start violation” error to be cleared for 200 ms. If there are no other errors, this allows the system to start up."
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.HaltAndHold.ToString(),
        Description = "This command stops the motor abruptly without respecting the deceleration limit. Besides stopping the motor, this command also sets the “position uncertain” flag (because the abrupt stop might cause steps to be missed), sets the input state to “halt”, and clears the “input after scaling” variable."
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.HaltAndSetPosition.ToString(),
        Description = "This command stops the motor abruptly without respecting the deceleration limit and sets the “Current position” variable, which represents what position the Tic currently thinks the motor is in. Besides stopping the motor and setting the current position, this command also clears the “position uncertain” flag, sets the input state to “halt”, and clears the “input after scaling” variable.",
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = StepperControllerCommandParameter.Position.ToString() , Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) } }
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.SetTargetPosition.ToString(),
        Description = "This command sets the target position of the Tic, in microsteps.",
        ParameterMetadatas = { new ParameterMetadata { Index = 0, Name = StepperControllerCommandParameter.Position.ToString(), Schema = AresSchemaHelper.CreateSchemaEntry(AresDataType.Number, true) } }
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.NextStep.ToString(),
        Description = "Goes to the next step in user defined microsteps."
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.PreviousStep.ToString(),
        Description = "Goes to the previous step in user defined microsteps."
      },
      new()
      {
        DeviceName = Device.Name,
        Name = StepperControllerCommand.HalfStep.ToString(),
        Description = "Advances the controller forward half of it's defined step size."
      }
    };
  }

  protected override async Task<DeviceCommandResult> ParseAndPerformDeviceAction(StepperControllerCommand deviceCommandEnum,
    Parameter[] parameters,
    CommandMetadata metadata,
    CancellationToken cancellationToken)
  {
    var result = new DeviceCommandResult();
    result.Success = true;

    var timeout = TimeSpan.FromSeconds(10);

    switch(deviceCommandEnum)
    {
      case StepperControllerCommand.Reset:
        await Device.Reset();
        break;
      case StepperControllerCommand.EnterSafeStart:
        await Device.EnterSafeStart();
        break;
      case StepperControllerCommand.ExitSafeStart:
        await Device.ExitSafeStart();
        break;
      case StepperControllerCommand.HaltAndHold:
        await Device.HaltAndHold();
        break;
      case StepperControllerCommand.HaltAndSetPosition:
        var positionParam = parameters.First().Value.Value;

        if(!positionParam.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The Stepper Controller command HaltAndSetPosition requires a number as a parameter, but none was provided!";
          break;
        }

        await Device.HaltAndSetPosition((int)positionParam.NumberValue);
        break;
      case StepperControllerCommand.SetTargetPosition:
        var targetPosition = parameters.First().Value.Value;

        if(!targetPosition.HasNumberValue)
        {
          result.Success = false;
          result.Error = "The Stepper Controller command SetTargetPosition requires a number as a parameter, but none was provided!";
          break;
        }
       
        await Device.SetTargetPosition((int)targetPosition.NumberValue);
        try
        {
          await Device.WaitForTargetPosition(timeout);
        }
        catch(TimeoutException)
        {
          var state = await Device.StateStream.FirstAsync();
          result.Success = false;
          result.Error = $"Stepper Motor {Device.Name} did not achieve target position of {targetPosition} within {timeout}. Current position: {state.CurrentPosition}";
        }
        break;
      case StepperControllerCommand.NextStep:
        try
        {
          await Device.NextStep();
        }
        catch(TimeoutException)
        {
          var state = await Device.StateStream.FirstAsync();
          result.Success = false;
          result.Error = $"Stepper Motor {Device.Name} did not achieve target position of {state.TargetPosition} within {timeout}. Current position: {state.CurrentPosition}";
        }
        break;
      case StepperControllerCommand.PreviousStep:
        try
        {
          await Device.PreviousStep();
        }
        catch(TimeoutException)
        {
          var state = await Device.StateStream.FirstAsync();
          result.Success = false;
          result.Error = $"Stepper Motor {Device.Name} did not achieve target position of {state.TargetPosition} within {timeout}. Current position: {state.CurrentPosition}";
        }
        break;
      case StepperControllerCommand.HalfStep:
        try
        {
          await Device.HalfStep();
        }
        catch(TimeoutException)
        {
          var state = await Device.StateStream.FirstAsync();
          result.Success = false;
          result.Error = $"Stepper Motor {Device.Name} did not achieve target position of {state.TargetPosition} within {timeout}. Current position: {state.CurrentPosition}";
        }
        break;
    }

    return result;
  }
}
