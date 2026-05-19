using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class PlanningSerializationHelpers
{
  public static PropertyBuilder<PlanningRequest> HasPlanningRequest(this PropertyBuilder<PlanningRequest> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<PlanningRequest>(v, settings) ?? new PlanningRequest())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<PlanningResponse> HasPlanningResponse(this PropertyBuilder<PlanningResponse> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => JsonSerializer.Deserialize<PlanningResponse>(v, settings) ?? new PlanningResponse())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
