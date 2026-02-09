using Google.Protobuf.WellKnownTypes;

namespace UI.Infrastructure.Helpers;

public static class DisplayFormatHelper
{
  public static string ToReadableTimestamp(this Timestamp protoTimestamp) => protoTimestamp.ToDateTime().ToLocalTime().ToString("MM/dd/yyyy hh:mm:ss tt");
}
