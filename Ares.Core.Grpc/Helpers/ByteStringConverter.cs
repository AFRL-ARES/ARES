using Google.Protobuf;
using Newtonsoft.Json;
using System;

namespace Ares.Core.Grpc.Helpers;

public class ByteStringConverter : JsonConverter<ByteString>
{
  public override void WriteJson(JsonWriter writer, ByteString? value, JsonSerializer serializer)
  {
    if(value is null)
    {
      writer.WriteValue(value);
      return;
    }

    var byteArray = value.ToByteArray();
    var jsonString = Convert.ToBase64String(byteArray);

    writer.WriteValue(jsonString);
  }

  public override ByteString? ReadJson(JsonReader reader, Type objectType, ByteString? existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    if(reader.TokenType is JsonToken.Null || reader.Value is null)
      return null;

    if(reader.Value is string stringValue)
    {
      var byteArray = Convert.FromBase64String(stringValue);
      return ByteString.CopyFrom(byteArray);
    }

    return existingValue ?? ByteString.Empty;
  }
}
