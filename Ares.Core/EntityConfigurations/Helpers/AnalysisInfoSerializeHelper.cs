using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class AnalysisInfoSerializeHelper
{
  public static PropertyBuilder<AnalyzerInfo> HasAnalyzerInfo(this PropertyBuilder<AnalyzerInfo> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    
    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<AnalyzerInfo>(v, settings) ?? new AnalyzerInfo())
    .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
