using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFC;
internal static class SetpointSourceExtensions
{
  public static string ToStringSource(this SetpointSource source) =>
    source switch
    {
      SetpointSource.Analog => "a",
      SetpointSource.Digital => "s",
      SetpointSource.UnsavedDigital => "u",
      _ => throw new NotSupportedException()
    };

  public static SetpointSource FromStringSource(string source)
  {
    if(source == "a")
      return SetpointSource.Analog;

    if(source == "s")
      return SetpointSource.Digital;

    if(source == "u")
      return SetpointSource.UnsavedDigital;

    return SetpointSource.UnknownSource;
  }
}
