using Ares.Messaging;
using Google.Protobuf;

namespace Ares.Tools;
public static class AresValueHelper
{
  public static AresValue CreateNumber(int value)
  {
    return new AresValue { NumberValue = value, };
  }

  public static AresValue CreateNumber(double value)
  {
    return new AresValue { NumberValue = value, };
  }

  public static AresValue CreateNumber(float value)
  {
    return new AresValue { NumberValue = value, };
  }

  public static AresValue CreateString(string value)
  {
    return new AresValue { StringValue = value, };
  }

  public static AresValue CreateNumberArray(IEnumerable<int> values)
  {
    var val = new AresValue { NumberArrayValue = new NumberArray() };
    val.NumberArrayValue.Numbers.AddRange(values.Select(num => (double)num));

    return val;
  }

  public static AresValue CreateNumberArray(IEnumerable<double> values)
  {
    var val = new AresValue { NumberArrayValue = new NumberArray() };
    val.NumberArrayValue.Numbers.AddRange(values.Select(num => num));

    return val;
  }

  public static AresValue CreateNumberArray(IEnumerable<float> values)
  {
    var val = new AresValue { NumberArrayValue = new NumberArray() };
    val.NumberArrayValue.Numbers.AddRange(values.Select(num => (double)num));

    return val;
  }

  public static AresValue CreateStringArray(IEnumerable<string> values)
  {
    var val = new AresValue { StringArrayValue = new StringArray() };
    val.StringArrayValue.Strings.AddRange(values);

    return val;
  }

  public static AresValue CreateNull()
  {
    return new AresValue { NullValue = NullValue.NullValue };
  }

  public static AresValue CreateBool(bool value)
  {
    return new AresValue { BoolValue = value };
  }

  public static AresValue CreateBytes(byte[] bytes)
  {
    return new AresValue { BytesValue = ByteString.CopyFrom(bytes) };
  }

  public static AresValue CreateBoolArray(IEnumerable<bool> values)
  {
    var val = new AresValue { BoolArrayValue = new BoolArray() };
    val.BoolArrayValue.Bools.AddRange(values);

    return val;
  }

  public static AresValue CreateDefault(AresDataType dataType)
  {
    return dataType switch
    {
      AresDataType.UnspecifiedType => CreateNull(),
      AresDataType.Null => CreateNull(),
      AresDataType.Boolean => CreateBool(false),
      AresDataType.String => CreateString(string.Empty),
      AresDataType.Number => CreateNumber(0),
      AresDataType.StringArray => CreateStringArray(Array.Empty<string>()),
      AresDataType.NumberArray => CreateNumberArray(Array.Empty<double>()),
      AresDataType.ByteArray => CreateBytes(Array.Empty<byte>()),
      AresDataType.BoolArray => CreateBoolArray(Array.Empty<bool>()),
      _ => CreateNull()
    };
  }

  public static AresValue CreateDefault(AresDataType dataType, IEnumerable<string>? choices = null)
  {
    return dataType switch
    {
      AresDataType.String => CreateString(choices?.FirstOrDefault() ?? string.Empty),
      _ => CreateDefault(dataType)
    };
  }

  public static AresValue CreateDefault(AresDataType dataType, IEnumerable<double>? choices = null)
  {
    return dataType switch
    {
      AresDataType.Number => CreateNumber(choices?.FirstOrDefault() ?? 0),
      _ => CreateDefault(dataType)
    };
  }
}
