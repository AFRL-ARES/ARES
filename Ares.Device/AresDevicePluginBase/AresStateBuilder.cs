using Ares.Datamodel;
using System;
using System.Collections.Generic;

namespace Ares.Device;

public class AresStateBuilder
{
  private readonly AresStruct _struct = new AresStruct();

  public static AresStateBuilder Create() => new AresStateBuilder();

  public AresStruct Build() => _struct;

  // --- Primitives ---
  public AresStateBuilder Add(string key, string? value)
  {
    _struct.Fields[key] = new AresValue { StringValue = value ?? "" };
    return this;
  }

  public AresStateBuilder Add(string key, bool value)
  {
    _struct.Fields[key] = new AresValue { BoolValue = value };
    return this;
  }

  public AresStateBuilder Add(string key, double value)
  {
    _struct.Fields[key] = new AresValue { NumberValue = value };
    return this;
  }

  public AresStateBuilder Add(string key, int value) => Add(key, (double)value);

  // --- Nested Objects (Recursion) ---
  public AresStateBuilder AddStruct(string key, Action<AresStateBuilder> builderAction)
  {
    var childBuilder = new AresStateBuilder();
    builderAction(childBuilder);
    _struct.Fields[key] = new AresValue { StructValue = childBuilder.Build() };
    return this;
  }

  // --- Lists ---
  public AresStateBuilder AddList<T>(string key, IEnumerable<T> items, Func<T, AresValue> mapper)
  {
    var list = new AresValueList();
    if (items != null)
    {
      foreach (var item in items)
      {
        list.Values.Add(mapper(item));
      }
    }
    _struct.Fields[key] = new AresValue { ListValue = list };
    return this;
  }
}
