namespace TicStepperController.UI.State;

public class TicStepperState
{
  public string OperationState { get; set; } = string.Empty;
  public int CurrentPosition { get; set; }
  public int TargetPosition { get; set; }
  public string StepMode { get; set; } = string.Empty;
  public uint MaxAcceleration { get; set; }
  public uint MaxDeceleration { get; set; }
  public uint MaxSpeed { get; set; }
  public uint CurrentLimit { get; set; }
  public uint CustomStepSize { get; set; }
  public uint StartingSpeed { get; set; }
  public TicMiscFlags MiscFlags { get; set; } = new();
  public TicErrors Errors { get; set; } = new();
}

public class TicMiscFlags
{
  public bool Energized { get; set; }
  public bool PositionUncertain { get; set; }
  public bool ForwardLimitActive { get; set; }
  public bool ReverseLimitActive { get; set; }
  public bool HomingActive { get; set; }
}

public class TicErrors
{
  public bool IntentionallyDeEnergized { get; set; }
  public bool MotorDriverError { get; set; }
  public bool LowVin { get; set; }
  public bool KillSwitchActive { get; set; }
  public bool RequiredInputsInvalid { get; set; }
  public bool SerialError { get; set; }
  public bool CommandTimeout { get; set; }
  public bool SafeStartViolation { get; set; }
  public bool ErrLineHigh { get; set; }
}
