using Ares.Datamodel;
using System.Diagnostics.CodeAnalysis;

namespace AresScript;

public class AresScriptEnvironment
{
  private readonly Stack<SystemScope> _systemScopes = [];
  private readonly Stack<UserScope> _userScopes = [];
  private readonly Dictionary<AresValue.KindOneofCase, Dictionary<string, AresSystemFunction>> _extensionFunctions = [];

  public AresScriptEnvironment()
  {
    var globalSystem = new SystemScope("global");
    _systemScopes.Push(globalSystem);

    var globalUser = new UserScope("global");
    _userScopes.Push(globalUser);
  }

  public void AssignVariable(string id, AresValue value)
  {
    if(SystemValueExists(id))
    {
      throw new InvalidOperationException($"Variable {id} already exists as a system variable.");
    }

    var currentScope = _userScopes.Peek();
    currentScope.Variables[id] = value;
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

  public bool TryGetValue(string id, [NotNullWhen(true)] out AresValue? value)
  {
    foreach(var scope in _userScopes)
    {
      var valueExists = scope.Variables.TryGetValue(id, out value);
      if(valueExists && value is not null)
        return true;
    }

    foreach(var scope in _systemScopes)
    {
      var valueExists = scope.Variables.TryGetValue(id, out var sysVal);
      value = sysVal?.ToAresValue();
      if(valueExists && value is not null)
        return true;
    }

    value = null;
    return false;
  }

  public bool TryGetUserValue(string id, [NotNullWhen(true)] out AresValue? value)
  {
    foreach(var scope in _userScopes)
    {
      var valueExists = scope.Variables.TryGetValue(id, out value);
      if(valueExists && value is not null)
        return true;
    }

    value = null;
    return false;
  }

  public bool TryGetValueCurrentScope(string id, [NotNullWhen(true)] out AresValue? value)
  {
    var scope = _userScopes.Peek();
    return scope.Variables.TryGetValue(id, out value);
  }

  public bool TryGetSystemValue(string id, [NotNullWhen(true)] out AresSystemValue? value)
  {
    foreach(var scope in _systemScopes)
    {
      var valueExists = scope.Variables.TryGetValue(id, out value);
      if(valueExists && value is not null)
        return true;
    }

    value = null;
    return false;
  }

  public bool SystemValueExists(string id)
  {
    foreach(var scope in _systemScopes)
    {
      if(scope.Variables.ContainsKey(id))
      {
        return true;
      }
    }

    return false;
  }

  public bool TryGetUserFunction(string id, [NotNullWhen(true)] out AresScriptFunction? func)
  {
    foreach(var scope in _userScopes)
    {
      var funcExists = scope.Functions.TryGetValue(id, out func);
      if(funcExists && func is not null)
        return true;
    }

    func = null;
    return false;
  }

  public bool TryGetUserLambda(string id, [NotNullWhen(true)] out AresScriptLambda? lambda)
  {
    foreach(var scope in _userScopes)
    {
      var lambdaExists = scope.Lambdas.TryGetValue(id, out lambda);
      if(lambdaExists && lambda is not null)
      {
        return true;
      }
    }

    lambda = null;
    return false;
  }

  public bool TryGetSystemFunction(string id, [NotNullWhen(true)] out AresSystemFunction? func)
  {
    foreach(var scope in _systemScopes)
    {
      if(scope.Functions.TryGetValue(id, out func))
      {
        return true;
      }
    }

    func = null;
    return false;
  }

  public bool SystemFunctionExists(string id)
  {
    foreach(var scope in _systemScopes)
    {
      if(scope.Functions.ContainsKey(id))
      {
        return true;
      }
    }

    return false;
  }

  public AresSystemFunction[] GetAllSystemFunctions()
  {
    return _systemScopes.SelectMany(scope => scope.Functions.Values).ToArray();
  }

  public IReadOnlyList<KeyValuePair<string, AresSystemValue>> GetAllSystemVariables()
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var results = new List<KeyValuePair<string, AresSystemValue>>();
    foreach(var scope in _systemScopes)
    {
      foreach(var kv in scope.Variables)
      {
        if(seen.Add(kv.Key))
        {
          results.Add(kv);
        }
      }
    }

    return results;
  }

  public KeyValuePair<string, AresValue>[] GetAllUserVariables()
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var results = new List<KeyValuePair<string, AresValue>>();
    foreach(var scope in _userScopes)
    {
      foreach(var variable in scope.Variables)
      {
        if(seen.Add(variable.Key))
        {
          results.Add(new KeyValuePair<string, AresValue>(variable.Key, variable.Value));
        }
      }
    }

    return results.ToArray();
  }

  public IReadOnlyList<string> GetAllUserVariableNames()
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var results = new List<string>();
    foreach(var scope in _userScopes)
    {
      foreach(var key in scope.Variables.Keys)
      {
        if(seen.Add(key))
        {
          results.Add(key);
        }
      }
    }

    return results;
  }

  public IReadOnlyList<AresScriptFunction> GetAllUserFunctions()
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var results = new List<AresScriptFunction>();
    foreach(var scope in _userScopes)
    {
      foreach(var func in scope.Functions.Values)
      {
        if(seen.Add(func.Name))
        {
          results.Add(func);
        }
      }
    }

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

  public void AssignSystemFunctions(params IEnumerable<AresSystemFunction> functions)
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
    foreach(var (key, value) in variables)
    {
      scope.Variables[key] = value;
    }
  }

  public void AssignExtensionFunctions(params IEnumerable<AresExtensionFunction> functions)
  {
    foreach(var function in functions)
    {
      var kind = function.ReceiverKind;
      if(!_extensionFunctions.TryGetValue(kind, out var map))
      {
        map = new Dictionary<string, AresSystemFunction>(StringComparer.Ordinal);
        _extensionFunctions[kind] = map;
      }

      map[function.MemberName] = function.Function;
    }
  }

  public bool TryGetExtensionFunction(AresValue receiver, string memberName, [NotNullWhen(true)] out AresSystemFunction? function)
  {
    return TryGetExtensionFunction(receiver.KindCase, memberName, out function);
  }

  public bool TryGetExtensionFunction(AresValue.KindOneofCase kind, string memberName, [NotNullWhen(true)] out AresSystemFunction? function)
  {
    if(_extensionFunctions.TryGetValue(kind, out var map) && map.TryGetValue(memberName, out var result))
    {
      function = result;
      return true;
    }

    function = null;
    return false;
  }

  public bool TryGetExtensionFunction(AresDataType type, string memberName, [NotNullWhen(true)] out AresSystemFunction? function)
  {
    if(TryMapDataTypeToKind(type, out var kind))
    {
      return TryGetExtensionFunction(kind, memberName, out function);
    }

    function = null;
    return false;
  }

  public IReadOnlyList<AresSystemFunction> GetExtensionFunctions(AresValue receiver)
  {
    return _extensionFunctions.TryGetValue(receiver.KindCase, out var map)
      ? map.Values.ToArray()
      : [];
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
      default:
        kind = default;
        return false;
    }
  }
}
