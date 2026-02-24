using Ares.Datamodel;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public interface IScriptSymbol
{
  string Name { get; }
  string? ParentName { get; }
  SymbolKind Kind { get; }
  string? Detail { get; }
  string? Documentation { get; }
  bool IsUserDefined { get; }
}

public interface IFunctionSymbol : IScriptSymbol
{
  bool IsExtension { get; }
  bool IsLambda { get; }
}

public interface IValueSymbol : IScriptSymbol
{
  AresValue Value { get; }
  bool IsReadOnly { get; }
}
