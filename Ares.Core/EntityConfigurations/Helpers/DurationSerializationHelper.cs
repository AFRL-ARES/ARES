using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class DurationSerializationHelper
{
  public static PropertyBuilder<Duration> HasDuration(this PropertyBuilder<Duration> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<Duration>(v, settings) ?? new Duration())
    .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
