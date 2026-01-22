using Ares.Datamodel;
using System.Diagnostics.CodeAnalysis;

namespace AresScript;

public class AresScriptEnvironment
{
  private readonly Stack<SystemScope> _systemScopes = [];
  private readonly Stack<UserScope> _userScopes = [];

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

  public bool TryGetSystemValue(string id, [NotNullWhen(true)] out AresValue? value)
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

  public bool TryGetSystemFunction(string id, [NotNullWhen(true)] out AresSystemFunction? func)
  {
    foreach(var scope in _systemScopes)
    {
      if(scope.SystemFunctions.TryGetValue(id, out func))
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
      if(scope.SystemFunctions.ContainsKey(id))
      {
        return true;
      }
    }

    return false;
  }

  public AresSystemFunction[] GetAllSystemFunctions()
  {
    return _systemScopes.SelectMany(scope => scope.SystemFunctions.Values).ToArray();
  }

  public IReadOnlyList<KeyValuePair<string, AresValue>> GetAllSystemVariables()
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var results = new List<KeyValuePair<string, AresValue>>();
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
      scope.SystemFunctions[f.Id] = f;
    }
  }

  public void AssignSystemVariables(IEnumerable<KeyValuePair<string, AresValue>> variables)
  {
    var scope = _systemScopes.Peek();
    foreach(var (key, value) in variables)
    {
      scope.Variables[key] = value;
    }
  }  
}
