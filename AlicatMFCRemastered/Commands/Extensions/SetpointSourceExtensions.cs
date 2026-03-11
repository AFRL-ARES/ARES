using AlicatMFCRemastered.Enums;

namespace AlicatMFCRemastered.Commands.Extensions;
internal static class SetpointSourceExtensions
{
  public static string ToStringSource(this MfcSetpointSourceEnum source) =>
    source switch
    {
      MfcSetpointSourceEnum.Analog => "A",
      MfcSetpointSourceEnum.Digital => "S",
      MfcSetpointSourceEnum.UnsavedDigital => "U",
      _ => throw new NotSupportedException()
    };

  public static MfcSetpointSourceEnum FromStringSource(string source)
  {
    if(source == "A")
      return MfcSetpointSourceEnum.Analog;

    if(source == "S")
      return MfcSetpointSourceEnum.Digital;

    if(source == "U")
      return MfcSetpointSourceEnum.UnsavedDigital;

    return MfcSetpointSourceEnum.UnknownSource;
  }
}
