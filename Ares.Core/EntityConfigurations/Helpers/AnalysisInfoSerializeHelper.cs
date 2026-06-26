using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Google.Protobuf.Collections;
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

  public static PropertyBuilder<AnalysisResponse> HasAnalysis(this PropertyBuilder<AnalysisResponse> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<AnalysisResponse>(v, settings) ?? new AnalysisResponse())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<RepeatedField<Objective>> HasObjectives(this PropertyBuilder<RepeatedField<Objective>> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<RepeatedField<Objective>>(v, settings) ?? new RepeatedField<Objective>())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
