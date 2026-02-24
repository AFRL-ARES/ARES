using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using AresScript.Symbols;

namespace AresScript;

public sealed record AresSystemValue
{
  public AresSystemValueKind Kind { get; }
  internal AresValue? RawValue { get; }
  public IDictionary<string, AresSystemValue>? StructFields { get; }
  internal IList<AresSystemValue>? ListValues { get; }
  public AresSystemStructKind StructKind { get; set; }
  internal AresSystemFunction? SystemFunction { get; set; }

  public string? Description { get; init; }

  private AresSystemValue(
    AresSystemValueKind kind,
    AresValue? rawValue,
    string? description,
    IDictionary<string, AresSystemValue>? structFields,
    IList<AresSystemValue>? listValues,
    AresSystemStructKind structKind)
  {
    Kind = kind;
    RawValue = rawValue;
    Description = description;
    StructFields = structFields;
    ListValues = listValues;
    StructKind = structKind;
  }

  public static AresSystemValue From(AresValue value, string? description = null)
    => new(AresSystemValueKind.Raw, value, description, null, null, AresSystemStructKind.Generic);

  public static AresSystemValue Null(string? description = null) => From(AresValueHelper.CreateNull(), description);
  public static AresSystemValue Unit(string? description = null) => From(AresValueHelper.CreateUnit(), description);
  public static AresSystemValue Bool(bool value, string? description = null) => From(AresValueHelper.CreateBool(value), description);
  public static AresSystemValue Number(int value, string? description = null) => From(AresValueHelper.CreateNumber(value), description);
  public static AresSystemValue Number(double value, string? description = null) => From(AresValueHelper.CreateNumber(value), description);
  public static AresSystemValue Number(float value, string? description = null) => From(AresValueHelper.CreateNumber(value), description);
  public static AresSystemValue String(string value, string? description = null) => From(AresValueHelper.CreateString(value), description);
  public static AresSystemValue Bytes(byte[] value, string? description = null) => From(AresValueHelper.CreateBytes(value), description);
  public static AresSystemValue StringArray(IEnumerable<string> values, string? description = null)
    => From(AresValueHelper.CreateStringArray(values), description);
  public static AresSystemValue NumberArray(IEnumerable<int> values, string? description = null)
    => From(AresValueHelper.CreateNumberArray(values), description);
  public static AresSystemValue NumberArray(IEnumerable<double> values, string? description = null)
    => From(AresValueHelper.CreateNumberArray(values), description);
  public static AresSystemValue NumberArray(IEnumerable<float> values, string? description = null)
    => From(AresValueHelper.CreateNumberArray(values), description);
  public static AresSystemValue List(IEnumerable<AresValue> values, string? description = null)
    => From(AresValueHelper.CreateList(values), description);
  public static AresSystemValue List(IEnumerable<AresSystemValue> values, string? description = null)
    => new(AresSystemValueKind.List, null, description, null, values.ToList(), AresSystemStructKind.Generic);

  public static AresSystemValue Struct(
    AresStruct value,
    string? description = null,
    AresSystemStructKind structKind = AresSystemStructKind.Generic)
    => From(AresValueHelper.CreateStruct(value), description) with { StructKind = structKind };

  public static AresSystemValue Struct(
    IDictionary<string, AresSystemValue> fields,
    string? description = null,
    AresSystemStructKind structKind = AresSystemStructKind.Generic)
    => new(AresSystemValueKind.Struct, null, description, fields, null, structKind);

  public static AresSystemValue Struct(
    string? description = null,
    AresSystemStructKind structKind = AresSystemStructKind.Generic)
    => new(AresSystemValueKind.Struct, null, description, new Dictionary<string, AresSystemValue>(), null, structKind);

  public static AresSystemValue Function(AresSystemFunction function)
    => From(AresValueHelper.CreateFunction(function.Id), function.Description).WithFunction(function);

  public AresSystemValue WithDescription(string? description) => this with { Description = description };
  public AresSystemValue WithStructKind(AresSystemStructKind structKind)
    => Kind == AresSystemValueKind.Struct ? this with { StructKind = structKind } : this;

  public AresSystemValue WithFunction(AresSystemFunction function) => this with { SystemFunction = function };

  public enum AresSystemValueKind
  {
    Raw,
    Struct,
    List,
    Function
  }

  public enum AresSystemStructKind
  {
    Generic,
    Device,
    Planner,
    Analyzer
  }
}
