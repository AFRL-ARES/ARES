using Ares.Datamodel;
using AresScript.Symbols;
using System.Diagnostics.CodeAnalysis;

namespace AresScript.Environment;

public class AresScriptEnvironment
{
  private readonly Stack<SystemScope> _systemScopes = [];
  private readonly Stack<UserScope> _userScopes = [];
  private readonly Dictionary<AresValue.KindOneofCase, Dictionary<string, AresSystemFunctionSymbol>> _extensionFunctions = [];

  public AresScriptEnvironment()
  {
    var globalSystem = new SystemScope(PredefinedScope.Global.ToString());
    _systemScopes.Push(globalSystem);

    var globalUser = new UserScope(PredefinedScope.Global.ToString());
    _userScopes.Push(globalUser);
  }

  public void AssignVariable(string id, AresValue value, AresValueSchema? declaredSchema = null)
  {
    if(SystemValueExists(id))
    {
      throw new InvalidOperationException($"Variable {id} already exists as a system variable.");
    }

    var currentScope = _userScopes.Peek();
    currentScope.Variables[id] = AresSystemValue.From(value) with
    {
      Name = id,
      IsReadOnly = false,
      IsUserDefined = true,
      DeclaredSchema = declaredSchema
    };
  }

  public void AssignFunction(string id, AresScriptFunction value)
  {
    if(SystemFunctionExists(id))
    {
      throw new InvalidOperationException($"Function {id} already exists as a system function.");
    }

    var currentScope = _userScopes.Peek();
    currentScope.Functions[id] = value;
  }

  public void AssignLambda(string id, AresScriptLambda value)
  {
    if(SystemFunctionExists(id))
    {
      throw new InvalidOperationException($"Lambda {id} already exists as a system function.");
    }

    var currentScope = _userScopes.Peek();
    currentScope.Lambdas[id] = value;
  }

  public bool TryGetValueSymbol(string id, [NotNullWhen(true)] out AresSystemValue? symbol)
  {
    return TryGetFromScopes(_userScopes, scope => scope.Variables, id, out symbol)
      || TryGetFromScopes(_systemScopes, scope => scope.Variables, id, out symbol);
  }

  public bool TryGetValue(string id, [NotNullWhen(true)] out AresValue? value)
  {
    if(TryGetValueSymbol(id, out var symbol) && symbol?.Value is not null)
    {
      value = symbol.Value;
      return true;
    }

    value = null;
    return false;
  }

  public bool TryGetUserValueSymbol(string id, [NotNullWhen(true)] out AresSystemValue? symbol)
  {
    return TryGetFromScopes(_userScopes, scope => scope.Variables, id, out symbol);
  }

  public bool TryGetUserValueSymbol(string id, string scopeName, [NotNullWhen(true)] out AresSystemValue? symbol)
  {
    if(TryGetUserScope(scopeName, out var scope) && scope.Variables.TryGetValue(id, out symbol))
    {
      return true;
    }

    symbol = null;
    return false;
  }

  public bool TryGetUserValue(string id, [NotNullWhen(true)] out AresValue? value)
  {
    if(TryGetUserValueSymbol(id, out var symbol) && symbol?.Value is not null)
    {
      value = symbol.Value;
      return true;
    }

    value = null;
    return false;
  }

  public bool TryGetValueCurrentScope(string id, [NotNullWhen(true)] out AresValue? value)
  {
    var scope = _userScopes.Peek();
    var exists = scope.Variables.TryGetValue(id, out var variableSymbol);
    value = variableSymbol?.Value;
    return exists && value is not null;
  }

  public bool TryGetSystemValue(string id, [NotNullWhen(true)] out AresValue? value)
  {
    if(TryGetSystemValueSymbol(id, out var symbol) && symbol?.Value is not null)
    {
      value = symbol.Value;
      return true;
    }

    value = null;
    return false;
  }

  public bool TryGetSystemValueSymbol(string id, [NotNullWhen(true)] out AresSystemValue? symbol)
  {
    return TryGetFromScopes(_systemScopes, scope => scope.Variables, id, out symbol);
  }

  public bool SystemValueExists(string id)
  {
    return TryGetSystemValueSymbol(id, out _);
  }

  public bool TryGetUserFunction(string id, [NotNullWhen(true)] out AresScriptFunction? func)
  {
    return TryGetFromScopes(_userScopes, scope => scope.Functions, id, out func);
  }

  public bool TryGetUserFunction(string id, string scopeName, [NotNullWhen(true)] out AresScriptFunction? func)
  {
    if(TryGetUserScope(scopeName, out var scope) && scope.Functions.TryGetValue(id, out func) && func is not null)
    {
      return true;
    }

    func = null;
    return false;
  }

  public bool TryGetUserLambda(string id, [NotNullWhen(true)] out AresScriptLambda? lambda)
  {
    return TryGetFromScopes(_userScopes, scope => scope.Lambdas, id, out lambda);
  }

  public bool TryGetSystemFunction(string id, [NotNullWhen(true)] out AresSystemFunctionSymbol? func)
  {
    return TryGetFromScopes(_systemScopes, scope => scope.Functions, id, out func);
  }

  public bool SystemFunctionExists(string id)
  {
    return TryGetSystemFunction(id, out _);
  }

  public AresSystemFunctionSymbol[] GetAllSystemFunctions()
  {
    return [.. CollectDistinctByName(_systemScopes, scope => scope.Functions.Values, function => function.Name)];
  }

  public IReadOnlyList<KeyValuePair<string, AresSystemValue>> GetAllSystemVariables()
  {
    return CollectDistinctByName(_systemScopes, scope => scope.Variables, variable => variable.Key);
  }

  public KeyValuePair<string, AresSystemValue>[] GetAllUserVariableSymbols()
  {
    return [.. CollectDistinctByName(_userScopes, scope => scope.Variables, variable => variable.Key)];
  }

  public KeyValuePair<string, AresSystemValue>[] GetAllUserVariableSymbols(string scopeName)
  {
    return TryGetUserScope(scopeName, out var scope)
      ? [.. scope.Variables]
      : [];
  }

  public IReadOnlyList<string> GetAllUserVariableNames()
  {
    return CollectDistinctByName(_userScopes, scope => scope.Variables.Keys, name => name);
  }

  public IReadOnlyList<string> GetAllUserVariableNames(string scopeName)
  {
    return TryGetUserScope(scopeName, out var scope)
      ? [.. scope.Variables.Keys]
      : [];
  }

  public IReadOnlyList<AresScriptFunction> GetAllUserFunctions()
  {
    return CollectDistinctByName(_userScopes, scope => scope.Functions.Values, function => function.Name);
  }

  public IReadOnlyList<AresScriptFunction> GetAllUserFunctions(string scopeName)
  {
    return TryGetUserScope(scopeName, out var scope)
      ? [.. scope.Functions.Values]
      : [];
  }

  public IReadOnlyList<IScriptSymbol> GetAllUserSymbols()
  {
    return CollectDistinctByName(_userScopes, scope => scope.GetSymbols(), symbol => symbol.Name);
  }

  public IReadOnlyList<IScriptSymbol> GetAllUserSymbols(string scopeName)
  {
    return TryGetUserScope(scopeName, out var scope)
      ? [.. scope.GetSymbols()]
      : [];
  }

  public IReadOnlyList<IScriptSymbol> GetAllSystemSymbols()
  {
    return CollectDistinctByName(_systemScopes, scope => scope.GetSymbols(), symbol => symbol.Name);
  }

  public IReadOnlyList<IScriptSymbol> GetAllSymbols()
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var results = new List<IScriptSymbol>();

    AppendDistinctByName(results, seen, _userScopes.SelectMany(scope => scope.GetSymbols()), symbol => symbol.Name);
    AppendDistinctByName(results, seen, _systemScopes.SelectMany(scope => scope.GetSymbols()), symbol => symbol.Name);

    return results;
  }

  public int Depth => _userScopes.Count;

  // Only for vars
  public AresValue this[string val]
  {
    get
    {
      if(TryGetValue(val, out var value))
        return value;

      throw new KeyNotFoundException($"Key {val} not found in the environment.");
    }
    set
    {
      AssignVariable(val, value);
    }
  }

  public void EnterScope(string name = "")
  {
    _userScopes.Push(new UserScope(name));
  }

  public void EnterSystemScope(string name = "")
  {
    _systemScopes.Push(new SystemScope(name));
  }

  public void ExitScope()
  {
    if(_userScopes.Count <= 1)
    {
      throw new InvalidOperationException("Cannot exit the global scope.");
    }

    _userScopes.Pop();
  }

  public void ExitSystemScope()
  {
    if(_systemScopes.Count <= 1)
    {
      throw new InvalidOperationException("Cannot exit the global system scope.");
    }

    _systemScopes.Pop();
  }

  public void AssignSystemFunctions(params IEnumerable<AresSystemFunctionSymbol> functions)
  {
    var scope = _systemScopes.Peek();
    foreach(var f in functions)
    {
      scope.Functions[f.Id] = f;
    }
  }

  public void AssignSystemVariables(params IEnumerable<KeyValuePair<string, AresSystemValue>> variables)
  {
    var scope = _systemScopes.Peek();
    foreach(var (key, variable) in variables)
    {
      scope.Variables[key] = variable;
    }
  }

  public void AssignExtensionFunctions(params IEnumerable<AresExtensionFunction> functions)
  {
    foreach(var function in functions)
    {
      var kind = function.ReceiverKind;
      if(!_extensionFunctions.TryGetValue(kind, out var map))
      {
        map = new Dictionary<string, AresSystemFunctionSymbol>(StringComparer.Ordinal);
        _extensionFunctions[kind] = map;
      }

      map[function.MemberName] = function.Function;
    }
  }

  public bool TryGetExtensionFunction(AresValue receiver, string memberName, [NotNullWhen(true)] out AresSystemFunctionSymbol? function)
  {
    return TryGetExtensionFunction(receiver.KindCase, memberName, out function);
  }

  public bool TryGetExtensionFunction(AresValue.KindOneofCase kind, string memberName, [NotNullWhen(true)] out AresSystemFunctionSymbol? function)
  {
    if(_extensionFunctions.TryGetValue(kind, out var map) && map.TryGetValue(memberName, out var result))
    {
      function = result;
      return true;
    }

    function = null;
    return false;
  }

  public bool TryGetExtensionFunction(AresDataType type, string memberName, [NotNullWhen(true)] out AresSystemFunctionSymbol? function)
  {
    if(TryMapDataTypeToKind(type, out var kind))
    {
      return TryGetExtensionFunction(kind, memberName, out function);
    }

    function = null;
    return false;
  }

  public IReadOnlyList<AresSystemFunctionSymbol> GetExtensionFunctions(AresValue receiver)
  {
    return _extensionFunctions.TryGetValue(receiver.KindCase, out var map)
      ? map.Values.ToArray()
      : [];
  }

  private bool TryGetUserScope(string scopeName, [NotNullWhen(true)] out UserScope? scope)
  {
    foreach(var candidate in _userScopes)
    {
      if(string.Equals(candidate.Name, scopeName, StringComparison.Ordinal))
      {
        scope = candidate;
        return true;
      }
    }

    scope = null;
    return false;
  }

  private static bool TryGetFromScopes<TScope, TValue>(
    IEnumerable<TScope> scopes,
    Func<TScope, IReadOnlyDictionary<string, TValue>> selector,
    string id,
    [NotNullWhen(true)] out TValue? value)
    where TValue : class
  {
    foreach(var scope in scopes)
    {
      if(selector(scope).TryGetValue(id, out var candidate) && candidate is not null)
      {
        value = candidate;
        return true;
      }
    }

    value = null;
    return false;
  }

  private static IReadOnlyList<TItem> CollectDistinctByName<TScope, TItem>(
    IEnumerable<TScope> scopes,
    Func<TScope, IEnumerable<TItem>> selector,
    Func<TItem, string> nameSelector)
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var results = new List<TItem>();
    foreach(var scope in scopes)
    {
      AppendDistinctByName(results, seen, selector(scope), nameSelector);
    }

    return results;
  }

  private static void AppendDistinctByName<TItem>(
    ICollection<TItem> results,
    ISet<string> seen,
    IEnumerable<TItem> items,
    Func<TItem, string> nameSelector)
  {
    foreach(var item in items)
    {
      if(seen.Add(nameSelector(item)))
      {
        results.Add(item);
      }
    }
  }

  private static bool TryMapDataTypeToKind(AresDataType type, out AresValue.KindOneofCase kind)
  {
    switch(type)
    {
      case AresDataType.Boolean:
        kind = AresValue.KindOneofCase.BoolValue;
        return true;
      case AresDataType.Number:
        kind = AresValue.KindOneofCase.NumberValue;
        return true;
      case AresDataType.String:
        kind = AresValue.KindOneofCase.StringValue;
        return true;
      case AresDataType.ByteArray:
        kind = AresValue.KindOneofCase.BytesValue;
        return true;
      case AresDataType.StringArray:
        kind = AresValue.KindOneofCase.StringArrayValue;
        return true;
      case AresDataType.NumberArray:
        kind = AresValue.KindOneofCase.NumberArrayValue;
        return true;
      case AresDataType.List:
        kind = AresValue.KindOneofCase.ListValue;
        return true;
      case AresDataType.Struct:
        kind = AresValue.KindOneofCase.StructValue;
        return true;
      case AresDataType.Null:
        kind = AresValue.KindOneofCase.NullValue;
        return true;
      case AresDataType.Function:
        kind = AresValue.KindOneofCase.FunctionValue;
        return true;
      case AresDataType.Quantity:
        kind = AresValue.KindOneofCase.QuantityValue;
        return true;
      default:
        kind = default;
        return false;
    }
  }
}
