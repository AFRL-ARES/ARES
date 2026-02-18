using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace AresScript;

public static class AresSystemValueExtensions
{
  public static AresValue ToAresValue(this AresSystemValue value)
  {
    if(value is null)
    {
      return AresValueHelper.CreateNull();
    }

    return value.Kind switch
    {
      AresSystemValue.AresSystemValueKind.Raw => value.RawValue ?? AresValueHelper.CreateNull(),
      AresSystemValue.AresSystemValueKind.Struct => BuildStructValue(value),
      AresSystemValue.AresSystemValueKind.List => BuildListValue(value),
      _ => AresValueHelper.CreateNull()
    };
  }

  private static AresValue BuildStructValue(AresSystemValue value)
  {
    var structValue = new AresStruct();
    if(value.StructFields is not null)
    {
      foreach(var (key, fieldValue) in value.StructFields)
      {
        structValue.Fields[key] = fieldValue.ToAresValue();
      }
    }

    return AresValueHelper.CreateStruct(structValue);
  }

  private static AresValue BuildListValue(AresSystemValue value)
  {
    if(value.ListValues is null)
    {
      return AresValueHelper.CreateList();
    }

    var list = value.ListValues.Select(item => item.ToAresValue());
    return AresValueHelper.CreateList(list);
  }
}
