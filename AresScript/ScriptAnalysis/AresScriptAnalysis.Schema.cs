using Ares.Datamodel;
using Ares.Datamodel.Factories;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  private static SchemaEntry ValueToSchemaEntry(AresValue value)
  {
    return value.KindCase switch
    {
      AresValue.KindOneofCase.NullValue => AresSchemaBuilder.Entry(AresDataType.Null).Build(),
      AresValue.KindOneofCase.BoolValue => AresSchemaBuilder.Entry(AresDataType.Boolean).Build(),
      AresValue.KindOneofCase.StringValue => AresSchemaBuilder.Entry(AresDataType.String).Build(),
      AresValue.KindOneofCase.NumberValue => AresSchemaBuilder.Entry(AresDataType.Number).Build(),
      AresValue.KindOneofCase.StringArrayValue => AresSchemaBuilder.Entry(AresDataType.StringArray).Build(),
      AresValue.KindOneofCase.NumberArrayValue => AresSchemaBuilder.Entry(AresDataType.NumberArray).Build(),
      AresValue.KindOneofCase.BytesValue => AresSchemaBuilder.Entry(AresDataType.ByteArray).Build(),
      AresValue.KindOneofCase.UnitValue => AresSchemaBuilder.Entry(AresDataType.Unit).Build(),
      AresValue.KindOneofCase.ListValue => CreateListEntry(value.ListValue.Values),
      AresValue.KindOneofCase.StructValue => CreateStructEntry(value.StructValue),
      _ => AresSchemaBuilder.Entry(AresDataType.Any).Build()
    };
  }

  private static SchemaEntry CreateStructEntry(AresStruct structValue)
  {
    var schema = new AresDataSchema();
    foreach(var field in structValue.Fields)
    {
      schema.Fields[field.Key] = ValueToSchemaEntry(field.Value);
    }

    var entry = AresSchemaBuilder.Entry(AresDataType.Struct).Build();
    entry.StructSchema = schema;
    return entry;
  }

  private static SchemaEntry CreateListEntry(IEnumerable<AresValue> values)
  {
    var list = values.ToArray();
    if(list.Length == 0)
    {
      return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
    }

    var first = ValueToSchemaEntry(list[0]);
    var allSameType = list.All(val => ValueToSchemaEntry(val).Type == first.Type);
    if(allSameType)
    {
      return CreateListEntry(first);
    }

    return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
  }

  private static SchemaEntry CreateListEntry(SchemaEntry elementSchema)
  {
    var entry = AresSchemaBuilder.Entry(AresDataType.List).Build();
    entry.ListElementSchema = elementSchema;
    return entry;
  }
}
