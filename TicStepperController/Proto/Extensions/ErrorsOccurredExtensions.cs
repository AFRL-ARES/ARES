using TicStepperController.Messaging;

namespace TicStepperController.Proto.Extensions;
internal static class ErrorsOccurredExtensions
{
  public static ErrorsOccurred ToProto(this Commands.Responses.ErrorsOccurred errors)
  {
    var protoErrors = new ErrorsOccurred
    {
      EncoderSkip = errors.EncoderSkip,
      SerialCrc = errors.SerialCrc,
      SerialFormat = errors.SerialFormat,
      SerialFraming = errors.SerialFraming,
      SerialRxOverrun = errors.SerialRxOverrun
    };

    return protoErrors;
  }
}
