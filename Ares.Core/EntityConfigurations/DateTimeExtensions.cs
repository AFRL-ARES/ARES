using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.EntityConfigurations;

public static class DateTimeExtensions
{
  public static Timestamp ToTimestampUtc(this DateTime dateTime)
  {
    var utc = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    return utc.ToTimestamp();
  }
}
