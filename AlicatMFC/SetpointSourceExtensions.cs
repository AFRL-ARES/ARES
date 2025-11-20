using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFC;
internal static class SetpointSourceExtensions
{
  public static string ToStringSource(this SetpointSource source) =>
    source switch
    {
      SetpointSource.Analog => "A",
      SetpointSource.Digital => "S",
      SetpointSource.UnsavedDigital => "U",
      _ => throw new NotSupportedException()
    };

  public static SetpointSource FromStringSource(string source)
  {
    if(source == "A")
      return SetpointSource.Analog;

    if(source == "S")
      return SetpointSource.Digital;

    if(source == "U")
      return SetpointSource.UnsavedDigital;

    return SetpointSource.UnknownSource;
  }
}
