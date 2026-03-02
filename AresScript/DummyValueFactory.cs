using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace AresScript;

public static class DummyValueFactory
{
  public static AresValue CreateDummyValue(SchemaEntry? schema)
  {
    if(schema is null)
    {
      return AresValueHelper.CreateNull();
    }

    switch(schema.Type)
    {
      case AresDataType.Struct:
        {
          var structValue = AresValueHelper.CreateStruct();
          if(schema.StructSchema?.Fields is not null)
          {
            foreach(var field in schema.StructSchema.Fields)
            {
              structValue.StructValue.Fields[field.Key] = CreateDummyValue(field.Value);
            }
          }

          return structValue;
        }
      case AresDataType.List:
        {
          if(schema.ListElementSchema is not null)
          {
            return AresValueHelper.CreateList([CreateDummyValue(schema.ListElementSchema)]);
          }

          return AresValueHelper.CreateList();
        }
      case AresDataType.String:
        if(schema.StringChoices is not null && schema.StringChoices.Strings.Count > 0)
        {
          return AresValueHelper.CreateString(schema.StringChoices.Strings[0]);
        }
        return AresValueHelper.CreateString(string.Empty);
      case AresDataType.Number:
        if(schema.NumberChoices is not null && schema.NumberChoices.Numbers.Count > 0)
        {
          return AresValueHelper.CreateNumber(schema.NumberChoices.Numbers[0]);
        }
        return AresValueHelper.CreateNumber(0);
      case AresDataType.Boolean:
        return AresValueHelper.CreateBool(false);
      case AresDataType.StringArray:
        return AresValueHelper.CreateStringArray([]);
      case AresDataType.NumberArray:
        return AresValueHelper.CreateNumberArray(Array.Empty<double>());
      case AresDataType.ByteArray:
        return AresValueHelper.CreateBytes([]);
      case AresDataType.Unit:
        return AresValueHelper.CreateUnit();
      case AresDataType.Function:
        return AresValueHelper.CreateFunction(string.Empty);
      case AresDataType.UnspecifiedType:
      case AresDataType.Any:
        return new AresValue();
      case AresDataType.Null:
      default:
        return AresValueHelper.CreateNull();
    }
  }

  public static AresValue CreateDummyValue(AresDataType dataType)
  {
    switch(dataType)
    {
      case AresDataType.Struct:
        {
          var structValue = AresValueHelper.CreateStruct();
          return structValue;
        }
      case AresDataType.List:
        return AresValueHelper.CreateList();
      case AresDataType.String:
        return AresValueHelper.CreateString("");
      case AresDataType.Number:
        return AresValueHelper.CreateNumber(0);
      case AresDataType.Boolean:
        return AresValueHelper.CreateBool(false);
      case AresDataType.StringArray:
        return AresValueHelper.CreateStringArray([]);
      case AresDataType.NumberArray:
        return AresValueHelper.CreateNumberArray(Array.Empty<double>());
      case AresDataType.ByteArray:
        return AresValueHelper.CreateBytes([]);
      case AresDataType.Unit:
        return AresValueHelper.CreateUnit();
      case AresDataType.Function:
        return AresValueHelper.CreateFunction(string.Empty);
      case AresDataType.Any:
      case AresDataType.UnspecifiedType:
        return new AresValue();
      case AresDataType.Null:
      default:
        return AresValueHelper.CreateNull();
    }
  }
}
