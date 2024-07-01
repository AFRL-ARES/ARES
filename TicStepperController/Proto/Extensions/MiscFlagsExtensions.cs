using TicStepperController.Messaging;

namespace TicStepperController.Proto.Extensions;
internal static class MiscFlagsExtensions
{
  public static MiscFlags ToProto(this Commands.Responses.MiscFlags flags)
  {
    var protoFlags = new MiscFlags
    {
      Energized = flags.Energized,
      ForwardLimitActive = flags.ForwardLimitActive,
      HomingActive = flags.HomingActive,
      PositionUncertain = flags.PositionUncertain,
      ReverseLimitActive = flags.ReverseLimitActive
    };

    return protoFlags;
  }
}
