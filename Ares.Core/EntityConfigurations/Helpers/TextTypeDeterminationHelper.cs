using System.Text.Json;
using Ares.Datamodel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class TextTypeDeterminationHelper
{
  public static PropertyBuilder<AresValue> HasAresValue(this PropertyBuilder<AresValue> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return value.HasConversion(
        v => JsonSerializer.Serialize(v, settings),
        v => JsonSerializer.Deserialize<AresValue>(v, settings) ?? new AresValue())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<AresDataSchema> HasDataSchema(this PropertyBuilder<AresDataSchema> schema)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schema.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresDataSchema>(s, settings) ?? new AresDataSchema())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<AresDataSchema> HasDataSchemaSimplified(this PropertyBuilder<AresDataSchema> schema)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schema.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresDataSchema>(s, settings) ?? new AresDataSchema())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<AresStruct> HasAresStruct(this PropertyBuilder<AresStruct> aresStruct)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return aresStruct.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresStruct>(s, settings) ?? new AresStruct())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<SchemaEntry> HasAresSchemaEntry(this PropertyBuilder<SchemaEntry> schemaEntry)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schemaEntry.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<SchemaEntry>(s, settings) ?? new SchemaEntry())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<Timestamp> HasTimestamp(this PropertyBuilder<Timestamp> timestamp)
  {
    return timestamp.HasConversion(t => t.ToDateTime(), time => time.ToTimestampUtc());
  }
}
