using System.Text.Json;
using Ares.Datamodel;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class EfCoreValueConverters
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

  public static PropertyBuilder<RepeatedField<T>> HasSerializedRepeatedField<T>(this PropertyBuilder<RepeatedField<T>> builder)
  {
    var converter = new ValueConverter<RepeatedField<T>, string>(
      v => SerializeRepeatedFieldToJson(v),
      v => DeserializeRepeatedFieldFromJson<T>(v));

    return builder.HasConversion(converter);
  }

  public static PropertyBuilder<MapField<TKey, TValue>> HasSerializedMap<TKey, TValue>(this PropertyBuilder<MapField<TKey, TValue>> builder) where TKey : notnull
  {
    var converter = new ValueConverter<MapField<TKey, TValue>, string>(
      v => SerializeMapToJson(v),
      v => DeserializeMapFromJson<TKey, TValue>(v));

    return builder.HasConversion(converter);
  }

  private static string SerializeRepeatedFieldToJson<T>(RepeatedField<T> items)
  {
    return JsonSerializer.Serialize(items.ToArray(), JsonSerializerOptions.Default);
  }

  private static string SerializeMapToJson<TKey, TValue>(MapField<TKey, TValue> map)
  {
    return JsonSerializer.Serialize(map, JsonSerializerOptions.Default);
  }

  private static MapField<TKey, TValue> DeserializeMapFromJson<TKey, TValue>(string json) where TKey : notnull
  {
    var dict = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json) ?? [];
    var map = new MapField<TKey, TValue>
    {
      dict
    };
    return map;
  }

  private static RepeatedField<T> DeserializeRepeatedFieldFromJson<T>(string json)
  {
    var arr = JsonSerializer.Deserialize<T[]>(json, JsonSerializerOptions.Default) ?? [];
    var rf = new RepeatedField<T>
    {
      arr
    };
    return rf;
  }
}
