using Ares.Datamodel.Visualizing.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class VisualizationSerializeHelper
{
  public static PropertyBuilder<VisualizationPath> HasVisualizationPath(this PropertyBuilder<VisualizationPath> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<VisualizationPath>(v, settings) ?? new VisualizationPath())
    .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
