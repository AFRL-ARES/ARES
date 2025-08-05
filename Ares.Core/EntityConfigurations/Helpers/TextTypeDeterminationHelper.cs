using System.Text.Json;
using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class TextTypeDeterminationHelper
{
  public static string DetermineColumnType()
  {
    var provider = DatabaseRuntimeEnvironment.DatabaseProvider;

    if(provider is null)
      return "TEXT";

    if(provider.Contains("Postgres", StringComparison.CurrentCultureIgnoreCase))
      return "jsonb";

    if(provider.Contains("Sqlite", StringComparison.CurrentCultureIgnoreCase))
      return "TEXT";

    if(provider.Contains("SqlServer", StringComparison.CurrentCultureIgnoreCase))
      return "nvarchar(max)";

    else
      return "TEXT";
  }

  public static PropertyBuilder<AresValue> HasAresValue(this PropertyBuilder<AresValue> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return value.HasConversion(
        v => JsonSerializer.Serialize(v, settings),
        v => JsonSerializer.Deserialize<AresValue>(v, settings) ?? new AresValue())
      .HasColumnType(DetermineColumnType());
  }

  public static PropertyBuilder<AresDataSchema> HasDataSchema(this PropertyBuilder<AresDataSchema> schema)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schema.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresDataSchema>(s, settings) ?? new AresDataSchema())
      .HasColumnType(DetermineColumnType());
  }

  public static PropertyBuilder<AresDataSchemaSimplified> HasDataSchemaSimplified(this PropertyBuilder<AresDataSchemaSimplified> schema)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schema.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresDataSchemaSimplified>(s, settings) ?? new AresDataSchemaSimplified())
      .HasColumnType(DetermineColumnType());
  }

  public static PropertyBuilder<AresStruct> HasAresStruct(this PropertyBuilder<AresStruct> aresStruct)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return aresStruct.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresStruct>(s, settings) ?? new AresStruct())
      .HasColumnType(DetermineColumnType());
  }

  public static PropertyBuilder<SchemaEntry> HasAresSchemaEntry(this PropertyBuilder<SchemaEntry> schemaEntry)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schemaEntry.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<SchemaEntry>(s, settings) ?? new SchemaEntry())
      .HasColumnType(DetermineColumnType());
  }
}
