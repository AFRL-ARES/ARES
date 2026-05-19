using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
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

  public static PropertyBuilder<AnalysisRequest> HasAnalysisRequest(this PropertyBuilder<AnalysisRequest> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<AnalysisRequest>(v, settings) ?? new AnalysisRequest())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<Analysis> HasAnalysis(this PropertyBuilder<Analysis> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<Analysis>(v, settings) ?? new Analysis())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
