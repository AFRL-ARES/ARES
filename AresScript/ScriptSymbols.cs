namespace AresScript;

public enum ScriptSymbolKind
{
  Unspecified = 0,
  Function = 1,
  Variable = 2,
  Struct = 3
}

public enum ScriptSymbolTag
{
  Deprecated = 1,
  ReadOnly = 2,
  Static = 3,
  Extension = 4,
  UserDefined = 5,
  Experimental = 6,
  Lambda = 7
}

public interface IScriptSymbol
{
  string Name { get; }
  ScriptSymbolKind Kind { get; }
  IReadOnlyCollection<ScriptSymbolTag> Tags { get; }
}

public interface IFunctionSymbol : IScriptSymbol
{
  bool IsUserDefined { get; }
  bool IsLambda { get; }
}

public interface ILambdaSymbol : IFunctionSymbol
{
}

public interface IValueSymbol : IScriptSymbol
{
  bool IsReadOnly { get; }
}
