using Ares.Datamodel;

namespace Ares.Core.DataManagement.DataMappers;

public static class AresValueFlattener
{
  public static IEnumerable<KeyValuePair<string, AresValue>> Flatten(string fieldName, AresValue value)
  {
    if(value.KindCase != AresValue.KindOneofCase.StructValue)
    {
      yield return KeyValuePair.Create(fieldName, value);
      yield break;
    }

    foreach(var childField in value.StructValue.Fields)
    {
      foreach(var flattenedChildField in Flatten($"{fieldName}.{childField.Key}", childField.Value))
      {
        yield return flattenedChildField;
      }
    }
  }
}
