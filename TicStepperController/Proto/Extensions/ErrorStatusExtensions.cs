using TicStepperController.Messaging;

namespace TicStepperController.Proto.Extensions;
internal static class ErrorStatusExtensions
{
  public static ErrorStatus ToProto(this Commands.Responses.ErrorStatus status)
  {
    var protoStatus = new ErrorStatus
    {
      CommandTimeout = status.CommandTimeout,
      ErrLineHigh = status.ErrLineHigh,
      IntentionallyDeEnergized = status.IntentionallyDeEnergized,
      KillSwitchActive = status.KillSwitchActive,
      LowVin = status.LowVin,
      MotorDriverError = status.MotorDriverError,
      RequiredInputInvalid = status.RequiredInputInvalid,
      SafeStartViolation = status.SafeStartViolation,
      SerialError = status.SerialError
    };

    return protoStatus;
  }
}
