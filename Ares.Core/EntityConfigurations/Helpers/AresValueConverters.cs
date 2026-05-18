using Ares.Datamodel;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Ares.Core.EntityConfigurations.Helpers;

public class AresValueConverters : ValueConverter<AresStruct, string>
{
  public AresValueConverters() : base(
      v => JsonSerializer.Serialize(v, SerializerSettingsHelper.CreateCustomSerializationSettings()),
      v => JsonSerializer.Deserialize<AresStruct>(v, SerializerSettingsHelper.CreateCustomSerializationSettings()) ?? new AresStruct())
  { }
}

public class AresValueConverter : ValueConverter<AresValue, string>
{
  public AresValueConverter() : base(
      v => JsonSerializer.Serialize(v, SerializerSettingsHelper.CreateCustomSerializationSettings()),
      v => JsonSerializer.Deserialize<AresValue>(v, SerializerSettingsHelper.CreateCustomSerializationSettings()) ?? new AresValue())
  { }
}

public class AresTimestampConverter : ValueConverter<Timestamp, DateTime>
{
  public AresTimestampConverter() : base(
      protobufTimestamp => protobufTimestamp.ToDateTime(),
      dateTime => Timestamp.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)))
  { }
}

public class AresValueSchemaConverter : ValueConverter<AresValueSchema, string>
{
  public AresValueSchemaConverter() : base(
      v => JsonSerializer.Serialize(v, SerializerSettingsHelper.CreateCustomSerializationSettings()),
      v => JsonSerializer.Deserialize<AresValueSchema>(v, SerializerSettingsHelper.CreateCustomSerializationSettings()) ?? new AresValueSchema())
  { }
}

public class AresStructSchemaConverter : ValueConverter<AresStructSchema, string>
{
  public AresStructSchemaConverter() : base(
      v => JsonSerializer.Serialize(v, SerializerSettingsHelper.CreateCustomSerializationSettings()),
      v => JsonSerializer.Deserialize<AresStructSchema>(v, SerializerSettingsHelper.CreateCustomSerializationSettings()) ?? new AresStructSchema())
  { }
}
