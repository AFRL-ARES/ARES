using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace AresScript;

public sealed class AresSystemValueBuilder
{
  private readonly AresSystemValue.AresSystemValueKind _kind;
  private readonly AresValue? _rawValue;
  private string? _description;
  private Dictionary<string, AresSystemValueBuilder>? _structFields;
  private List<AresSystemValueBuilder>? _listValues;
  private AresSystemValue.AresSystemStructKind _structKind;
  private AresSystemFunction? _systemFunction;

  private AresSystemValueBuilder(AresSystemValue.AresSystemValueKind kind, AresValue? rawValue, string? description)
  {
    _kind = kind;
    _rawValue = rawValue;
    _description = description;
    _structKind = AresSystemValue.AresSystemStructKind.Generic;
  }

  public static AresSystemValueBuilder From(AresValue value, string? description = null)
    => new(AresSystemValue.AresSystemValueKind.Raw, value, description);

  public static AresSystemValueBuilder From(AresSystemValue value)
  {
    if(value is null)
    {
      return Null();
    }

    return value.Kind switch
    {
      AresSystemValue.AresSystemValueKind.Raw => From(value.RawValue ?? AresValueHelper.CreateNull(), value.Description),
      AresSystemValue.AresSystemValueKind.Struct => FromStruct(value),
      AresSystemValue.AresSystemValueKind.List => FromList(value),
      AresSystemValue.AresSystemValueKind.Function => FromFunction(value),
      _ => Null(value.Description)
    };
  }

  public static AresSystemValueBuilder Null(string? description = null) => From(AresValueHelper.CreateNull(), description);
  public static AresSystemValueBuilder Unit(string? description = null) => From(AresValueHelper.CreateUnit(), description);
  public static AresSystemValueBuilder Bool(bool value, string? description = null) => From(AresValueHelper.CreateBool(value), description);
  public static AresSystemValueBuilder Number(int value, string? description = null) => From(AresValueHelper.CreateNumber(value), description);
  public static AresSystemValueBuilder Number(double value, string? description = null) => From(AresValueHelper.CreateNumber(value), description);
  public static AresSystemValueBuilder Number(float value, string? description = null) => From(AresValueHelper.CreateNumber(value), description);
  public static AresSystemValueBuilder String(string value, string? description = null) => From(AresValueHelper.CreateString(value), description);
  public static AresSystemValueBuilder Bytes(byte[] value, string? description = null) => From(AresValueHelper.CreateBytes(value), description);
  public static AresSystemValueBuilder StringArray(IEnumerable<string> values, string? description = null)
    => From(AresValueHelper.CreateStringArray(values), description);
  public static AresSystemValueBuilder NumberArray(IEnumerable<int> values, string? description = null)
    => From(AresValueHelper.CreateNumberArray(values), description);
  public static AresSystemValueBuilder NumberArray(IEnumerable<double> values, string? description = null)
    => From(AresValueHelper.CreateNumberArray(values), description);
  public static AresSystemValueBuilder NumberArray(IEnumerable<float> values, string? description = null)
    => From(AresValueHelper.CreateNumberArray(values), description);
  public static AresSystemValueBuilder Function(string functionId, AresSystemFunction function, string? description = null)
    => From(AresValueHelper.CreateFunction(functionId), description).WithFunction(function);

  public static AresSystemValueBuilder Struct(string? description = null)
    => new(AresSystemValue.AresSystemValueKind.Struct, null, description);

  public static AresSystemValueBuilder List(string? description = null)
    => new(AresSystemValue.AresSystemValueKind.List, null, description);

  public AresSystemValueBuilder WithDescription(string? description)
  {
    _description = description;
    return this;
  }

  public AresSystemValueBuilder Field(string name, AresSystemValueBuilder builder)
  {
    EnsureKind(AresSystemValue.AresSystemValueKind.Struct, "field");
    _structFields ??= new Dictionary<string, AresSystemValueBuilder>(StringComparer.Ordinal);
    _structFields[name] = builder;
    return this;
  }

  public AresSystemValueBuilder WithStructKind(AresSystemValue.AresSystemStructKind structKind)
  {
    EnsureKind(AresSystemValue.AresSystemValueKind.Struct, "struct kind");
    _structKind = structKind;
    return this;
  }

  public AresSystemValueBuilder WithFunction(AresSystemFunction function)
  {
    EnsureKind(AresSystemValue.AresSystemValueKind.Function, "function");
    _systemFunction = function;
    return this;
  }

  public AresSystemValueBuilder Field(string name, AresSystemValue value)
  {
    return Field(name, From(value));
  }

  public AresSystemValueBuilder AddItem(AresSystemValueBuilder builder)
  {
    EnsureKind(AresSystemValue.AresSystemValueKind.List, "list item");
    _listValues ??= new List<AresSystemValueBuilder>();
    _listValues.Add(builder);
    return this;
  }

  public AresSystemValueBuilder AddItem(AresSystemValue value)
  {
    return AddItem(From(value));
  }

  public AresSystemValue Build()
  {
    return _kind switch
    {
      AresSystemValue.AresSystemValueKind.Raw => AresSystemValue.From(_rawValue ?? AresValueHelper.CreateNull(), _description),
      AresSystemValue.AresSystemValueKind.Struct => BuildStruct(),
      AresSystemValue.AresSystemValueKind.List => BuildList(),
      _ => AresSystemValue.Null(_description)
    };
  }

  private AresSystemValue BuildStruct()
  {
    var fields = new Dictionary<string, AresSystemValue>(StringComparer.Ordinal);
    if(_structFields is not null)
    {
      foreach(var (key, value) in _structFields)
      {
        fields[key] = value.Build();
      }
    }

    return AresSystemValue.Struct(fields, _description, _structKind);
  }

  private AresSystemValue BuildList()
  {
    var values = _listValues?.Select(item => item.Build()).ToList() ?? [];
    return AresSystemValue.List(values, _description);
  }

  private void EnsureKind(AresSystemValue.AresSystemValueKind expected, string action)
  {
    if(_kind != expected)
    {
      throw new InvalidOperationException($"Cannot add {action} to a {_kind} AresSystemValueBuilder.");
    }
  }

  private static AresSystemValueBuilder FromStruct(AresSystemValue value)
  {
    var builder = Struct(value.Description);
    builder._structKind = value.StructKind;
    if(value.StructFields is not null)
    {
      foreach(var (key, fieldValue) in value.StructFields)
      {
        builder.Field(key, From(fieldValue));
      }
    }
    return builder;
  }

  private static AresSystemValueBuilder FromList(AresSystemValue value)
  {
    var builder = List(value.Description);
    if(value.ListValues is not null)
    {
      foreach(var item in value.ListValues)
      {
        builder.AddItem(From(item));
      }
    }
    return builder;
  }

  private static AresSystemValueBuilder FromFunction(AresSystemValue value)
  {
    if(value.SystemFunction is null)
    {
      throw new NullReferenceException($"Tried to create a builder with value that is not a function");
    }

    var builder = Function(value.SystemFunction.Id, value.SystemFunction, value.Description);
    return builder;
  }
}
