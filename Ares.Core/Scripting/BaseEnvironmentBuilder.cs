using AresScript;
using System.Collections.Generic;
using System.Linq;

namespace Ares.Core.Scripting;

public class BaseEnvironmentBuilder(IEnumerable<ISystemFunctionProvider> systemFunctionProviders)
{
  private readonly IEnumerable<ISystemFunctionProvider> _systemFunctionProvider = systemFunctionProviders;

  public AresScriptEnvironment Build()
  {
    var env = new AresScriptEnvironment();
    var functions = _systemFunctionProvider.SelectMany(sfp => sfp.GetFunctions()).ToArray();
    if(functions.Length == 0)
    {
      return env;
    }

    // let's stick with global scope for now
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignSystemFunctions(functions);
    env.AssignSystemVariables(BuildNamespaceVariables(env));

    return env;
  }

  private static IEnumerable<KeyValuePair<string, AresSystemValue>> BuildNamespaceVariables(AresScriptEnvironment environment)
  {
    var rootVariables = new Dictionary<string, AresSystemValue>(StringComparer.Ordinal);
    var functions = environment.GetAllSystemFunctions();

    foreach(var func in functions)
    {
      if(string.IsNullOrWhiteSpace(func.Namespace))
      {
        var globalName = string.IsNullOrWhiteSpace(func.Name) ? func.Id : func.Name;
        if(!string.IsNullOrWhiteSpace(globalName) && !rootVariables.ContainsKey(globalName))
        {
          rootVariables[globalName] = AresSystemValue.Function(func);
        }
        continue;
      }

      var fieldName = string.IsNullOrWhiteSpace(func.Name) ? func.Id : func.Name;
      if(string.IsNullOrWhiteSpace(fieldName))
      {
        continue;
      }

      var pathSegments = func.Namespace
        .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      if(pathSegments.Length == 0)
      {
        continue;
      }

      AddFunctionToPath(rootVariables, pathSegments, fieldName, func);
    }

    return rootVariables.Select(kv => new KeyValuePair<string, AresSystemValue>(kv.Key, kv.Value));
  }

  private static void AddFunctionToPath(
    IDictionary<string, AresSystemValue> root,
    IReadOnlyList<string> pathSegments,
    string fieldName,
    AresSystemFunction func)
  {
    var current = root;
    for(var i = 0; i < pathSegments.Count; i++)
    {
      var segment = pathSegments[i];
      if(string.IsNullOrWhiteSpace(segment))
      {
        continue;
      }

      if(!current.TryGetValue(segment, out var value)
        || value.Kind != AresSystemValue.AresSystemValueKind.Struct
        || value.StructFields is null)
      {
        var fields = new Dictionary<string, AresSystemValue>(StringComparer.Ordinal);
        var structValue = AresSystemValue.Struct(fields);
        current[segment] = structValue;
        current = fields;
        continue;
      }

      if(value.StructFields is Dictionary<string, AresSystemValue> dict)
      {
        current = dict;
      }
      else
      {
        var fields = new Dictionary<string, AresSystemValue>(value.StructFields, StringComparer.Ordinal);
        current[segment] = AresSystemValue.Struct(fields, value.Description, value.StructKind);
        current = fields;
      }
    }

    if(!current.ContainsKey(fieldName))
    {
      current[fieldName] = AresSystemValue.Function(func);
    }
  }
}
