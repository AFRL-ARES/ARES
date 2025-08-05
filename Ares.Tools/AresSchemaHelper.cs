using Ares.Messaging;

namespace Ares.Tools;
public static class AresSchemaHelper
{
  public static AresDataSchema AddEntry(this AresDataSchema schema, string name, AresDataType type, bool optional)
  {
    var entry = new SchemaEntry() { Type = type, Optional = optional };
    schema.Fields[name] = entry;

    return schema;
  }

  public static AresDataSchema AddEntry(this AresDataSchema schema, string name, AresDataType type, bool optional, IEnumerable<string> stringOptions)
  {
    var entry = new SchemaEntry() { Type = type, Optional = optional };
    if(type != AresDataType.String && type != AresDataType.StringArray)
    {
      throw new InvalidOperationException($"Cannot provide string options to a datatype that is {type}");
    }
    entry.StringChoices = new StringArray();
    entry.StringChoices.Strings.AddRange(stringOptions);
    schema.Fields[name] = entry;

    return schema;
  }

  public static AresDataSchema AddEntry(this AresDataSchema schema, string name, AresDataType type, bool optional, IEnumerable<double> numOptions)
  {
    var entry = new SchemaEntry() { Type = type, Optional = optional };
    if(type != AresDataType.Number && type != AresDataType.NumberArray)
    {
      throw new InvalidOperationException($"Cannot provide number options to a datatype that is {type}");
    }
    entry.NumberChoices = new NumberArray();
    entry.NumberChoices.Numbers.AddRange(numOptions);
    schema.Fields[name] = entry;

    return schema;
  }

  public static AresDataSchema AddEntry(this AresDataSchema schema, string name, AresDataType type, bool optional, IEnumerable<int> numOptions)
  {
    schema.AddEntry(name, type, optional, numOptions.Select(n => (double)n));

    return schema;
  }

  public static AresDataSchema AddEntry(this AresDataSchema schema, string name, AresDataType type, bool optional, IEnumerable<float> numOptions)
  {
    schema.AddEntry(name, type, optional, numOptions.Select(n => (double)n));

    return schema;
  }

  public static AresDataSchemaSimplified CreateSchema(string name, AresDataType type)
  {
    var schema = new AresDataSchemaSimplified();
    schema.AddEntry(name, type);
    return schema;
  }

  public static AresDataSchemaSimplified AddEntry(this AresDataSchemaSimplified schema, string name, AresDataType type)
  {
    schema.Fields[name] = type;

    return schema;
  }

  public static SchemaEntry CreateSchemaEntry(AresDataType dataType, bool optional)
  {
    return new SchemaEntry() { Type = dataType, Optional = optional };
  }

  public static SchemaEntry CreateSchemaEntry(AresDataType dataType, bool optional, IEnumerable<string> stringChoices)
  {
    if(dataType != AresDataType.String && dataType != AresDataType.StringArray)
    {
      throw new InvalidOperationException($"String choices were provided, but the data type was of type {dataType}");
    }

    var entry = new SchemaEntry
    {
      Type = dataType,
      Optional = optional,
      StringChoices = new StringArray()
    };
    entry.StringChoices.Strings.AddRange(stringChoices);

    return entry;
  }

  public static SchemaEntry CreateSchemaEntry(AresDataType dataType, bool optional, IEnumerable<double> numberChoices)
  {
    if(dataType != AresDataType.Number && dataType != AresDataType.NumberArray)
    {
      throw new InvalidOperationException($"Number choices were provided, but the data type was of type {dataType}");
    }

    var entry = new SchemaEntry
    {
      Type = dataType,
      Optional = optional,
      NumberChoices = new NumberArray()
    };
    entry.NumberChoices.Numbers.AddRange(numberChoices);

    return entry;
  }

  public static SchemaEntry CreateSchemaEntry(AresDataType dataType, bool optional, IEnumerable<int> numberChoices)
  {
    var doubles = numberChoices.Select(num => (double)num);

    return CreateSchemaEntry(dataType, optional, doubles);
  }

  public static SchemaEntry CreateSchemaEntry(AresDataType dataType, bool optional, IEnumerable<float> numberChoices)
  {
    var doubles = numberChoices.Select(num => (double)num);

    return CreateSchemaEntry(dataType, optional, doubles);
  }
}
