using Ares.Datamodel;
using System;
using System.Collections.Generic;

namespace Ares.Device;

/// <summary>
/// Fluent builder for constructing <see cref="AresStruct"/> instances.
/// 
/// This builder is designed to support immutable-by-convention state updates:
/// callers create new state objects rather than mutating existing ones.
/// </summary>
public class AresStateBuilder
{
  private readonly AresStruct _struct = new AresStruct();

  /// <summary>
  /// Creates a new, empty <see cref="AresStateBuilder"/>.
  /// </summary>
  /// <returns>A new builder instance.</returns>
  public static AresStateBuilder Create() => new AresStateBuilder();

  /// <summary>
  /// Builds and returns the underlying <see cref="AresStruct"/>.
  /// 
  /// After calling this method, the returned struct should be treated as immutable
  /// by convention and not modified directly.
  /// </summary>
  /// <returns>The constructed <see cref="AresStruct"/>.</returns>
  public AresStruct Build() => _struct;

  /// <summary>
  /// Adds or replaces a string field in the state.
  /// 
  /// If <paramref name="value"/> is <c>null</c>, an empty string is stored.
  /// </summary>
  /// <param name="key">The field name.</param>
  /// <param name="value">The string value.</param>
  /// <returns>The current builder instance.</returns>
  public AresStateBuilder Add(string key, string? value)
  {
    _struct.Fields[key] = new AresValue { StringValue = value ?? "" };
    return this;
  }

  /// <summary>
  /// Adds or replaces a boolean field in the state.
  /// </summary>
  /// <param name="key">The field name.</param>
  /// <param name="value">The boolean value.</param>
  /// <returns>The current builder instance.</returns>
  public AresStateBuilder Add(string key, bool value)
  {
    _struct.Fields[key] = new AresValue { BoolValue = value };
    return this;
  }

  /// <summary>
  /// Adds or replaces a numeric (double) field in the state.
  /// </summary>
  /// <param name="key">The field name.</param>
  /// <param name="value">The numeric value.</param>
  /// <returns>The current builder instance.</returns>
  public AresStateBuilder Add(string key, double value)
  {
    _struct.Fields[key] = new AresValue { NumberValue = value };
    return this;
  }

  /// <summary>
  /// Adds or replaces a numeric field using an integer value.
  /// 
  /// The value is stored as a double in the underlying state representation.
  /// </summary>
  /// <param name="key">The field name.</param>
  /// <param name="value">The integer value.</param>
  /// <returns>The current builder instance.</returns>
  public AresStateBuilder Add(string key, int value) => Add(key, (double)value);

  /// <summary>
  /// Adds or replaces a nested struct field.
  /// 
  /// The provided <paramref name="builderAction"/> is used to construct the
  /// nested <see cref="AresStruct"/> via a child builder.
  /// </summary>
  /// <param name="key">The field name.</param>
  /// <param name="builderAction">Action used to populate the nested struct.</param>
  /// <returns>The current builder instance.</returns>
  public AresStateBuilder AddStruct(string key, Action<AresStateBuilder> builderAction)
  {
    var childBuilder = new AresStateBuilder();
    builderAction(childBuilder);
    _struct.Fields[key] = new AresValue { StructValue = childBuilder.Build() };
    return this;
  }

  /// <summary>
  /// Adds or replaces a list field.
  /// 
  /// Each item in <paramref name="items"/> is converted to an <see cref="AresValue"/>
  /// using the provided <paramref name="mapper"/>.
  /// </summary>
  /// <typeparam name="T">The type of items in the list.</typeparam>
  /// <param name="key">The field name.</param>
  /// <param name="items">The items to include in the list.</param>
  /// <param name="mapper">Function mapping items to <see cref="AresValue"/>.</param>
  /// <returns>The current builder instance.</returns>
  public AresStateBuilder AddList<T>(string key, IEnumerable<T> items, Func<T, AresValue> mapper)
  {
    var list = new AresValueList();
    if(items != null)
    {
      foreach(var item in items)
      {
        list.Values.Add(mapper(item));
      }
    }
    _struct.Fields[key] = new AresValue { ListValue = list };
    return this;
  }

  /// <summary>
  /// Creates a new <see cref="AresStateBuilder"/> initialized with a deep copy
  /// of an existing <see cref="AresStruct"/>.
  /// 
  /// This method ensures that no mutable protobuf objects are shared between
  /// the source and the newly built state.
  /// </summary>
  /// <param name="source">The source state to copy from.</param>
  /// <returns>A new builder initialized with the copied state.</returns>
  /// <exception cref="ArgumentNullException">
  /// Thrown if <paramref name="source"/> is <c>null</c>.
  /// </exception>
  public static AresStateBuilder From(AresStruct source)
  {
    if(source is null)
      throw new ArgumentNullException(nameof(source));

    var builder = new AresStateBuilder();

    foreach(var kvp in source.Fields)
    {
      builder._struct.Fields[kvp.Key] = CloneValue(kvp.Value);
    }

    return builder;
  }

  /// <summary>
  /// Performs a deep clone of an <see cref="AresValue"/>, including any nested
  /// structs or lists.
  /// 
  /// This is required to prevent shared mutable state when working with
  /// protobuf-generated types.
  /// </summary>
  /// <param name="value">The value to clone.</param>
  /// <returns>A deep copy of the provided value.</returns>
  private static AresValue CloneValue(AresValue value)
  {
    if(value == null)
      return new AresValue();

    if(value.StructValue != null)
    {
      var structClone = new AresStruct();
      foreach(var kvp in value.StructValue.Fields)
      {
        structClone.Fields[kvp.Key] = CloneValue(kvp.Value);
      }

      return new AresValue { StructValue = structClone };
    }

    if(value.ListValue != null)
    {
      var listClone = new AresValueList();
      foreach(var item in value.ListValue.Values)
      {
        listClone.Values.Add(CloneValue(item));
      }

      return new AresValue { ListValue = listClone };
    }

    return new AresValue
    {
      BoolValue = value.BoolValue,
      StringValue = value.StringValue,
      NumberValue = value.NumberValue,
      BytesValue = value.BytesValue,
      StringArrayValue = value.StringArrayValue,
      NumberArrayValue = value.NumberArrayValue,
      UnitValue = value.UnitValue,
      FunctionValue = value.FunctionValue
    };
  }
}
