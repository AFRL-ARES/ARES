using Ares.Datamodel;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;
using AresScript.Generated;

namespace AresScript.Symbols;

public record AresScriptFunction(
  string Name,
  IReadOnlyList<AresScriptParameter> Parameters,
  AresLangParser.FuncBlockContext Body,
  AresValueSchema ReturnSchema,
  string? Description = null,
  string? Documentation = null,
  string? ParentName = null) : IFunctionSymbol
{
  public AresScriptFunction(
    string name,
    IReadOnlyList<AresScriptParameter> parameters,
    AresLangParser.FuncBlockContext body,
    AresDataType returnType = AresDataType.Any,
    string? description = null,
    string? documentation = null,
    string? parentName = null)
    : this(name, parameters, body, AresSchemaBuilder.Entry(returnType).Build(), description, documentation, parentName)
  {
  }

  public SymbolKind SymbolKind => SymbolKind.Function;
  public string? Detail => Description;
  public bool IsUserDefined => true;
  public bool IsExtension => false;
  public bool IsLambda => false;
  public AresDataType ReturnType => ReturnSchema.Type;
  public IReadOnlyList<string> ParameterNames { get; } = Parameters.Select(parameter => parameter.Name).ToArray();
}
