using Ares.Datamodel.Visualizing.Local;
using Google.Protobuf.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class VisualizationSerializeHelper
{
  public static PropertyBuilder<RepeatedField<VisualizationPath>> HasVisualizationPath(this PropertyBuilder<RepeatedField<VisualizationPath>> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<RepeatedField<VisualizationPath>>(v, settings) ?? new RepeatedField<VisualizationPath>())
    .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
