using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class ParameterSourceSerializeHelper
{
  public static PropertyBuilder<ParameterSourcePersistence?> HasParameterSource(this PropertyBuilder<ParameterSourcePersistence?> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();

    return value.HasConversion(
      v => JsonSerializer.Serialize(v, settings),
      v => string.IsNullOrWhiteSpace(v)
        ? null
        : JsonSerializer.Deserialize<ParameterSourcePersistence>(v, settings))
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }
}
