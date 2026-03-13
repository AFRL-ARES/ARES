using Ares.Toolkit.Serial.Commands;
using TicStepperController.Enums;

namespace TicStepperController.Responses;

public class ErrorStatus : SerialResponse
{
  public ErrorStatus(bool intentionallyDeEnergized, bool motorDriverError, bool lowVin, bool killSwitchActive, bool requiredInputInvalid, bool serialError, bool commandTimeout, bool safeStartViolation, bool errLineHigh)
  {
    IntentionallyDeEnergized = intentionallyDeEnergized;
    MotorDriverError = motorDriverError;
    LowVin = lowVin;
    KillSwitchActive = killSwitchActive;
    RequiredInputInvalid = requiredInputInvalid;
    SerialError = serialError;
    CommandTimeout = commandTimeout;
    SafeStartViolation = safeStartViolation;
    ErrLineHigh = errLineHigh;
  }
  
  public override string ToString() => $"Errors: {(MotorDriverError ? "MotorDriver " : "")}{(SerialError ? "Serial " : "")}{(CommandTimeout ? "Timeout " : "")}";

  public bool IntentionallyDeEnergized { get; }
  public bool MotorDriverError { get; }
  public bool LowVin { get; }
  public bool KillSwitchActive { get; }
  public bool RequiredInputInvalid { get; }
  public bool SerialError { get; }
  public bool CommandTimeout { get; }
  public bool SafeStartViolation { get; }
  public bool ErrLineHigh { get; }
}

public class ErrorsOccurred : SerialResponse
{
  public ErrorsOccurred(bool serialFraming, bool serialRxOverrun, bool serialFormat, bool serialCrc, bool encoderSkip)
  {
    SerialFraming = serialFraming;
    SerialRxOverrun = serialRxOverrun;
    SerialFormat = serialFormat;
    SerialCrc = serialCrc;
    EncoderSkip = encoderSkip;
  }

  public bool SerialFraming { get; }
  public bool SerialRxOverrun { get; }
  public bool SerialFormat { get; }
  public bool SerialCrc { get; }
  public bool EncoderSkip { get; }
}

public class MiscFlags : SerialResponse
{
  public MiscFlags(bool energized, bool positionUncertain, bool forwardLimitActive, bool reverseLimitActive, bool homingActive)
  {
    Energized = energized;
    PositionUncertain = positionUncertain;
    ForwardLimitActive = forwardLimitActive;
    ReverseLimitActive = reverseLimitActive;
    HomingActive = homingActive;
  }

  public bool Energized { get; }
  public bool PositionUncertain { get; }
  public bool ForwardLimitActive { get; }
  public bool ReverseLimitActive { get; }
  public bool HomingActive { get; }
}

public class OperationStateResponse : SerialResponse
{
  public OperationStateResponse(OperationState state) => State = state;

  public OperationState State { get; }
}

public class CurrentPositionResponse : SerialResponse
{
  public CurrentPositionResponse(int position) => Position = position;

  public int Position { get; }
}

public class TargetPositionResponse : SerialResponse
{
  public TargetPositionResponse(int position) => Position = position;

  public int Position { get; }
}

public class StepModeResponse : SerialResponse
{
  public StepModeResponse(StepMode stepMode) => StepMode = stepMode;

  public StepMode StepMode { get; }
}

public class Uint32Response : SerialResponse
{
  public Uint32Response(uint value) => Value = value;

  public uint Value { get; }
}
