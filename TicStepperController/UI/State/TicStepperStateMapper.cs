using Ares.Datamodel;

namespace TicStepperController.UI.State;

public static class TicStepperStateMapper
{
  public static TicStepperState FromAresStruct(AresStruct state)
  {
    var model = new TicStepperState
    {
      OperationState = state.Fields.GetValueOrDefault("OperationState")?.StringValue ?? "Unknown",
      CurrentPosition = (int)(state.Fields.GetValueOrDefault("CurrentPosition")?.NumberValue ?? 0),
      TargetPosition = (int)(state.Fields.GetValueOrDefault("TargetPosition")?.NumberValue ?? 0),
      StepMode = state.Fields.GetValueOrDefault("StepMode")?.StringValue ?? "Unknown",
      MaxAcceleration = (uint)(state.Fields.GetValueOrDefault("MaxAcceleration")?.NumberValue ?? 0),
      MaxDeceleration = (uint)(state.Fields.GetValueOrDefault("MaxDeceleration")?.NumberValue ?? 0),
      MaxSpeed = (uint)(state.Fields.GetValueOrDefault("MaxSpeed")?.NumberValue ?? 0),
      CurrentLimit = (uint)(state.Fields.GetValueOrDefault("CurrentLimit")?.NumberValue ?? 0),
      CustomStepSize = (uint)(state.Fields.GetValueOrDefault("CustomStepSize")?.NumberValue ?? 0),
      StartingSpeed = (uint)(state.Fields.GetValueOrDefault("StartingSpeed")?.NumberValue ?? 0)
    };

    if (state.Fields.TryGetValue("MiscFlags", out var miscVal) && miscVal.StructValue != null)
    {
      var fields = miscVal.StructValue.Fields;
      model.MiscFlags = new TicMiscFlags
      {
        Energized = fields.GetValueOrDefault("Energized")?.BoolValue ?? false,
        PositionUncertain = fields.GetValueOrDefault("PositionUncertain")?.BoolValue ?? false,
        ForwardLimitActive = fields.GetValueOrDefault("ForwardLimitActive")?.BoolValue ?? false,
        ReverseLimitActive = fields.GetValueOrDefault("ReverseLimitActive")?.BoolValue ?? false,
        HomingActive = fields.GetValueOrDefault("HomingActive")?.BoolValue ?? false
      };
    }

    if (state.Fields.TryGetValue("Errors", out var errorsVal) && errorsVal.StructValue != null)
    {
      var fields = errorsVal.StructValue.Fields;
      model.Errors = new TicErrors
      {
        IntentionallyDeEnergized = fields.GetValueOrDefault("IntentionallyDeEnergized")?.BoolValue ?? false,
        MotorDriverError = fields.GetValueOrDefault("MotorDriverError")?.BoolValue ?? false,
        LowVin = fields.GetValueOrDefault("LowVin")?.BoolValue ?? false,
        KillSwitchActive = fields.GetValueOrDefault("KillSwitchActive")?.BoolValue ?? false,
        RequiredInputsInvalid = fields.GetValueOrDefault("RequiredInputsInvalid")?.BoolValue ?? false,
        SerialError = fields.GetValueOrDefault("SerialError")?.BoolValue ?? false,
        CommandTimeout = fields.GetValueOrDefault("CommandTimeout")?.BoolValue ?? false,
        SafeStartViolation = fields.GetValueOrDefault("SafeStartViolation")?.BoolValue ?? false,
        ErrLineHigh = fields.GetValueOrDefault("ErrLineHigh")?.BoolValue ?? false
      };
    }

    return model;
  }
}
