using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public sealed record AresSystemValue : IValueSymbol
{
  public string Name { get; init; } = string.Empty;
  public bool IsReadOnly { get; init; } = false;
  public bool IsUserDefined { get; init; } = false;
  public string? ParentName { get; init; }
  public SchemaEntry? DeclaredSchema { get; init; }
  public AresSystemValueKind ValueKind { get; }
  internal AresValue? RawValue { get; }
  public IDictionary<string, AresSystemValue>? StructFields { get; }
  internal IList<AresSystemValue>? ListValues { get; }
  public SymbolKind SymbolKind { get; init; }
  internal AresSystemFunctionSymbol? SystemFunction { get; set; }
  public string? Detail { get; set; }
  public string? Documentation { get; set; }
  public AresValue Value => this.ToAresValue();

  private AresSystemValue(
    AresSystemValueKind valueKind,
    AresValue? rawValue,
    IDictionary<string, AresSystemValue>? structFields,
    IList<AresSystemValue>? listValues,
    SymbolKind symbolKind)
  {
    ValueKind = valueKind;
    RawValue = rawValue;
    StructFields = structFields;
    ListValues = listValues;
    SymbolKind = symbolKind;
  }

  public static AresSystemValue From(AresValue value)
    => new(AresSystemValueKind.Raw, value, null, null, SymbolKind.Variable);

  public static AresSystemValue Null() => From(AresValueHelper.CreateNull());
  public static AresSystemValue Unit() => From(AresValueHelper.CreateUnit());
  public static AresSystemValue Bool(bool value) => From(AresValueHelper.CreateBool(value));
  public static AresSystemValue Number(int value) => From(AresValueHelper.CreateNumber(value));
  public static AresSystemValue Number(double value) => From(AresValueHelper.CreateNumber(value));
  public static AresSystemValue Number(float value) => From(AresValueHelper.CreateNumber(value));
  public static AresSystemValue String(string value) => From(AresValueHelper.CreateString(value));
  public static AresSystemValue Bytes(byte[] value) => From(AresValueHelper.CreateBytes(value));
  public static AresSystemValue StringArray(IEnumerable<string> values)
    => From(AresValueHelper.CreateStringArray(values));
  public static AresSystemValue NumberArray(IEnumerable<int> values)
    => From(AresValueHelper.CreateNumberArray(values));
  public static AresSystemValue NumberArray(IEnumerable<double> values)
    => From(AresValueHelper.CreateNumberArray(values));
  public static AresSystemValue NumberArray(IEnumerable<float> values)
    => From(AresValueHelper.CreateNumberArray(values));
  public static AresSystemValue List(IEnumerable<AresValue> values)
    => From(AresValueHelper.CreateList(values));
  public static AresSystemValue List(IEnumerable<AresSystemValue> values)
    => new(AresSystemValueKind.List, null, null, values.ToList(), SymbolKind.Variable);

  public static AresSystemValue Struct(
    AresStruct value,
    string? description = null,
    SymbolKind structKind = SymbolKind.Struct)
    => From(AresValueHelper.CreateStruct(value)) with { SymbolKind = structKind };

  public static AresSystemValue Struct(
    IDictionary<string, AresSystemValue> fields,
    SymbolKind structKind = SymbolKind.Struct)
    => new(AresSystemValueKind.Struct, null, fields, null, structKind);

  public static AresSystemValue Struct(
    SymbolKind structKind = SymbolKind.Struct)
    => new(AresSystemValueKind.Struct, null, new Dictionary<string, AresSystemValue>(), null, structKind);

  public static AresSystemValue Function(AresSystemFunctionSymbol function)
    => From(AresValueHelper.CreateFunction(function.Id)).WithFunction(function);

  public AresSystemValue WithStructKind(SymbolKind structKind)
    => ValueKind == AresSystemValueKind.Struct ? this with { SymbolKind = structKind } : this;

  public AresSystemValue WithFunction(AresSystemFunctionSymbol function) => this with { SystemFunction = function };

  public enum AresSystemValueKind
  {
    Raw,
    Struct,
    List,
    Function
  }
}
